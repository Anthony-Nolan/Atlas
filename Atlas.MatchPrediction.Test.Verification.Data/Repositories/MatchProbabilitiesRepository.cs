using Atlas.MatchPrediction.Test.Verification.Data.Context;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Dapper;

namespace Atlas.MatchPrediction.Test.Verification.Data.Repositories
{
    public class MatchProbabilitiesRepository : IProcessedResultsRepository<MatchProbability>
    {
        private readonly string connectionString;

        public MatchProbabilitiesRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task DeleteResults(int searchRequestRecordId)
        {
            var sql = $@"
                DELETE FROM MatchProbabilities
                FROM MatchProbabilities m
                JOIN MatchedDonors d
                ON m.MatchedDonor_Id = d.Id
                WHERE d.SearchRequestRecord_Id = @{nameof(searchRequestRecordId)}";

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, new { searchRequestRecordId });
            }
        }

        public async Task BulkInsertResults(IReadOnlyCollection<MatchProbability> matchProbabilities)
        {
            if (!matchProbabilities.Any())
            {
                return;
            }

            var columnNames = matchProbabilities.GetColumnNamesForBulkInsert();
            var dataTable = BuildDataTable(matchProbabilities, columnNames);

            using (var sqlBulk = BuildSqlBulkCopy(columnNames))
            {
                await sqlBulk.WriteToServerAsync(dataTable);
            }
        }

        private static DataTable BuildDataTable(IReadOnlyCollection<MatchProbability> matchProbabilities, IEnumerable<string> columnNames)
        {
            var dataTable = new DataTable();
            foreach (var columnName in columnNames)
            {
                dataTable.Columns.Add(columnName);
            }

            foreach (var probability in matchProbabilities)
            {
                dataTable.Rows.Add(
                    probability.MatchedDonor_Id,
                    probability.Locus,
                    probability.MismatchCount,
                    probability.Probability);
            }

            return dataTable;
        }

        private SqlBulkCopy BuildSqlBulkCopy(IEnumerable<string> columnNames)
        {
            var sqlBulk = new SqlBulkCopy(connectionString)
            {
                BulkCopyTimeout = 3600,
                BatchSize = 10000,
                DestinationTableName = nameof(MatchPredictionVerificationContext.MatchProbabilities)
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
