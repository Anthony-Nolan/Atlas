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
            var donorToContinueFrom = highestDonorId - (batchSize * 2);

            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                var donors = conn.Query<Donor>($@"
SELECT * FROM Donors d
WHERE DonorId > {donorToContinueFrom}
");
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
                return await conn.QueryFirstAsync<int>(sql);
            }
        }

        private async Task<int> GetHighestDonorIdForWhichHlaHasBeenProcessed()
        {
            using (var connection = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                var maxDonorIdAtDrb1 = await connection.QuerySingleOrDefaultAsync<int>(@"
SELECT TOP(1) DonorId FROM MatchingHlaAtDrb1 m
ORDER BY m.DonorId DESC
", 0);
                var maxDonorIdAtB = await connection.QuerySingleOrDefaultAsync<int>(@"
SELECT TOP(1) DonorId FROM MatchingHlaAtB m
ORDER BY m.DonorId DESC
", 0);
                var maxDonorIdAtA = await connection.QuerySingleOrDefaultAsync<int>(@"
SELECT TOP(1) DonorId FROM MatchingHlaAtA m
ORDER BY m.DonorId DESC
", 0);

                return Math.Min(maxDonorIdAtA, Math.Min(maxDonorIdAtB, maxDonorIdAtDrb1));
            }
        }
    }
}