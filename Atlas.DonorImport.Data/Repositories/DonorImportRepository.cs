using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Models;
using Microsoft.Data.SqlClient;

namespace Atlas.DonorImport.Data.Repositories
{
    public interface IDonorImportRepository
    {
        public Task InsertDonorBatch(IEnumerable<Donor> donors);
    }

    public class DonorImportRepository : DonorRepositoryBase, IDonorImportRepository
    {
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

            using var sqlBulk = BuildDonorSqlBulkCopy();
            await sqlBulk.WriteToServerAsync(dataTable);
        }

        private SqlBulkCopy BuildDonorSqlBulkCopy()
        {
            var sqlBulk = new SqlBulkCopy(ConnectionString) {BulkCopyTimeout = 3600, BatchSize = 10000, DestinationTableName = "Donors"};

            foreach (var columnName in DonorInsertDataTableColumnNames)
            {
                // Relies on setting up the data table with column names matching the database columns.
                sqlBulk.ColumnMappings.Add(columnName, columnName);
            }

            return sqlBulk;
        }

        private DataTable BuildDonorInsertDataTable(IEnumerable<Donor> donors)
        {
            var dataTable = new DataTable();
            foreach (var columnName in DonorInsertDataTableColumnNames)
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
                    donor.Hash);
            }

            return dataTable;
        }
    }
}