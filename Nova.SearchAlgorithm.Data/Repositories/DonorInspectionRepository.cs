using Dapper;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Matching;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Data.Helpers;
using Nova.SearchAlgorithm.Data.Models;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Data.Repositories
{
    public class DonorInspectionRepository : IDonorInspectionRepository
    {
        private readonly SearchAlgorithmContext context;

        private readonly string connectionString = ConfigurationManager.ConnectionStrings["SqlConnectionString"].ConnectionString;

        public DonorInspectionRepository(SearchAlgorithmContext context)
        {
            this.context = context;
        }

        public Task<int> HighestDonorId()
        {
            return context.Donors.OrderByDescending(d => d.DonorId).Take(1).Select(d => d.DonorId).FirstOrDefaultAsync();
        }

        public async Task<IBatchQueryAsync<DonorResult>> DonorsAddedSinceLastHlaUpdate()
        {
            var highestDonorId = await GetHighestDonorIdForWhichHlaHasBeenProcessed();
            
            using (var conn = new SqlConnection(connectionString))
            {
                var donors = conn.Query<Donor>($@"
SELECT * FROM Donors d
WHERE DonorId > {highestDonorId}
");
                return new SqlDonorBatchQueryAsync(donors);
            }
        }

        public async Task<DonorResult> GetDonor(int donorId)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                var donor = await conn.QuerySingleOrDefaultAsync<Donor>($"SELECT * FROM Donors WHERE DonorId = {donorId}");
                return donor?.ToDonorResult();
            }
        }

        /// <summary>
        /// Fetches all PGroups for a batch of donors from the MatchingHlaAt$Locus tables
        /// </summary>
        public async Task<IEnumerable<DonorIdWithPGroupNames>> GetPGroupsForDonors(IEnumerable<int> donorIds)
        {
            donorIds = donorIds.ToList();
            if (!donorIds.Any())
            {
                return new List<DonorIdWithPGroupNames>();
            }

            var results = donorIds
                .Select(id => new DonorIdWithPGroupNames {DonorId = id, PGroupNames = new PhenotypeInfo<IEnumerable<string>>()})
                .ToList();
            using (var conn = new SqlConnection(connectionString))
            {
                // TODO NOVA-1427: Do not fetch PGroups for loci that have already been matched at the DB level
                foreach (var locus in LocusSettings.MatchingOnlyLoci)
                {
                    var sql = $@"
SELECT m.DonorId, m.TypePosition, p.Name as PGroupName FROM {MatchingTableNameHelper.MatchingTableName(locus)} m
JOIN PGroupNames p 
ON m.PGroup_Id = p.Id
INNER JOIN (
    SELECT '{donorIds.FirstOrDefault()}' AS Id
    UNION ALL SELECT '{string.Join("' UNION ALL SELECT '", donorIds.Skip(1))}'
)
AS DonorIds 
ON m.DonorId = DonorIds.Id
";
                    var pGroups = await conn.QueryAsync<DonorMatchWithName>(sql, commandTimeout: 300);
                    foreach (var donorGroups in pGroups.GroupBy(p => p.DonorId))
                    {
                        foreach (var pGroupGroup in donorGroups.GroupBy(p => (TypePosition) p.TypePosition))
                        {
                            var donorResult = results.Single(r => r.DonorId == donorGroups.Key);
                            donorResult.PGroupNames.SetAtPosition(locus, pGroupGroup.Key, pGroupGroup.Select(p => p.PGroupName));
                        }
                    }
                }
            }

            return results;
        }

        public async Task<IEnumerable<DonorResult>> GetDonors(IEnumerable<int> donorIds)
        {
            donorIds = donorIds.ToList();
            if (!donorIds.Any())
            {
                return new List<DonorResult>();
            }

            using (var conn = new SqlConnection(connectionString))
            {
                var sql = $@"
SELECT * FROM Donors 
INNER JOIN (
    SELECT '{donorIds.FirstOrDefault()}' AS Id
    UNION ALL SELECT '{string.Join("' UNION ALL SELECT '", donorIds.Skip(1))}'
)
AS DonorIds 
ON DonorId = DonorIds.Id
";
                var donors = await conn.QueryAsync<Donor>(sql, commandTimeout: 300);
                return donors.Select(d => d.ToDonorResult());
            }
        }

        private async Task<int> GetHighestDonorIdForWhichHlaHasBeenProcessed()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                // A, B, and DRB1 should have entries for all donors, so we query the smallest of the three
                return await connection.QuerySingleOrDefaultAsync<int>(@"
SELECT TOP(1) DonorId FROM MatchingHlaAtDrb1 m
ORDER BY m.DonorId DESC
", 0);
            }
        }
    }
}