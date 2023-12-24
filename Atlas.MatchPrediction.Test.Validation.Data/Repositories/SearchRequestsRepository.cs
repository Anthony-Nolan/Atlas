using Atlas.ManualTesting.Common.Repositories;
using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.MatchPrediction.Test.Validation.Data.Repositories
{
    public class SearchRequestsRepository :
        SearchRequestsRepositoryBase<ValidationSearchRequestRecord>,
        ISearchRequestsRepository<ValidationSearchRequestRecord>
    {
        public SearchRequestsRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task AddSearchRequest(ValidationSearchRequestRecord request)
        {
            const string sql = $@"INSERT INTO SearchRequests(
                SearchSet_Id,
                PatientId,
                AtlasSearchIdentifier,
                DonorMismatchCount,
                WasSuccessful)
                VALUES(
                    @{nameof(request.SearchSet_Id)},
                    @{nameof(request.PatientId)},
                    @{nameof(request.AtlasSearchIdentifier)},
                    @{nameof(request.DonorMismatchCount)},
                    @{nameof(request.WasSuccessful)}
                )";

            await using (var connection = new SqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql, new
                {
                    request.SearchSet_Id,
                    request.PatientId,
                    request.AtlasSearchIdentifier,
                    request.DonorMismatchCount,
                    request.WasSuccessful
                });
            }
        }
    }
}