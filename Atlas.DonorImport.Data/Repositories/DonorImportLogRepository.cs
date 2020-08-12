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
                const string sql = "SELECT LastUpdateFileUploadTime FROM DonorLogs WHERE ExternalDonorCode = (@DonorId)";
                return await connection.QuerySingleOrDefaultAsync<DateTime>(sql, new {DonorId = donorId});
            }
        }

        public async Task SetLastUpdated(string donorId, DateTime lastUpdateTime)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                const string donorExistsSql = "SELECT COUNT(*) FROM DonorLogs WHERE ExternalDonorCode = (@DonorId)";
                var result = connection.QuerySingle<int>(donorExistsSql, new {DonorId = donorId});
                var querySql = result == 0
                    ? "INSERT INTO DonorLogs (ExternalDonorCode, LastUpdateFileUploadTime) VALUES ((@DonorId), (@LastUpdateTime))"
                    : "UPDATE DonorLogs SET LastUpdateFileUploadTime = (@LastUpdateTime) WHERE ExternalDonorCode = (@DonorId)";

                await connection.ExecuteAsync(querySql, new {DonorId = donorId, LastUpdateTime = lastUpdateTime});
            }
        }
    }
}