using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.Common.Utils.Concurrency;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using MoreLinq;

namespace Atlas.DonorImport.Data.Repositories
{
    public interface IDonorImportLogRepository
    {
        public Task<DateTime> GetLastUpdateForDonorWithId(string externalDonorCode);

        Task SetLastUpdatedBatch(IEnumerable<string> externalDonorCodes, DateTime lastUpdateTime);

        public Task<bool> CheckDonorExists(string externalDonorCode);
        public Task DeleteDonorLogBatch(IReadOnlyCollection<string> externalDonorCodes);
    }

    public class DonorImportLogRepository : IDonorImportLogRepository
    {
        private string ConnectionString { get; }

        private const string DonorLogTableName = "DonorLogs";

        private const string ExternalDonorCodeColumnName = "ExternalDonorCode";
        private const string LastUpdatedColumnName = "LastUpdateFileUploadTime";

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

        public async Task SetLastUpdatedBatch(IEnumerable<string> externalDonorCodes, DateTime lastUpdateTime)
        {
            await using (var connection = new SqlConnection(ConnectionString))
            {
                var donorCodes = externalDonorCodes.ToList();

                connection.Open();
                const string sql = @"SELECT ExternalDonorCode FROM DonorLogs WHERE ExternalDonorCode IN @ExternalDonorCodes";

                var existingRecords = await donorCodes.ProcessInBatchesAsync(
                    2000,
                    async codes => await connection.QueryAsync<string>(sql, new {ExternalDonorCodes = codes.ToList()})
                );

                var (donorsToUpdate, donorsToInsert) = donorCodes.ReifyAndSplit(d => existingRecords.Contains(d));

                await InsertDonorBatch(connection, donorsToInsert, lastUpdateTime);
                await UpdateDonorBatch(connection, donorsToUpdate, lastUpdateTime);

                connection.Close();
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

        public async Task DeleteDonorLogBatch(IReadOnlyCollection<string> externalDonorCodes)
        {
            await using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var sql = $"DELETE FROM {DonorLogTableName} WHERE {ExternalDonorCodeColumnName} IN @externalDonorCodes";
                await connection.ExecuteAsync(sql, new {externalDonorCodes});
                connection.Close();
            }
        }

        private static async Task InsertDonorBatch(SqlConnection connection, IEnumerable<string> donorCodes, DateTime lastUpdateTime)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add(ExternalDonorCodeColumnName);
            dataTable.Columns.Add(LastUpdatedColumnName);
            foreach (var donorCode in donorCodes)
            {
                dataTable.Rows.Add(donorCode, lastUpdateTime);
            }

            using (var sqlBulk = new SqlBulkCopy(connection) {BulkCopyTimeout = 3600, BatchSize = 10000, DestinationTableName = "DonorLogs"})
            {
                sqlBulk.ColumnMappings.Add(ExternalDonorCodeColumnName, ExternalDonorCodeColumnName);
                sqlBulk.ColumnMappings.Add(LastUpdatedColumnName, LastUpdatedColumnName);

                await sqlBulk.WriteToServerAsync(dataTable);
            }
        }

        private static async Task UpdateDonorBatch(SqlConnection connection, IReadOnlyCollection<string> donorCodes, DateTime lastUpdateTime)
        {
            var sql =
                $"UPDATE {DonorLogTableName} SET {LastUpdatedColumnName} = (@lastUpdateTime) WHERE {ExternalDonorCodeColumnName} IN @externalDonorCodes";
            await connection.ExecuteAsync(sql, new {lastUpdateTime, externalDonorCodes = donorCodes});
        }
    }
}