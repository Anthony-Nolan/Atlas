using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Models;
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

    static class SearchDonorExtensions
    {
        public static Donor ToDonorEntity(this InputDonor donor)
        {
            return new Donor
            {
                DonorId = donor.DonorId,
                DonorType = donor.DonorType,
                RegistryCode = donor.RegistryCode,
                A_1 = donor.MatchingHla?.A_1?.Name,
                A_2 = donor.MatchingHla?.A_2?.Name,
                B_1 = donor.MatchingHla?.B_1?.Name,
                B_2 = donor.MatchingHla?.B_2?.Name,
                C_1 = donor.MatchingHla?.C_1?.Name,
                C_2 = donor.MatchingHla?.C_2?.Name,
                DRB1_1 = donor.MatchingHla?.DRB1_1?.Name,
                DRB1_2 = donor.MatchingHla?.DRB1_2?.Name,
                DQB1_1 = donor.MatchingHla?.DQB1_1?.Name,
                DQB1_2 = donor.MatchingHla?.DQB1_2?.Name,
            };
        }
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
            //TEMP: for now assume if something is at A, then we're done
            if (context.MatchingGroupsAtA.Any(m => m.DonorId == donor.DonorId)) {
                return;
            }

            context.Donors.AddOrUpdate(donor.ToDonorEntity());

            context.Database.ExecuteSqlCommand(@"DELETE FROM MatchingGroupAtAs WHERE DonorId = " + donor.DonorId);
            context.Database.ExecuteSqlCommand(@"DELETE FROM MatchingGroupAtBs WHERE DonorId = " + donor.DonorId);
            context.Database.ExecuteSqlCommand(@"DELETE FROM MatchingGroupAtCs WHERE DonorId = " + donor.DonorId);
            context.Database.ExecuteSqlCommand(@"DELETE FROM MatchingGroupAtDrb1 WHERE DonorId = " + donor.DonorId);
            context.Database.ExecuteSqlCommand(@"DELETE FROM MatchingGroupAtDqb1 WHERE DonorId = " + donor.DonorId);

            InsertPGroupMatches(donor.DonorId, context.MatchingGroupsAtA, donor.MatchingHla.A_1, () => new MatchingGroupAtA());
            InsertPGroupMatches(donor.DonorId, context.MatchingGroupsAtA, donor.MatchingHla.A_2, () => new MatchingGroupAtA());
            InsertPGroupMatches(donor.DonorId, context.MatchingGroupsAtB, donor.MatchingHla.B_1, () => new MatchingGroupAtB());
            InsertPGroupMatches(donor.DonorId, context.MatchingGroupsAtB, donor.MatchingHla.B_2, () => new MatchingGroupAtB());
            InsertPGroupMatches(donor.DonorId, context.MatchingGroupsAtC, donor.MatchingHla.C_1, () => new MatchingGroupAtC());
            InsertPGroupMatches(donor.DonorId, context.MatchingGroupsAtC, donor.MatchingHla.C_2, () => new MatchingGroupAtC());
            InsertPGroupMatches(donor.DonorId, context.MatchingGroupsAtDrb1, donor.MatchingHla.DRB1_1, () => new MatchingGroupAtDrb1());
            InsertPGroupMatches(donor.DonorId, context.MatchingGroupsAtDrb1, donor.MatchingHla.DRB1_2, () => new MatchingGroupAtDrb1());
            InsertPGroupMatches(donor.DonorId, context.MatchingGroupsAtDqb1, donor.MatchingHla.DQB1_1, () => new MatchingGroupAtDqb1());
            InsertPGroupMatches(donor.DonorId, context.MatchingGroupsAtDqb1, donor.MatchingHla.DQB1_2, () => new MatchingGroupAtDqb1());
            context.SaveChanges();
        }

        public void InsertPGroupMatches<T>(int donorId, DbSet<T> table, ExpandedHla hla, Func<T> maker) where T : MatchingGroup
        {
            if (hla != null && hla.PGroups != null)
            {
                table.AddRange(
                    hla.PGroups.Select(pg => {
                        T match = maker();
                        match.DonorId = donorId;
                        match.PGroup = FindOrCreatePGroup(pg);
                        match.TypePosition = (int)TypePositions.One;
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

            return context.PGroupNames.Add(new PGroupName { Name = pGroupName });
        }

        public IEnumerable<PotentialMatch> Search(DonorMatchCriteria matchRequest)
        {
            string sql = $@"SELECT DonorId, SUM(match_count) AS TotalMatchCount
FROM (
    -- get overall match count by Locus
    SELECT DonorId, Locus, MIN(match_count) AS match_count
    FROM (
        -- count number of matches in each direction
        SELECT DonorId, matching_direction, Locus, count(*) AS match_count
        FROM(
            -- get DISTINCT list of matches between search and donor type by Locus and position
            SELECT DISTINCT DonorId, matching_direction, Locus, type_position
            FROM (
                -- Select search and donor directional match lists by Locus & matching hla name
                -- First from type position 1 in the search hla
				SELECT d.DonorId, 'A' as Locus, d.TypePosition AS GvH, 1 AS HvG FROM MatchingGroupAtAs d JOIN dbo.PGroupNames p ON p.Id = d.Pgroup_Id 
                    WHERE [Name] IN ('{string.Join("','", matchRequest.LocusMismatchA.HlaNamesToMatchInPositionOne)}') GROUP BY d.DonorId, d.TypePosition
                UNION
				SELECT d.DonorId, 'B' as Locus, d.TypePosition AS GvH, 1 AS HvG FROM MatchingGroupAtBs d JOIN dbo.PGroupNames p ON p.Id = d.Pgroup_Id 
                    WHERE [Name] IN ('{string.Join("','", matchRequest.LocusMismatchB.HlaNamesToMatchInPositionOne)}') GROUP BY d.DonorId, d.TypePosition
                UNION
				SELECT d.DonorId, 'DRB1' as Locus, d.TypePosition AS GvH, 1 AS HvG FROM MatchingGroupAtDrb1 d JOIN dbo.PGroupNames p ON p.Id = d.Pgroup_Id 
                    WHERE [Name] IN ('{string.Join("','", matchRequest.LocusMismatchDRB1.HlaNamesToMatchInPositionOne)}') GROUP BY d.DonorId, d.TypePosition
				UNION
                -- Next from type position 2 in the search hla
				SELECT d.DonorId, 'A' as Locus, d.TypePosition AS GvH, 2 AS HvG FROM MatchingGroupAtAs d JOIN dbo.PGroupNames p ON p.Id = d.Pgroup_Id 
                    WHERE [Name] IN ('{string.Join("','", matchRequest.LocusMismatchA.HlaNamesToMatchInPositionTwo)}') GROUP BY d.DonorId, d.TypePosition
                UNION
				SELECT d.DonorId, 'B' as Locus, d.TypePosition AS GvH, 2 AS HvG FROM MatchingGroupAtBs d JOIN dbo.PGroupNames p ON p.Id = d.Pgroup_Id 
                    WHERE [Name] IN ('{string.Join("','", matchRequest.LocusMismatchB.HlaNamesToMatchInPositionTwo)}') GROUP BY d.DonorId, d.TypePosition
                UNION
				SELECT d.DonorId, 'DRB1' as Locus, d.TypePosition AS GvH, 2 AS HvG FROM MatchingGroupAtDrb1 d JOIN dbo.PGroupNames p ON p.Id = d.Pgroup_Id 
                    WHERE [Name] IN ('{string.Join("', '", matchRequest.LocusMismatchDRB1.HlaNamesToMatchInPositionTwo)}') GROUP BY d.DonorId, d.TypePosition                
                ) AS src
            UNPIVOT (type_position FOR matching_direction IN (GvH, HvG)) AS unpvt
            ) ByDirection
        GROUP BY DonorId, matching_direction, Locus
        ) ByLocus
    GROUP BY DonorId, Locus
    ) ByDonor
GROUP BY DonorId
HAVING SUM(match_count) >= {6 - matchRequest.DonorMismatchCount}
ORDER BY TotalMatchCount DESC";

            return context.Database.SqlQuery<PotentialMatch>(sql);
        }

        public void UpdateDonorWithNewHla(InputDonor donor)
        {
            // Insert will insert or update.
            InsertDonor(donor);
        }
    }
}