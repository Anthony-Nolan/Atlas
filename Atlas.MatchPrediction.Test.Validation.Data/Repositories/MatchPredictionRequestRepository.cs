using Atlas.Common.Sql.BulkInsert;
using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using ValidationContext = Atlas.MatchPrediction.Test.Validation.Data.Context.MatchPredictionValidationContext;

namespace Atlas.MatchPrediction.Test.Validation.Data.Repositories
{
    public interface IMatchPredictionRequestRepository : IBulkInsertRepository<MatchPredictionRequest>
    {
        Task<int> GetMaxPatientId();
        Task<IReadOnlyCollection<MatchPredictionRequest>> GetMatchPredictionRequests(IEnumerable<string> algorithmIds);
    }

    public class MatchPredictionRequestRepository : BulkInsertRepository<MatchPredictionRequest>, IMatchPredictionRequestRepository
    {
        private const string TableName = nameof(ValidationContext.MatchPredictionRequests);

        public MatchPredictionRequestRepository(string connectionString) : base(connectionString, TableName)
        {
        }

        public async Task<int> GetMaxPatientId()
        {
            const string sql = @$"SELECT MAX({nameof(MatchPredictionRequest.PatientId)}) FROM {TableName}";

            await using (var conn = new SqlConnection(ConnectionString))
            {
                return (await conn.QueryAsync<int>(sql)).SingleOrDefault();
            }
        }

        public async Task<IReadOnlyCollection<MatchPredictionRequest>> GetMatchPredictionRequests(IEnumerable<string> algorithmIds)
        {
            const string sql = @$"SELECT *
                                FROM {TableName}
                                WHERE {nameof(MatchPredictionRequest.MatchPredictionAlgorithmRequestId)} IN @{nameof(algorithmIds)}";

            await using (var conn = new SqlConnection(ConnectionString))
            {
                return (await conn.QueryAsync<MatchPredictionRequest>(sql, new { algorithmIds })).ToList();
            }
        }
    }
}
