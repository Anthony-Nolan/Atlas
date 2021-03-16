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
using MoreLinq.Extensions;

namespace Atlas.DonorImport.Data.Repositories
{
    public interface IDonorImportRepository
    {
        public Task InsertDonorBatch(IEnumerable<Donor> donors);
        Task UpdateDonorBatch(List<Donor> editedDonorsWithAtlasIds, DateTime updateTime);
        Task DeleteDonorBatch(List<int> deletedAtlasDonorIds);
    }

    public class DonorImportRepository : DonorRepositoryBase, IDonorImportRepository
    {
        private const int DonorImportBatchSize = 2000;

        /// <inheritdoc />
        public DonorImportRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task InsertDonorBatch(IEnumerable<Donor> donors)
        {
            donors = donors.ToList();
            if (!donors.Any())
            {
                return;
            }

            var dataTable = BuildDonorInsertDataTable(donors);

            using (var sqlBulk = BuildDonorSqlBulkCopy())
            {
                await sqlBulk.WriteToServerAsync(dataTable);
            }
        }

        /// <inheritdoc />
        public async Task UpdateDonorBatch(List<Donor> editedDonorsWithAtlasIds, DateTime updateTime)
        {
            var columnUpdateStrings =
                Donor.DbTableColumnNamesForUpdate
                    .Select(columnName => $"{columnName} = @{columnName}")
                    .StringJoin("," + Environment.NewLine);

            var sql = $@"
                UPDATE {Donor.QualifiedTableName}
                SET
                    {columnUpdateStrings}
                WHERE {nameof(Donor.AtlasId)} = @{nameof(Donor.AtlasId)}
                ";

            using (var transaction = new AsyncTransactionScope())
            {
                await using (var conn = NewConnection())
                {
                    conn.Open();
                    foreach (var donorEdit in editedDonorsWithAtlasIds)
                    {
                        await conn.ExecuteAsync(sql, donorEdit, commandTimeout: 600);
                    }

                    transaction.Complete();
                    conn.Close();
                }
            }
        }

        /// <inheritdoc />
        public async Task DeleteDonorBatch(List<int> deletedAtlasDonorIds)
        {
            var sql = @$"
                DELETE FROM {Donor.QualifiedTableName}
                WHERE {nameof(Donor.AtlasId)} IN @Ids
                ";

            await using (var connection = NewConnection())
            {
                foreach (var atlasIds in deletedAtlasDonorIds.Batch(DonorImportBatchSize))
                {
                    await connection.ExecuteAsync(sql, new {Ids = atlasIds.ToList()});
                }
            }
        }

        private SqlBulkCopy BuildDonorSqlBulkCopy()
        {
            var sqlBulk = new SqlBulkCopy(ConnectionString)
                {BulkCopyTimeout = 300, BatchSize = 10000, DestinationTableName = Donor.QualifiedTableName};

            foreach (var columnName in Donor.DataTableColumnNamesForInsertion)
            {
                // Relies on setting up the data table with column names matching the database columns.
                sqlBulk.ColumnMappings.Add(columnName, columnName);
            }

            return sqlBulk;
        }

        private DataTable BuildDonorInsertDataTable(IEnumerable<Donor> donors)
        {
            var dataTable = new DataTable();
            foreach (var columnName in Donor.DataTableColumnNamesForInsertion)
            {
                dataTable.Columns.Add(columnName);
            }

            foreach (var donor in donors)
            {
                dataTable.Rows.Add(
                    0,
                    donor.ExternalDonorCode,
                    (int) donor.DonorType,
                    donor.EthnicityCode,
                    donor.RegistryCode,
                    donor.A_1,
                    donor.A_2,
                    donor.B_1,
                    donor.B_2,
                    donor.C_1,
                    donor.C_2,
                    donor.DPB1_1,
                    donor.DPB1_2,
                    donor.DQB1_1,
                    donor.DQB1_2,
                    donor.DRB1_1,
                    donor.DRB1_2,
                    donor.Hash,
                    donor.UpdateFile,
                    donor.LastUpdated);
            }

            return dataTable;
        }
    }
}