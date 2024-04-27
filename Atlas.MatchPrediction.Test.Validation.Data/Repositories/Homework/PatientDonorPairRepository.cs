using Atlas.Common.Sql.BulkInsert;
using Atlas.MatchPrediction.Test.Validation.Data.Context;
using Atlas.MatchPrediction.Test.Validation.Data.Models.Homework;

namespace Atlas.MatchPrediction.Test.Validation.Data.Repositories.Homework
{
    public interface IPatientDonorPairRepository : IBulkInsertRepository<PatientDonorPair>
    {
    }

    public class PatientDonorPairRepository : BulkInsertRepository<PatientDonorPair>, IPatientDonorPairRepository
    {
        public PatientDonorPairRepository(string connectionString) 
            : base(connectionString, nameof(MatchPredictionValidationContext.PatientDonorPairs))
        {
        }
    }
}