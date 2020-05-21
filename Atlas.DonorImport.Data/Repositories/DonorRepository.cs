using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Models;
using Microsoft.Data.SqlClient;

namespace Atlas.DonorImport.Data.Repositories
{
    public interface IDonorRepository
    {
        public Task InsertDonorBatch(IEnumerable<Donor> donors);
    }

    public class DonorRepository : IDonorRepository
    {
        private readonly string connectionString;

        public DonorRepository(string connectionString)
        {
            this.connectionString = connectionString;
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
            var sqlBulk = new SqlBulkCopy(connectionString) {BulkCopyTimeout = 3600, BatchSize = 10000, DestinationTableName = "Donors"};
            
            sqlBulk.ColumnMappings.Add(nameof(Donor.Id), nameof(Donor.Id));
            sqlBulk.ColumnMappings.Add(nameof(Donor.DonorId), nameof(Donor.DonorId));
            sqlBulk.ColumnMappings.Add(nameof(Donor.DonorType), nameof(Donor.DonorType));
            sqlBulk.ColumnMappings.Add(nameof(Donor.EthnicityCode), nameof(Donor.EthnicityCode));
            sqlBulk.ColumnMappings.Add(nameof(Donor.RegistryCode), nameof(Donor.RegistryCode));
            sqlBulk.ColumnMappings.Add(nameof(Donor.A_1), nameof(Donor.A_1));
            sqlBulk.ColumnMappings.Add(nameof(Donor.A_2), nameof(Donor.A_2));
            sqlBulk.ColumnMappings.Add(nameof(Donor.B_1), nameof(Donor.B_1));
            sqlBulk.ColumnMappings.Add(nameof(Donor.B_2), nameof(Donor.B_2));
            sqlBulk.ColumnMappings.Add(nameof(Donor.C_1), nameof(Donor.C_1));
            sqlBulk.ColumnMappings.Add(nameof(Donor.C_2), nameof(Donor.C_2));
            sqlBulk.ColumnMappings.Add(nameof(Donor.DPB1_1), nameof(Donor.DPB1_1));
            sqlBulk.ColumnMappings.Add(nameof(Donor.DPB1_2), nameof(Donor.DPB1_2));
            sqlBulk.ColumnMappings.Add(nameof(Donor.DQB1_1), nameof(Donor.DQB1_1));
            sqlBulk.ColumnMappings.Add(nameof(Donor.DQB1_2), nameof(Donor.DQB1_2));
            sqlBulk.ColumnMappings.Add(nameof(Donor.DRB1_1), nameof(Donor.DRB1_1));
            sqlBulk.ColumnMappings.Add(nameof(Donor.DRB1_2), nameof(Donor.DRB1_2));
            sqlBulk.ColumnMappings.Add(nameof(Donor.Hash), nameof(Donor.Hash));
            
            return sqlBulk;
        }

        private static DataTable BuildDonorInsertDataTable(IEnumerable<Donor> donors)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add(nameof(Donor.Id));
            dataTable.Columns.Add(nameof(Donor.DonorId));
            dataTable.Columns.Add(nameof(Donor.DonorType));
            dataTable.Columns.Add(nameof(Donor.EthnicityCode));
            dataTable.Columns.Add(nameof(Donor.RegistryCode));
            dataTable.Columns.Add(nameof(Donor.A_1));
            dataTable.Columns.Add(nameof(Donor.A_2));
            dataTable.Columns.Add(nameof(Donor.B_1));
            dataTable.Columns.Add(nameof(Donor.B_2));
            dataTable.Columns.Add(nameof(Donor.C_1));
            dataTable.Columns.Add(nameof(Donor.C_2));
            dataTable.Columns.Add(nameof(Donor.DPB1_1));
            dataTable.Columns.Add(nameof(Donor.DPB1_2));
            dataTable.Columns.Add(nameof(Donor.DQB1_1));
            dataTable.Columns.Add(nameof(Donor.DQB1_2));
            dataTable.Columns.Add(nameof(Donor.DRB1_1));
            dataTable.Columns.Add(nameof(Donor.DRB1_2));
            dataTable.Columns.Add(nameof(Donor.Hash));

            foreach (var donor in donors)
            {
                dataTable.Rows.Add(
                    0,
                    donor.DonorId,
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