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
    public class MatchCountsRepository : IProcessedSearchResultsRepository<LocusMatchCount>
    {
        private readonly string connectionString;

        public MatchCountsRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task DeleteResults(int searchRequestRecordId)
        {
            var sql = $@"
                DELETE FROM MatchCounts
                FROM MatchCounts c
                JOIN MatchedDonors d
                ON c.MatchedDonor_Id = d.Id
                WHERE d.SearchRequestRecord_Id = @{nameof(searchRequestRecordId)}";

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, new { searchRequestRecordId });
            }
        }

        public async Task BulkInsertResults(IReadOnlyCollection<LocusMatchCount> matchCounts)
        {
            if (!matchCounts.Any())
            {
                return;
            }

            var columnNames = matchCounts.GetColumnNamesForBulkInsert();
            var dataTable = BuildDataTable(matchCounts, columnNames);

            using (var sqlBulk = BuildSqlBulkCopy(columnNames))
            {
                await sqlBulk.WriteToServerAsync(dataTable);
            }
        }

        private static DataTable BuildDataTable(IEnumerable<LocusMatchCount> matchCounts, IEnumerable<string> columnNames)
        {
            var dataTable = new DataTable();
            foreach (var columnName in columnNames)
            {
                dataTable.Columns.Add(columnName);
            }

            foreach (var probability in matchCounts)
            {
                dataTable.Rows.Add(
                    probability.MatchedDonor_Id,
                    probability.Locus,
                    probability.MatchCount);
            }

            return dataTable;
        }

        private SqlBulkCopy BuildSqlBulkCopy(IEnumerable<string> columnNames)
        {
            var sqlBulk = new SqlBulkCopy(connectionString)
            {
                BulkCopyTimeout = 3600,
                BatchSize = 10000,
                DestinationTableName = nameof(MatchPredictionVerificationContext.MatchCounts)
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
