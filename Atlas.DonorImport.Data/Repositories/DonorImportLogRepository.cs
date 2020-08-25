using System;
using System.Data;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.DonorImport.Data.Repositories
{
    public interface IDonorImportLogRepository
    {
        public Task<DateTime> GetLastUpdateForDonorWithId(string externalDonorCode);
        public Task SetLastUpdated(string externalDonorCode, DateTime lastUpdateTime);
        public Task<bool> CheckDonorExists(string externalDonorCode);
        public Task DeleteDonorLog(string externalDonorCode);
    }

    public class DonorImportLogRepository : IDonorImportLogRepository
    {
        private string ConnectionString { get; }
        private const string ExternalDonorCodeColumnName = "ExternalDonorCode";
        private const string LastUpdatedColumnName = "LastUpdateFileUploadTime";
        private const string DonorLogTableName = "DonorLogs";

        public DonorImportLogRepository(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public async Task<DateTime> GetLastUpdateForDonorWithId(string externalDonorCode)
        {
            await using (var connection = new SqlConnection(ConnectionString))
            {
                var sql = $"SELECT {LastUpdatedColumnName} FROM {DonorLogTableName} WHERE {ExternalDonorCodeColumnName} = (@externalDonorCode)";
                return await connection.QuerySingleOrDefaultAsync<DateTime>(sql, new {externalDonorCode});
            }
        }

        public async Task SetLastUpdated(string externalDonorCode, DateTime lastUpdateTime)
        {
            await using (var connection = new SqlConnection(ConnectionString))
            {
                var donorExistsSql = $"SELECT COUNT(*) FROM {DonorLogTableName} WHERE {ExternalDonorCodeColumnName} = (@externalDonorCode)";
                var result = connection.QuerySingle<int>(donorExistsSql, new {externalDonorCode});
                var querySql = result == 0
                    ? $"INSERT INTO {DonorLogTableName} ({ExternalDonorCodeColumnName}, {LastUpdatedColumnName}) VALUES ((@externalDonorCode), (@LastUpdateTime))"
                    : $"UPDATE {DonorLogTableName} SET {LastUpdatedColumnName} = (@LastUpdateTime) WHERE {ExternalDonorCodeColumnName} = (@externalDonorCode)";

                await connection.ExecuteAsync(querySql, new {externalDonorCode, LastUpdateTime = lastUpdateTime});
            }
        }

        public async Task<bool> CheckDonorExists(string externalDonorCode)
        {
            await using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var sql = $"SELECT COUNT(1) FROM {DonorLogTableName} WHERE {ExternalDonorCodeColumnName} = (@externalDonorCode)";
                var result = connection.QuerySingleOrDefault<bool>(sql, new {externalDonorCode});
                connection.Close();
                return result;
            }
        }

        public async Task DeleteDonorLog(string externalDonorCode)
        {
            await using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var sql = $"DELETE FROM {DonorLogTableName} WHERE {ExternalDonorCodeColumnName} = (@externalDonorCode)";
                await connection.ExecuteAsync(sql, new {externalDonorCode});
                connection.Close();
            }
        }
    }
}