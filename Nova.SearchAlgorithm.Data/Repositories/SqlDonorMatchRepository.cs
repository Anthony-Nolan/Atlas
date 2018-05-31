using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Models.Extensions;
using Nova.SearchAlgorithm.Data.Entity;

namespace Nova.SearchAlgorithm.Data.Repositories
{
    public interface IDonorMatchRepository
    {
        int HighestDonorId();
        void InsertDonor(InputDonor donor);
        void UpdateDonorWithNewHla(InputDonor donor);
        DonorResult GetDonor(int donorId);
        IEnumerable<DonorResult> AllDonors();
        IEnumerable<PotentialMatch> Search(DonorMatchCriteria matchRequest);
    }

    public class SqlDonorMatchRepository : IDonorMatchRepository
    {
        private readonly SearchAlgorithmContext context;

        public SqlDonorMatchRepository(SearchAlgorithmContext context)
        {
            this.context = context;
        }

        public int HighestDonorId()
        {
            return context.Donors.OrderByDescending(d => d.DonorId).Take(1).Select(d => d.DonorId).FirstOrDefault();
        }

        public IEnumerable<DonorResult> AllDonors()
        {
            return context.Donors.ToList().Select(d => d.ToRawDonor());
        }

        public DonorResult GetDonor(int donorId)
        {
            return context.Donors.FirstOrDefault(d => d.DonorId == donorId)?.ToRawDonor();
        }

        public void InsertDonor(InputDonor donor)
        {
            // Delete donor if it exists, since it won't be attached to the entity context so we can't just update it.
            DeleteDonorIfExists(donor);

            context.Donors.Add(donor.ToDonorEntity());

            foreach (Locus locus in Enum.GetValues(typeof(Locus)).Cast<Locus>())
            {
                context.Database.ExecuteSqlCommand($@"DELETE FROM MatchingHlaAt{locus.ToString().ToUpper()} WHERE DonorId = {donor.DonorId}");
            }

            InsertPGroupMatches(donor.DonorId, context.MatchingHlaAtA, donor.MatchingHla.A_1, TypePositions.One, () => new MatchingHlaAtA());
            InsertPGroupMatches(donor.DonorId, context.MatchingHlaAtA, donor.MatchingHla.A_2, TypePositions.Two, () => new MatchingHlaAtA());
            InsertPGroupMatches(donor.DonorId, context.MatchingHlaAtB, donor.MatchingHla.B_1, TypePositions.One, () => new MatchingHlaAtB());
            InsertPGroupMatches(donor.DonorId, context.MatchingHlaAtB, donor.MatchingHla.B_2, TypePositions.Two, () => new MatchingHlaAtB());
            InsertPGroupMatches(donor.DonorId, context.MatchingHlaAtC, donor.MatchingHla.C_1, TypePositions.One, () => new MatchingHlaAtC());
            InsertPGroupMatches(donor.DonorId, context.MatchingHlaAtC, donor.MatchingHla.C_2, TypePositions.Two, () => new MatchingHlaAtC());
            InsertPGroupMatches(donor.DonorId, context.MatchingHlaAtDrb1, donor.MatchingHla.DRB1_1, TypePositions.One, () => new MatchingHlaAtDrb1());
            InsertPGroupMatches(donor.DonorId, context.MatchingHlaAtDrb1, donor.MatchingHla.DRB1_2, TypePositions.Two, () => new MatchingHlaAtDrb1());
            InsertPGroupMatches(donor.DonorId, context.MatchingHlaAtDqb1, donor.MatchingHla.DQB1_1, TypePositions.One, () => new MatchingHlaAtDqb1());
            InsertPGroupMatches(donor.DonorId, context.MatchingHlaAtDqb1, donor.MatchingHla.DQB1_2, TypePositions.Two, () => new MatchingHlaAtDqb1());

            context.SaveChanges();
        }

        private void DeleteDonorIfExists(InputDonor donor)
        {
            var result = context.Donors.FirstOrDefault(d => d.DonorId == donor.DonorId);
            if (result != null)
            {
                context.Donors.Remove(result);
            }
        }

        public void InsertPGroupMatches<T>(int donorId, DbSet<T> table, ExpandedHla hla, TypePositions position, Func<T> maker) where T : MatchingHla
        {
            if (hla?.PGroups != null)
            {
                table.AddRange(
                    hla.PGroups.Select(pg => {
                        T match = maker();
                        match.DonorId = donorId;
                        match.PGroup = FindOrCreatePGroup(pg);
                        match.TypePosition = (int)position;
                    return match;
                    })
                );
            }
        }

        private PGroupName FindOrCreatePGroup(string pGroupName)
        {
            var existing = context.PGroupNames.FirstOrDefault(pg => pg.Name == pGroupName);

            if (existing != null)
            {
                return existing;
            }

            var newPGroup = context.PGroupNames.Add(new PGroupName { Name = pGroupName });
            context.SaveChanges();
            return newPGroup;
        }

        public IEnumerable<PotentialMatch> Search(DonorMatchCriteria matchRequest)
        {
            string sql = $@"SELECT DonorId, SUM(MatchCount) AS TotalMatchCount
FROM (
    -- get overall match count by Locus
    SELECT DonorId, Locus, MIN(MatchCount) AS MatchCount
    FROM (
        -- count number of matches in each direction
        SELECT DonorId, MatchingDirection, Locus, count(*) AS MatchCount
        FROM(
            -- get DISTINCT list of matches between search and donor type by Locus and position
            SELECT DISTINCT DonorId, MatchingDirection, Locus, TypePosition
            FROM (
                -- Select search and donor directional match lists by Locus & matching hla name
                -- First from type position 1 in the search hla
				{SelectForLocus(Locus.A, matchRequest.LocusMismatchA, TypePositions.One)}
                UNION
				{SelectForLocus(Locus.B, matchRequest.LocusMismatchB, TypePositions.One)}
                UNION
				{SelectForLocus(Locus.Drb1, matchRequest.LocusMismatchDRB1, TypePositions.One)}
                UNION
                -- Next from type position 2 in the search hla
				{SelectForLocus(Locus.A, matchRequest.LocusMismatchA, TypePositions.Two)}
                UNION
				{SelectForLocus(Locus.B, matchRequest.LocusMismatchB, TypePositions.Two)}
                UNION
				{SelectForLocus(Locus.Drb1, matchRequest.LocusMismatchDRB1, TypePositions.Two)}
                ) AS source
            UNPIVOT (TypePosition FOR MatchingDirection IN (GvH, HvG)) AS unpivoted
            ) ByDirection
        GROUP BY DonorId, MatchingDirection, Locus
        ) ByLocus
    GROUP BY DonorId, Locus
    ) ByDonor
GROUP BY DonorId
HAVING SUM(MatchCount) >= {6 - matchRequest.DonorMismatchCount}
ORDER BY TotalMatchCount DESC";

            return context.Database.SqlQuery<PotentialMatch>(sql);
        }

        private string SelectForLocus(Locus locus, DonorLocusMatchCriteria mismatch, TypePositions typePosition)
        {
            var names = typePosition.Equals(TypePositions.One) ? mismatch.HlaNamesToMatchInPositionOne : mismatch.HlaNamesToMatchInPositionTwo;
            return $@"SELECT d.DonorId, '{locus.ToString().ToUpper()}' as Locus, d.TypePosition AS GvH, {(int)typePosition} AS HvG
                      FROM MatchingHlaAt{locus.ToString().ToUpper()} d
                      JOIN dbo.PGroupNames p ON p.Id = d.PGroup_Id 
                      WHERE [Name] IN('{string.Join("', '", names)}')
                      GROUP BY d.DonorId, d.TypePosition";
        }

        public void UpdateDonorWithNewHla(InputDonor donor)
        {
            // Insert will insert or update.
            InsertDonor(donor);
        }
    }
}