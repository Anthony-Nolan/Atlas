using Atlas.Common.Sql.BulkInsert;
using Atlas.MatchPrediction.Test.Validation.Data.Context;
using Atlas.MatchPrediction.Test.Validation.Data.Models.Homework;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.MatchPrediction.Test.Validation.Data.Repositories.Homework
{
    public interface ISubjectGenotypeRepository : IBulkInsertRepository<SubjectGenotype>
    {
        Task<IEnumerable<SubjectGenotype>> Get(int imputationSummaryId);
    }

    public class SubjectGenotypeRepository : BulkInsertRepository<SubjectGenotype>, ISubjectGenotypeRepository
    {
        private const string TableName = nameof(MatchPredictionValidationContext.SubjectGenotypes);

        /// <inheritdoc />
        public SubjectGenotypeRepository(string connectionString) : base(connectionString, TableName)
        {
        }

        /// <inheritdoc />
        public async Task<IEnumerable<SubjectGenotype>> Get(int imputationSummaryId)
        {
            const string sql = $@"
                SELECT * 
                FROM {TableName} 
                WHERE {nameof(SubjectGenotype.ImputationSummary_Id)} = @{nameof(imputationSummaryId)}";

            await using (var connection = new SqlConnection(ConnectionString))
            {
                return await connection.QueryAsync<SubjectGenotype>(sql, new { imputationSummaryId });
            }
        }
    }
}