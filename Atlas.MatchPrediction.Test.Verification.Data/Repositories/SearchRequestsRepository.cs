using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Dapper;

namespace Atlas.MatchPrediction.Test.Verification.Data.Repositories
{
    public interface ISearchRequestsRepository
    {
        Task AddSearchRequest(SearchRequestRecord request);
        Task<SearchRequestRecord> GetRecordByAtlasSearchId(string atlasSearchId);
        Task MarkSearchResultsAsFailed(int searchRequestRecordId);
        Task MarkSearchResultsAsSuccessful(SuccessfulSearchRequestInfo info);
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
                AtlasSearchIdentifier,
                DonorMismatchCount)
                VALUES(
                    @{nameof(request.VerificationRun_Id)},
                    @{nameof(request.PatientSimulant_Id)},
                    @{nameof(request.AtlasSearchIdentifier)},
                    @{nameof(request.DonorMismatchCount)}
                )";

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, new
                {
                    request.VerificationRun_Id,
                    request.PatientSimulant_Id,
                    request.AtlasSearchIdentifier,
                    request.DonorMismatchCount
                });
            }
        }

        public async Task<SearchRequestRecord> GetRecordByAtlasSearchId(string atlasSearchId)
        {
            var sql = @$"SELECT * FROM SearchRequests s WHERE s.AtlasSearchIdentifier = @{nameof(atlasSearchId)}";

            await using (var conn = new SqlConnection(connectionString))
            {
                return (await conn.QueryAsync<SearchRequestRecord>(sql, new { atlasSearchId })).SingleOrDefault();
            }
        }

        public async Task MarkSearchResultsAsFailed(int searchRequestRecordId)
        {
            var sql = $@"UPDATE SearchRequests SET 
                SearchResultsRetrieved = 1,
                WasSuccessful = 0
                WHERE Id = @{nameof(searchRequestRecordId)}";

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, new { searchRequestRecordId });
            }
        }

        public async Task MarkSearchResultsAsSuccessful(SuccessfulSearchRequestInfo info)
        {
            var sql = $@"UPDATE SearchRequests SET 
                SearchResultsRetrieved = 1,
                WasSuccessful = 1,
                MatchedDonorCount = @{nameof(info.MatchedDonorCount)},
                MatchingAlgorithmTimeInMs = @{nameof(info.MatchingAlgorithmTimeInMs)},
                MatchPredictionTimeInMs = @{nameof(info.MatchPredictionTimeInMs)},
                OverallSearchTimeInMs = @{nameof(info.OverallSearchTimeInMs)}
                WHERE Id = @{nameof(info.SearchRequestRecordId)}";

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, new
                {
                    info.MatchedDonorCount,
                    info.MatchingAlgorithmTimeInMs,
                    info.MatchPredictionTimeInMs,
                    info.OverallSearchTimeInMs,
                    info.SearchRequestRecordId
                });
            }
        }
    }
}
