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
            context.Donors.AddOrUpdate(donor.ToDonorEntity());

            foreach (string locus in new List<string> { "A", "B", "C", "DRB1", "DQB1" })
            {
                context.Database.ExecuteSqlCommand($@"DELETE FROM MatchingGroupAt{locus} WHERE DonorId = " + donor.DonorId);
            }

            InsertPGroupMatches(donor.DonorId, context.MatchingGroupsAtA, donor.MatchingHla.A_1, TypePositions.One, () => new MatchingGroupAtA());
            InsertPGroupMatches(donor.DonorId, context.MatchingGroupsAtA, donor.MatchingHla.A_2, TypePositions.Two, () => new MatchingGroupAtA());
            InsertPGroupMatches(donor.DonorId, context.MatchingGroupsAtB, donor.MatchingHla.B_1, TypePositions.One, () => new MatchingGroupAtB());
            InsertPGroupMatches(donor.DonorId, context.MatchingGroupsAtB, donor.MatchingHla.B_2, TypePositions.Two, () => new MatchingGroupAtB());
            InsertPGroupMatches(donor.DonorId, context.MatchingGroupsAtC, donor.MatchingHla.C_1, TypePositions.One, () => new MatchingGroupAtC());
            InsertPGroupMatches(donor.DonorId, context.MatchingGroupsAtC, donor.MatchingHla.C_2, TypePositions.Two, () => new MatchingGroupAtC());
            InsertPGroupMatches(donor.DonorId, context.MatchingGroupsAtDrb1, donor.MatchingHla.DRB1_1, TypePositions.One, () => new MatchingGroupAtDrb1());
            InsertPGroupMatches(donor.DonorId, context.MatchingGroupsAtDrb1, donor.MatchingHla.DRB1_2, TypePositions.Two, () => new MatchingGroupAtDrb1());
            InsertPGroupMatches(donor.DonorId, context.MatchingGroupsAtDqb1, donor.MatchingHla.DQB1_1, TypePositions.One, () => new MatchingGroupAtDqb1());
            InsertPGroupMatches(donor.DonorId, context.MatchingGroupsAtDqb1, donor.MatchingHla.DQB1_2, TypePositions.Two, () => new MatchingGroupAtDqb1());

            context.SaveChanges();
        }

        public void InsertPGroupMatches<T>(int donorId, DbSet<T> table, ExpandedHla hla, TypePositions position, Func<T> maker) where T : MatchingGroup
        {
            if (hla != null && hla.PGroups != null)
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
				{SelectForLocus("A", matchRequest.LocusMismatchA, TypePositions.One)}
                UNION
				{SelectForLocus("B", matchRequest.LocusMismatchB, TypePositions.One)}
                UNION
				{SelectForLocus("DRB1", matchRequest.LocusMismatchDRB1, TypePositions.One)}
                UNION
                -- Next from type position 2 in the search hla
				{SelectForLocus("A", matchRequest.LocusMismatchA, TypePositions.Two)}
                UNION
				{SelectForLocus("B", matchRequest.LocusMismatchB, TypePositions.Two)}
                UNION
				{SelectForLocus("DRB1", matchRequest.LocusMismatchDRB1, TypePositions.Two)}
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

        private string SelectForLocus(string locus, DonorLocusMatchCriteria mismatch, TypePositions typePosition)
        {
            var names = typePosition.Equals(TypePositions.One) ? mismatch.HlaNamesToMatchInPositionOne : mismatch.HlaNamesToMatchInPositionTwo;
            return $@"SELECT d.DonorId, '{locus}' as Locus, d.TypePosition AS GvH, {(int)typePosition} AS HvG
                      FROM MatchingGroupAt{locus} d
                      JOIN dbo.PGroupNames p ON p.Id = d.Pgroup_Id 
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