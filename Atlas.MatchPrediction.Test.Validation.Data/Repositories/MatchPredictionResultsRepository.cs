using Atlas.Common.Sql.BulkInsert;
using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using ValidationContext = Atlas.MatchPrediction.Test.Validation.Data.Context.MatchPredictionValidationContext;

namespace Atlas.MatchPrediction.Test.Validation.Data.Repositories
{
    public interface IMatchPredictionResultsRepository : IBulkInsertRepository<MatchPredictionResults>
    {
        Task DeleteExistingResults(IEnumerable<int> requestIds);
    }

    public class MatchPredictionResultsRepository : BulkInsertRepository<MatchPredictionResults>, IMatchPredictionResultsRepository
    {
        private const string TableName = nameof(ValidationContext.MatchPredictionResults);

        public MatchPredictionResultsRepository(string connectionString) : base(connectionString, TableName)
        {
        }

        public async Task DeleteExistingResults(IEnumerable<int> requestIds)
        {
            const string sql = @$"
                DELETE FROM {TableName}
                WHERE {nameof(MatchPredictionResults.MatchPredictionRequestId)} IN @{nameof(requestIds)}";

            await using (var conn = new SqlConnection(ConnectionString))
            {
                await conn.ExecuteAsync(sql, new { requestIds });
            }
        }
    }
}
