using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using MoreLinq;

namespace Atlas.DonorImport.Data.Repositories
{
    public interface IDonorImportLogRepository
    {
        public Task<IReadOnlyDictionary<string, DateTime>> GetLastUpdatedTimes(IReadOnlyCollection<string> externalDonorCodes);

        Task SetLastUpdatedBatch(IEnumerable<string> externalDonorCodes, DateTime lastUpdateTime);

        public Task DeleteDonorLogBatch(IReadOnlyCollection<string> externalDonorCodes);
    }

    public class DonorImportLogRepository : IDonorImportLogRepository
    {
        private string ConnectionString { get; }

        private const string DonorLogTableName = "DonorLogs";

        private const string ExternalDonorCodeColumnName = "ExternalDonorCode";
        private const string LastUpdatedColumnName = "LastUpdateFileUploadTime";

        private const int DonorImportBatchSize = 2000;

        public DonorImportLogRepository(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public async Task<IReadOnlyDictionary<string, DateTime>> GetLastUpdatedTimes(IReadOnlyCollection<string> externalDonorCodes)
        {
            await using (var connection = new SqlConnection(ConnectionString))
            {
                var sql = $"SELECT {ExternalDonorCodeColumnName}, {LastUpdatedColumnName} FROM {DonorLogTableName} WHERE {ExternalDonorCodeColumnName} IN @externalDonorCodes";
                var donorLogs = await connection.QueryAsync<DonorLog>(sql, new {externalDonorCodes}, commandTimeout: 600);
                return donorLogs.ToDictionary(d => d.ExternalDonorCode, d => d.LastUpdateFileUploadTime);
            }
        }

        public async Task SetLastUpdatedBatch(IEnumerable<string> externalDonorCodes, DateTime lastUpdateTime)
        {
            await using (var connection = new SqlConnection(ConnectionString))
            {
                var donorCodes = externalDonorCodes.ToList();

                connection.Open();
                var sql = $@"SELECT {ExternalDonorCodeColumnName} FROM {DonorLogTableName} WHERE {ExternalDonorCodeColumnName} IN @ExternalDonorCodes";

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

        public async Task DeleteDonorLogBatch(IReadOnlyCollection<string> externalDonorCodes)
        {
            await using (var connection = new SqlConnection(ConnectionString))
            {
                var sql = $"DELETE FROM {DonorLogTableName} WHERE {ExternalDonorCodeColumnName} IN @externalDonorCodesBatch";
                connection.Open();

                foreach (var externalDonorCodesBatch in externalDonorCodes.Batch(DonorImportBatchSize))
                {
                    await connection.ExecuteAsync(sql, new { externalDonorCodesBatch });
                }

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

            foreach (var codes in donorCodes.Batch(DonorImportBatchSize))
            {
                await connection.ExecuteAsync(sql, new {lastUpdateTime, externalDonorCodes = codes});
            }
        }
    }
}