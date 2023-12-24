using Atlas.ManualTesting.Common.Models;
using Atlas.ManualTesting.Common.Models.Entities;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.ManualTesting.Common.Repositories
{
    public abstract class SearchRequestsRepositoryBase<TRecord> where TRecord : SearchRequestRecord
    {
        protected readonly string ConnectionString;

        protected SearchRequestsRepositoryBase(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        public async Task<TRecord?> GetRecordByAtlasSearchId(string atlasSearchId)
        {
            const string sql = @$"SELECT * FROM SearchRequests s WHERE s.AtlasSearchIdentifier = @{nameof(atlasSearchId)}";

            await using (var conn = new SqlConnection(ConnectionString))
            {
                return (await conn.QueryAsync<TRecord>(sql, new { atlasSearchId })).SingleOrDefault();
            }
        }

        public async Task MarkSearchResultsAsFailed(int searchRequestRecordId)
        {
            const string sql = $@"UPDATE SearchRequests SET 
                SearchResultsRetrieved = 1,
                WasSuccessful = 0
                WHERE Id = @{nameof(searchRequestRecordId)}";

            await using (var connection = new SqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql, new { searchRequestRecordId });
            }
        }

        public async Task MarkSearchResultsAsSuccessful(SuccessfulSearchRequestInfo info)
        {
            const string sql = $@"UPDATE SearchRequests SET 
                SearchResultsRetrieved = 1,
                WasSuccessful = 1,
                MatchedDonorCount = @{nameof(info.MatchedDonorCount)}
                WHERE Id = @{nameof(info.SearchRequestRecordId)}";

            await using (var connection = new SqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql, new
                {
                    info.MatchedDonorCount,
                    info.SearchRequestRecordId
                });
            }
        }
    }
}
