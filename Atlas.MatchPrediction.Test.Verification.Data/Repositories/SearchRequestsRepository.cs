using Atlas.MatchPrediction.Test.Verification.Data.Context;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Dapper;

namespace Atlas.MatchPrediction.Test.Verification.Data.Repositories
{
    public interface ISearchRequestsRepository
    {
        Task AddSearchRequest(SearchRequestRecord request);
        Task<int> GetRecordIdByAtlasSearchId(string atlasSearchId);
        Task MarkSearchResultsAsRetrieved(int searchRequestRecordId, int? matchedDonorCount, bool wasSuccessful);
    }

    public class SearchRequestsRepository : ISearchRequestsRepository
    {
        private readonly string connectionString;

        public SearchRequestsRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task AddSearchRequest(SearchRequestRecord request)
        {
            var sql = $@"INSERT INTO SearchRequests(
                VerificationRun_Id,
                PatientSimulant_Id,
                AtlasSearchIdentifier)
                VALUES(
                    @{nameof(request.VerificationRun_Id)},
                    @{nameof(request.PatientSimulant_Id)},
                    @{nameof(request.AtlasSearchIdentifier)}
                )";

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, new
                {
                    request.VerificationRun_Id,
                    request.PatientSimulant_Id,
                    request.AtlasSearchIdentifier
                });
            }
        }

        public async Task<int> GetRecordIdByAtlasSearchId(string atlasSearchId)
        {
            var sql = @$"SELECT s.Id FROM SearchRequests s WHERE s.AtlasSearchIdentifier = @{nameof(atlasSearchId)}";

            await using (var conn = new SqlConnection(connectionString))
            {
                return (await conn.QueryAsync<int>(sql, new { atlasSearchId })).SingleOrDefault();
            }
        }

        public async Task MarkSearchResultsAsRetrieved(int searchRequestRecordId, int? matchedDonorCount, bool wasSuccessful)
        {
            var sql = $@"UPDATE SearchRequests SET 
                SearchResultsRetrieved = 1,
                MatchedDonorCount = @{nameof(matchedDonorCount)},
                WasSuccessful = @{nameof(wasSuccessful)}
                WHERE Id = @{nameof(searchRequestRecordId)}";

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, new { matchedDonorCount, wasSuccessful, searchRequestRecordId });
            }
        }

        private static DataTable BuildDataTable(IReadOnlyCollection<SearchRequestRecord> records, IEnumerable<string> columnNames)
        {
            var dataTable = new DataTable();
            foreach (var columnName in columnNames)
            {
                dataTable.Columns.Add(columnName);
            }

            foreach (var record in records)
            {
                dataTable.Rows.Add(
                    record.VerificationRun_Id,
                    record.PatientSimulant_Id,
                    record.AtlasSearchIdentifier);
            }

            return dataTable;
        }

        private SqlBulkCopy BuildSqlBulkCopy(IEnumerable<string> columnNames)
        {
            var sqlBulk = new SqlBulkCopy(connectionString)
            {
                BulkCopyTimeout = 3600,
                BatchSize = 10000,
                DestinationTableName = nameof(MatchPredictionVerificationContext.SearchRequests)
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
