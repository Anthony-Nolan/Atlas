using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Microsoft.Data.SqlClient;
using Dapper;
using ValidationContext = Atlas.MatchPrediction.Test.Validation.Data.Context.MatchPredictionValidationContext;

namespace Atlas.MatchPrediction.Test.Validation.Data.Repositories
{
    public interface IValidationRepository
    {
        Task DeleteAllExistingData();

        /// <param name="firstPatientId">Delete data where patient ID is >= than this value.</param>
        Task DeleteMatchPredictionRelatedData(int firstPatientId);

        Task<IEnumerable<string>> GetAlgorithmIdsOfRequestsMissingResults();
    }

    public class ValidationRepository : IValidationRepository
    {
        private const string ResultsTable = nameof(ValidationContext.MatchPredictionResults);
        private const string RequestsTable = nameof(ValidationContext.MatchPredictionRequests);

        private readonly string connectionString;

        public ValidationRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task DeleteAllExistingData()
        {
            const string sql = @$"
                DELETE FROM {ResultsTable}
                DELETE FROM {RequestsTable}
                DELETE FROM {nameof(ValidationContext.TestDonorExportRecords)}
                DELETE FROM {nameof(ValidationContext.SubjectInfo)}";

            await using (var conn = new SqlConnection(connectionString))
            {
                await conn.ExecuteAsync(sql, commandTimeout: 600);
            }
        }

        public async Task DeleteMatchPredictionRelatedData(int firstPatientId)
        {
            const string patientId = nameof(MatchPredictionRequest.PatientId);

            const string sql = @$"
                DELETE FROM {ResultsTable}
                FROM {ResultsTable} res
                JOIN {RequestsTable} req
                ON res.{nameof(MatchPredictionResults.MatchPredictionRequestId)} = req.{nameof(MatchPredictionRequest.Id)}
                WHERE req.{patientId} >= @{nameof(firstPatientId)}
                
                DELETE FROM {RequestsTable} WHERE {patientId} >= @{nameof(firstPatientId)}";

            await using (var conn = new SqlConnection(connectionString))
            {
                await conn.ExecuteAsync(sql, new { firstPatientId }, commandTimeout: 600);
            }
        }

        public async Task<IEnumerable<string>> GetAlgorithmIdsOfRequestsMissingResults()
        {
            const string sql = @$"SELECT {nameof(MatchPredictionRequest.MatchPredictionAlgorithmRequestId)}
                              FROM {RequestsTable} req
                              LEFT JOIN {ResultsTable} res
                              ON req.{nameof(MatchPredictionRequest.Id)} = res.{nameof(MatchPredictionResults.MatchPredictionRequestId)}
                              WHERE res.{nameof(MatchPredictionResults.Id)} IS NULL";

            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QueryAsync<string>(sql, commandTimeout: 600);
            }
        }
    }
}
