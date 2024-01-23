using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Microsoft.Data.SqlClient;
using Dapper;
using ValidationContext = Atlas.MatchPrediction.Test.Validation.Data.Context.MatchPredictionValidationContext;

namespace Atlas.MatchPrediction.Test.Validation.Data.Repositories
{
    public interface IValidationRepository
    {
        Task DeleteSubjectInfo();

        /// <param name="firstPatientId">Delete data where patient ID is >= than this value.</param>
        Task DeleteMatchPredictionRequestData(int firstPatientId);

        Task<IEnumerable<string>> GetAlgorithmIdsOfMatchPredictionRequestsMissingResults();
    }

    public class ValidationRepository : IValidationRepository
    {
        private readonly string connectionString;

        public ValidationRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task DeleteSubjectInfo()
        {
            const string sql = @$"DELETE FROM {nameof(ValidationContext.SubjectInfo)}";

            await using (var conn = new SqlConnection(connectionString))
            {
                await conn.ExecuteAsync(sql, commandTimeout: 600);
            }
        }

        public async Task DeleteMatchPredictionRequestData(int firstPatientId)
        {
            const string patientId = nameof(MatchPredictionRequest.PatientId);

            const string sql = @$"
                DELETE FROM MatchPredictionResults
                FROM MatchPredictionResults res
                JOIN MatchPredictionRequests req
                ON res.{nameof(MatchPredictionResults.MatchPredictionRequestId)} = req.{nameof(MatchPredictionRequest.Id)}
                WHERE req.{patientId} >= @{nameof(firstPatientId)}
                
                DELETE FROM MatchPredictionRequests WHERE {patientId} >= @{nameof(firstPatientId)}";

            await using (var conn = new SqlConnection(connectionString))
            {
                await conn.ExecuteAsync(sql, new { firstPatientId }, commandTimeout: 600);
            }
        }

        public async Task<IEnumerable<string>> GetAlgorithmIdsOfMatchPredictionRequestsMissingResults()
        {
            const string sql = @$"SELECT {nameof(MatchPredictionRequest.MatchPredictionAlgorithmRequestId)}
                              FROM MatchPredictionRequests req
                              LEFT JOIN MatchPredictionResults res
                              ON req.{nameof(MatchPredictionRequest.Id)} = res.{nameof(MatchPredictionResults.MatchPredictionRequestId)}
                              WHERE res.{nameof(MatchPredictionResults.Id)} IS NULL";

            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QueryAsync<string>(sql, commandTimeout: 600);
            }
        }
    }
}
