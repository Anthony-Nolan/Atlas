using Atlas.MatchPrediction.Test.Verification.Data.Context;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Atlas.Common.Sql.BulkInsert;

namespace Atlas.MatchPrediction.Test.Verification.Data.Repositories
{
    public interface IMatchedDonorsRepository
    {
        Task<int?> GetMatchedDonorId(int searchRequestRecordId, int simulantId);
    }

    public class MatchedDonorsRepository : IMatchedDonorsRepository, IProcessedResultsRepository<MatchedDonor>
    {
        private readonly string connectionString;

        public MatchedDonorsRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task DeleteResults(int searchRequestRecordId)
        {
            var sql = $@"DELETE FROM MatchedDonors WHERE SearchRequestRecord_Id = @{nameof(searchRequestRecordId)}";

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, new { searchRequestRecordId });
            }
        }

        public async Task BulkInsertResults(IReadOnlyCollection<MatchedDonor> matchedDonors)
        {
            if (!matchedDonors.Any())
            {
                return;
            }

            var columnNames = matchedDonors.GetColumnNamesForBulkInsert();
            var dataTable = BuildDataTable(matchedDonors, columnNames);

            using (var sqlBulk = BuildSqlBulkCopy(columnNames))
            {
                await sqlBulk.WriteToServerAsync(dataTable);
            }
        }

        public async Task<int?> GetMatchedDonorId(int searchRequestRecordId, int simulantId)
        {
            var sql = @$"SELECT Id FROM MatchedDonors WHERE 
                SearchRequestRecord_Id = @{nameof(searchRequestRecordId)} AND
                MatchedDonorSimulant_Id = @{nameof(simulantId)}";

            await using (var conn = new SqlConnection(connectionString))
            {
                return (await conn.QueryAsync<int>(sql, new { searchRequestRecordId, simulantId })).SingleOrDefault();
            }
        }

        private static DataTable BuildDataTable(IEnumerable<MatchedDonor> matchedDonors, IEnumerable<string> columnNames)
        {
            var dataTable = new DataTable();
            foreach (var columnName in columnNames)
            {
                dataTable.Columns.Add(columnName);
            }

            foreach (var donor in matchedDonors)
            {
                dataTable.Rows.Add(
                    donor.SearchRequestRecord_Id,
                    donor.MatchedDonorSimulant_Id,
                    donor.TotalMatchCount,
                    donor.TypedLociCount,
                    donor.WasPatientRepresented,
                    donor.WasDonorRepresented,
                    donor.MatchingResult);
            }

            return dataTable;
        }

        private SqlBulkCopy BuildSqlBulkCopy(IEnumerable<string> columnNames)
        {
            var sqlBulk = new SqlBulkCopy(connectionString)
            {
                BulkCopyTimeout = 3600,
                BatchSize = 10000,
                DestinationTableName = nameof(MatchPredictionVerificationContext.MatchedDonors)
            };

            foreach (var columnName in columnNames)
            {
                // Relies on setting up the data table with column names matching the database columns.
                sqlBulk.ColumnMappings.Add(columnName, columnName);
            }

            return sqlBulk;
        }
    }
}
