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
				-- join search and donor match lists by Locus & matching hla name
				SELECT d.DonorId, d.Locus, d.TypePosition AS GvH, 1 AS HvG
                FROM DonorHlas d
				WHERE (d.Locus = '{0}' AND d.HlaName IN ('{1}'))
				GROUP BY d.DonorId, d.Locus, d.TypePosition
                UNION
				SELECT d.DonorId, d.Locus, d.TypePosition AS GvH, 2 AS HvG
                FROM DonorHlas d
				WHERE (d.Locus = '{0}' AND d.HlaName IN ('{2}'))
				GROUP BY d.DonorId, d.Locus, d.TypePosition
				) AS src
			UNPIVOT (type_position FOR matching_direction IN (GvH, HvG)) AS unpvt
			) ByDirection
		GROUP BY DonorId, matching_direction, Locus
		) ByLocus
	GROUP BY DonorId, Locus
	) ByDonor
GROUP BY DonorId
HAVING SUM(match_count) >= 0
ORDER BY TotalMatchCount DESC", "A",
                string.Join("','", matchRequest.LocusMismatchA.HlaNamesToMatchInPositionOne),
                string.Join(",", matchRequest.LocusMismatchA.HlaNamesToMatchInPositionTwo));

            return context.Database.SqlQuery<PotentialMatch>(sql);
        }

        public void UpdateDonorWithNewHla(SearchableDonor donor)
        {
            // Insert will insert or update.
            InsertDonor(donor);
        }
    }
}