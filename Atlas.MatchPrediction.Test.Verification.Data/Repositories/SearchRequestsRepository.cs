using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Atlas.ManualTesting.Common.Repositories;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Dapper;

namespace Atlas.MatchPrediction.Test.Verification.Data.Repositories
{
    public class SearchRequestsRepository :
        SearchRequestsRepositoryBase<VerificationSearchRequestRecord>,
        ISearchRequestsRepository<VerificationSearchRequestRecord>
    {
        public SearchRequestsRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task AddSearchRequest(VerificationSearchRequestRecord request)
        {
            const string sql = $@"INSERT INTO SearchRequests(
                VerificationRun_Id,
                PatientId,
                AtlasSearchIdentifier,
                DonorMismatchCount,
                WasSuccessful,
                WasMatchPredictionRun)
                VALUES(
                    @{nameof(request.VerificationRun_Id)},
                    @{nameof(request.PatientId)},
                    @{nameof(request.AtlasSearchIdentifier)},
                    @{nameof(request.DonorMismatchCount)},
                    @{nameof(request.WasSuccessful)},
                    @{nameof(request.WasMatchPredictionRun)}
                )";

            await using (var connection = new SqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql, new
                {
                    request.VerificationRun_Id,
                    request.PatientId,
                    request.AtlasSearchIdentifier,
                    request.DonorMismatchCount,
                    request.WasSuccessful,
                    request.WasMatchPredictionRun
                });
            }
        }
    }
}
