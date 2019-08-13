using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Data.Services;

namespace Nova.SearchAlgorithm.Data.Repositories
{
    public class DataRefreshRepository : Repository, IDataRefreshRepository
    {
        public const int NumberOfBatchesOverlapOnRestart = 2;

        public DataRefreshRepository(IConnectionStringProvider connectionStringProvider) : base(connectionStringProvider)
        {
        }

        public async Task<int> HighestDonorId()
        {
            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                return (await conn.QueryAsync<int>("SELECT TOP (1) DonorId FROM Donors ORDER BY DonorId DESC")).SingleOrDefault();
            }
        }

        public async Task<IBatchQueryAsync<DonorResult>> DonorsAddedSinceLastHlaUpdate(int batchSize)
        {
            var highestDonorId = await GetHighestDonorIdForWhichHlaHasBeenProcessed();

            // Continue from an earlier donor than the highest imported donor id, in case hla processing was only successful for some loci for previous batch
            var donorToContinueFrom = highestDonorId - (batchSize * NumberOfBatchesOverlapOnRestart);

            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                var donors = conn.Query<Donor>($@"
SELECT * FROM Donors d
WHERE DonorId > {donorToContinueFrom}
", commandTimeout: 3600);
                return new SqlDonorBatchQueryAsync(donors, batchSize);
            }
        }

        public async Task<int> GetDonorCount()
        {
            const string sql = @"
SELECT COUNT(*) FROM DONORS
";

            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                return await conn.QueryFirstAsync<int>(sql, 600);
            }
        }

        private async Task<int> GetHighestDonorIdForWhichHlaHasBeenProcessed()
        {
            // While this query is expected to be quite fast, throttling of azure databases can slow down all requests when in heavy use
            const int timeout = 600;

            using (var connection = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                var maxDonorIdAtDrb1 = await connection.QuerySingleOrDefaultAsync<int?>(@"
SELECT MAX(DonorId) FROM MatchingHlaAtDrb1
", 0, commandTimeout: timeout) ?? 0;
                var maxDonorIdAtB = await connection.QuerySingleOrDefaultAsync<int?>(@"
SELECT MAX(DonorId) FROM MatchingHlaAtB
", 0, commandTimeout: timeout) ?? 0;
                var maxDonorIdAtA = await connection.QuerySingleOrDefaultAsync<int?>(@"
SELECT MAX(DonorId) FROM MatchingHlaAtA
", 0, commandTimeout: timeout) ?? 0;

                return Math.Min(maxDonorIdAtA, Math.Min(maxDonorIdAtB, maxDonorIdAtDrb1));
            }
        }
    }
}