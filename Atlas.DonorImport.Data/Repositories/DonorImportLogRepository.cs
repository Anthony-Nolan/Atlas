using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.DonorImport.Data.Repositories
{
    public interface IDonorImportLogRepository
    {
        public Task<DateTime> GetLastUpdateForDonorWithId(string donorId);
        public Task SetLastUpdated(string donorId, DateTime lastUpdateTime);
        public Task SetLastUpdatedByBatch(IEnumerable<string> donorBatch, DateTime lastUpdateTime);
    }

    public class DonorImportLogRepository : IDonorImportLogRepository
    {
        private string ConnectionString { get; set; }
        private const string ExternalDonorCodeColumnName = "ExternalDonorCode";
        private const string LastUpdatedColumnName = "LastUpdateFileUploadTime";
        private const string DonorLogTableName = "DonorLogs";

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

        public async Task SetLastUpdatedByBatch(IEnumerable<string> donorIdBatch, DateTime lastUpdateTime)
        {
            await using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                const string sql = "SELECT ExternalDonorCode FROM DonorLogs";
                var result = connection.Query<string>(sql);
                var (donorsToUpdate, donorsToInsert) = donorIdBatch.ReifyAndSplit(d => result.Contains(d));
                await InsertDonorBatch(connection, donorsToInsert, lastUpdateTime);
                await UpdateDonorBatch(connection, donorsToUpdate, lastUpdateTime);
                connection.Close();
            }
        }

        private async Task InsertDonorBatch(SqlConnection connection, IEnumerable<string> donorsToInsert, DateTime lastUpdateTime)
        {

            var sql = $"INSERT INTO {DonorLogTableName} ({ExternalDonorCodeColumnName}, {LastUpdatedColumnName}) VALUES ((@donorId), (@lastUpdate))";
            foreach (var donor in donorsToInsert)
            {
                await connection.ExecuteAsync(sql, new {lastUpdate = lastUpdateTime, donorId = donor});
            }
        }

        private static async Task UpdateDonorBatch(SqlConnection connection, IReadOnlyCollection<string> donorsToUpdate, DateTime lastUpdateTime)
        {
            var sql = $"UPDATE {DonorLogTableName} SET {LastUpdatedColumnName} = (@lastUpdateTime) WHERE {ExternalDonorCodeColumnName} = (@donorId)";
            foreach (var donor in donorsToUpdate)
            {
                await connection.ExecuteAsync(sql, new {lastUpdateTime, donorId = donor});
            }
        }
    }
}