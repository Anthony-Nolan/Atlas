using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Entity;

namespace Nova.SearchAlgorithm.Data.Repositories
{
    public interface IDonorMatchRepository
    {
        void InsertDonor(SearchableDonor donor);
        void UpdateDonorWithNewHla(SearchableDonor donor);
        SearchableDonor GetDonor(int donorId);
        IEnumerable<SearchableDonor> AllDonors();
        IEnumerable<PotentialMatch> Search(DonorMatchCriteria matchRequest);
    }

    static class SearchDonorExtensions
    {
        public static Donor ToDonorEntity(this SearchableDonor donor)
        {
            return new Donor
            {
                DonorId = donor.DonorId,
                DonorType = donor.DonorType,
                RegistryCode = donor.RegistryCode
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

        public IEnumerable<SearchableDonor> AllDonors()
        {
            return context.Donors.ToList().Select(d => d.ToSearchableDonor());
        }

        public SearchableDonor GetDonor(int donorId)
        {
            return context.Donors.FirstOrDefault(d => d.DonorId == donorId)?.ToSearchableDonor();
        }

        public void InsertDonor(SearchableDonor donor)
        {
            context.Donors.AddOrUpdate(donor.ToDonorEntity());

            context.DonorHla.SqlQuery(@"DELETE * FROM DonorHlas WHERE DonorId = " + donor.DonorId);

            donor.MatchingHla.EachPosition((l, p, hla) => {
                if (hla == null || hla.PGroups == null)
                {
                    return;
                }

                context.DonorHla.AddRange(
                    hla.PGroups.Select(pg => new DonorHla
                    {
                        DonorId = donor.DonorId,
                        HlaName = pg,
                        Locus = l,
                        TypePosition = (int)p
                    })
                );
            });

            context.SaveChanges();
        }

        public IEnumerable<PotentialMatch> Search(DonorMatchCriteria matchRequest)
        {
            string sql = string.Format(@"SELECT DonorId, SUM(match_count) AS TotalMatchCount
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
				SELECT d.DonorId, d.Locus, d.TypePosition AS GvH, 1 AS HvG
                FROM DonorHlas d
				WHERE (d.Locus = 'A' AND d.HlaName IN ('{0}'))
                   OR (d.Locus = 'B' AND d.HlaName IN ('{2}'))
                   OR (d.Locus = 'DRB1' AND d.HlaName IN ('{4}'))
				GROUP BY d.DonorId, d.Locus, d.TypePosition
                UNION
                -- Next from type position 2 in the search hla
				SELECT d.DonorId, d.Locus, d.TypePosition AS GvH, 2 AS HvG
                FROM DonorHlas d
				WHERE (d.Locus = 'A' AND d.HlaName IN ('{1}'))
                   OR (d.Locus = 'B' AND d.HlaName IN ('{3}'))
                   OR (d.Locus = 'DRB1' AND d.HlaName IN ('{5}'))
				GROUP BY d.DonorId, d.Locus, d.TypePosition
				) AS src
			UNPIVOT (type_position FOR matching_direction IN (GvH, HvG)) AS unpvt
			) ByDirection
		GROUP BY DonorId, matching_direction, Locus
		) ByLocus
	GROUP BY DonorId, Locus
	) ByDonor
GROUP BY DonorId
HAVING SUM(match_count) >= {6}
ORDER BY TotalMatchCount DESC",
                // No chance of injection attack since these strings come from our own matching dictionary.
                string.Join("','", matchRequest.LocusMismatchA.HlaNamesToMatchInPositionOne),
                string.Join("','", matchRequest.LocusMismatchA.HlaNamesToMatchInPositionTwo),
                string.Join("','", matchRequest.LocusMismatchB.HlaNamesToMatchInPositionOne),
                string.Join("','", matchRequest.LocusMismatchB.HlaNamesToMatchInPositionTwo),
                string.Join("','", matchRequest.LocusMismatchDRB1.HlaNamesToMatchInPositionOne),
                string.Join("','", matchRequest.LocusMismatchDRB1.HlaNamesToMatchInPositionTwo),
                // TODO:NOVA-1119 fix the matching logic
                6 - matchRequest.DonorMismatchCount);

            return context.Database.SqlQuery<PotentialMatch>(sql);
        }

        public void UpdateDonorWithNewHla(SearchableDonor donor)
        {
            // Insert will insert or update.
            InsertDonor(donor);
        }
    }
}