using Atlas.MatchPrediction.Test.Verification.Data.Context;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Verification;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Data.Repositories
{
    public interface IMatchedDonorsRepository
    {
        Task DeleteMatchedDonors(int searchRequestRecordId);
        Task BulkInsertMatchedDonors(IReadOnlyCollection<MatchedDonor> matchedDonors);

        /// <returns>Dictionary with key of <see cref="MatchedDonor.MatchedDonorSimulant_Id"/>and value of
        /// <see cref="MatchedDonor.Id"/></returns>
        Task<IDictionary<int, int>> GetMatchedDonorIdsBySimulantIds(int searchRequestRecordId, IEnumerable<int> donorSimulantIds);
    }

    public class MatchedDonorsRepository : IMatchedDonorsRepository
    {
        private readonly string connectionString;

        public MatchedDonorsRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task DeleteMatchedDonors(int searchRequestRecordId)
        {
            var sql = $@"DELETE FROM MatchedDonors WHERE SearchRequestRecord_Id = @{nameof(searchRequestRecordId)}";

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, new { searchRequestRecordId });
            }
        }

        public async Task BulkInsertMatchedDonors(IReadOnlyCollection<MatchedDonor> matchedDonors)
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

        public async Task<IDictionary<int, int>> GetMatchedDonorIdsBySimulantIds(int searchRequestRecordId, IEnumerable<int> donorSimulantIds)
        {
            var sql = @$"SELECT * FROM MatchedDonors WHERE
                SearchRequestRecord_Id = @{nameof(searchRequestRecordId)} AND
                MatchedDonorSimulant_Id IN @{nameof(donorSimulantIds)}";

            await using (var conn = new SqlConnection(connectionString))
            {
                return (await conn.QueryAsync<MatchedDonor>(sql, new { searchRequestRecordId, donorSimulantIds }))
                    .ToDictionary(m => m.MatchedDonorSimulant_Id, m => m.Id);
            }
        }

        private static DataTable BuildDataTable(IReadOnlyCollection<MatchedDonor> matchedDonors, IEnumerable<string> columnNames)
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
                    donor.WasPatientRepresented,
                    donor.WasDonorRepresented,
                    donor.SearchResult);
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
