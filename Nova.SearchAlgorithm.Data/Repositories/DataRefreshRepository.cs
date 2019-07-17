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
    public class DataRefreshRepository : IDataRefreshRepository
    {
        private readonly IConnectionStringProvider connectionStringProvider;
        
        public DataRefreshRepository(IConnectionStringProvider connectionStringProvider)
        {
            this.connectionStringProvider = connectionStringProvider;
        }
        
        public async Task<int> HighestDonorId()
        {
            using (var conn = new SqlConnection(connectionStringProvider.GetConnectionString()))
            {
                return (await conn.QueryAsync<int>("SELECT TOP (1) DonorId FROM Donors ORDER BY DonorId DESC")).SingleOrDefault();
            }
        }

        public async Task<IBatchQueryAsync<DonorResult>> DonorsAddedSinceLastHlaUpdate()
        {
            var highestDonorId = await GetHighestDonorIdForWhichHlaHasBeenProcessed();
            
            using (var conn = new SqlConnection(connectionStringProvider.GetConnectionString()))
            {
                var donors = conn.Query<Donor>($@"
SELECT * FROM Donors d
WHERE DonorId > {highestDonorId}
");
                return new SqlDonorBatchQueryAsync(donors);
            }
        }

        private async Task<int> GetHighestDonorIdForWhichHlaHasBeenProcessed()
        {
            using (var connection = new SqlConnection(connectionStringProvider.GetConnectionString()))
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