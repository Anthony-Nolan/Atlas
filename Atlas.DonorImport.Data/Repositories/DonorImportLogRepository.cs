using System;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.DonorImport.Data.Repositories
{
    public interface IDonorImportLogRepository
    {
        public Task<DateTime> GetLastUpdateForDonorWithId(string donorId);
        public Task SetLastUpdated(string donorId, DateTime lastUpdateTime);
    }
    
    public class DonorImportLogRepository : IDonorImportLogRepository
    {
        private string ConnectionString { get; set; }

        public DonorImportLogRepository(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        public async Task<DateTime> GetLastUpdateForDonorWithId(string donorId)
        {
            await using (var connection = new SqlConnection(ConnectionString))
            {
                const string sql = "SELECT LastUpdateDateTime FROM DonorLogs WHERE ExternalDonorId = (@DonorId)";
                return await connection.QuerySingleOrDefaultAsync<DateTime>(sql, new {DonorId = donorId});
            }
        }

        public async Task SetLastUpdated(string donorId, DateTime lastUpdateTime)
        {
            await using (var connection = new SqlConnection(ConnectionString))
            {
                const string donorExistsSql = "SELECT COUNT(*) FROM DonorLogs WHERE ExternalDonorId = (@DonorId)";
                var result = connection.QuerySingle<int>(donorExistsSql, new {DonorId = donorId});
                var querySql = result == 0
                    ? "INSERT INTO DonorLogs (ExternalDonorId, LastUpdateTime), VALUES ((@DonorId), (@LastUpdateTime))"
                    : "UPDATE DonorLogs SET LastUpdateTime = (@LastUpdateTime) WHERE ExternalDonorId = (@DonorId)";

                await connection.ExecuteAsync(querySql, new {DonorId = donorId, LastUpdateTime = lastUpdateTime});
            }
        }
        
        
    }
}