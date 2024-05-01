using Atlas.Common.Sql.BulkInsert;
using Atlas.MatchPrediction.Test.Validation.Data.Context;
using Atlas.MatchPrediction.Test.Validation.Data.Models.Homework;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.MatchPrediction.Test.Validation.Data.Repositories.Homework
{
    public interface IPatientDonorPairRepository : IBulkInsertRepository<PatientDonorPair>
    {
        Task<IEnumerable<PatientDonorPair>> GetUnprocessedPairs(int homeworkSetId);
        Task UpdateEditableFields(PatientDonorPair pdp);
    }

    public class PatientDonorPairRepository : BulkInsertRepository<PatientDonorPair>, IPatientDonorPairRepository
    {
        public PatientDonorPairRepository(string connectionString) 
            : base(connectionString, nameof(MatchPredictionValidationContext.PatientDonorPairs))
        {
        }

        /// <inheritdoc />
        public async Task<IEnumerable<PatientDonorPair>> GetUnprocessedPairs(int homeworkSetId)
        {
            const string sql = $@"
                SELECT * 
                FROM PatientDonorPairs 
                WHERE {nameof(PatientDonorPair.HomeworkSet_Id)} = @{nameof(homeworkSetId)} AND {nameof(PatientDonorPair.IsProcessed)} = 0";

            await using (var connection = new SqlConnection(ConnectionString))
            {
                return await connection.QueryAsync<PatientDonorPair>(sql, new { homeworkSetId });
            }
        }

        /// <inheritdoc />
        public async Task UpdateEditableFields(PatientDonorPair pdp)
        {
            const string sql = $@"
                UPDATE PatientDonorPairs SET 
                    IsProcessed = @{nameof(pdp.IsProcessed)},
                    DidPatientHaveMissingHla = @{nameof(pdp.DidPatientHaveMissingHla)}, 
                    DidDonorHaveMissingHla = @{nameof(pdp.DidDonorHaveMissingHla)},
                    MatchingGenotypesCalculated = @{nameof(pdp.MatchingGenotypesCalculated)}
                WHERE Id = @{nameof(pdp.Id)}";

            await using (var connection = new SqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql, pdp);
            }
        }
    }
}