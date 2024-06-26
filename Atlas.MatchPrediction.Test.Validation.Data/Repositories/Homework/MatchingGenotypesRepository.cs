using Atlas.Common.Sql.BulkInsert;
using Atlas.MatchPrediction.Test.Validation.Data.Context;
using Atlas.MatchPrediction.Test.Validation.Data.Models.Homework;

namespace Atlas.MatchPrediction.Test.Validation.Data.Repositories.Homework
{
    public interface IMatchingGenotypesRepository : IBulkInsertRepository<MatchingGenotypes>
    {
    }

    public class MatchingGenotypesRepository : BulkInsertRepository<MatchingGenotypes>, IMatchingGenotypesRepository
    {
        private const string TableName = nameof(MatchPredictionValidationContext.MatchingGenotypes);

        /// <inheritdoc />
        public MatchingGenotypesRepository(string connectionString) : base(connectionString, TableName)
        {
        }
    }
}