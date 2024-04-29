using Atlas.Common.Sql.BulkInsert;
using Atlas.MatchPrediction.Test.Validation.Data.Context;
using Atlas.MatchPrediction.Test.Validation.Data.Models.Homework;

namespace Atlas.MatchPrediction.Test.Validation.Data.Repositories.Homework
{
    public interface ISubjectGenotypeRepository : IBulkInsertRepository<SubjectGenotype>
    {
    }

    public class SubjectGenotypeRepository : BulkInsertRepository<SubjectGenotype>, ISubjectGenotypeRepository
    {
        private const string TableName = nameof(MatchPredictionValidationContext.SubjectGenotypes);

        /// <inheritdoc />
        public SubjectGenotypeRepository(string connectionString) : base(connectionString, TableName)
        {
        }
    }
}