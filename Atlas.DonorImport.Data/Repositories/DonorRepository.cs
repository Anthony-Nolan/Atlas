using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.DonorImport.Data.Repositories
{
    public interface IDonorRepository
    {
        public Task InsertDonorBatch(IEnumerable<Donor> donors);

        public IEnumerable<Donor> GetAllDonors();
    }

    public class DonorRepository : IDonorRepository
    {
        private readonly string connectionString;

        // The order of these matters when setting up the datatable - if re-ordering, also re-order datatable contents
        private readonly string[] donorInsertDataTableColumnNames = {
            nameof(Donor.Id),
            nameof(Donor.DonorId),
            nameof(Donor.DonorType),
            nameof(Donor.EthnicityCode),
            nameof(Donor.RegistryCode),
            nameof(Donor.A_1),
            nameof(Donor.A_2),
            nameof(Donor.B_1),
            nameof(Donor.B_2),
            nameof(Donor.C_1),
            nameof(Donor.C_2),
            nameof(Donor.DPB1_1),
            nameof(Donor.DPB1_2),
            nameof(Donor.DQB1_1),
            nameof(Donor.DQB1_2),
            nameof(Donor.DRB1_1),
            nameof(Donor.DRB1_2),
            nameof(Donor.Hash)
        };

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

        public IEnumerable<Donor> GetAllDonors()
        {
            var sql = $"SELECT {string.Join(", ", donorInsertDataTableColumnNames)} FROM Donors";
            using var connection = new SqlConnection(connectionString);
            // TODO: ATLAS-186: Determine whether it would be better to switch off "buffered" here, essentially streaming the data. 
            // Pro: Smaller memory footprint.
            // Con: Longer open connection, consumer can cause timeouts by not fully enumerating.
            return connection.Query<Donor>(sql, buffered: true);
        }

        private SqlBulkCopy BuildDonorSqlBulkCopy()
        {
            var sqlBulk = new SqlBulkCopy(connectionString) {BulkCopyTimeout = 3600, BatchSize = 10000, DestinationTableName = "Donors"};

            foreach (var columnName in donorInsertDataTableColumnNames)
            {
                // Relies on setting up the data table with column names matching the database columns.
                sqlBulk.ColumnMappings.Add(columnName, columnName);
            }
            
            return sqlBulk;
        }

        private DataTable BuildDonorInsertDataTable(IEnumerable<Donor> donors)
        {
            var dataTable = new DataTable();
            foreach (var columnName in donorInsertDataTableColumnNames)
            {
                dataTable.Columns.Add(columnName);
            }

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