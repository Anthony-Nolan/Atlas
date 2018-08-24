using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models
{
    public class PatientApiResult
    {
        public IPatientDataFactory PatientDataFactory;
        public SearchAlgorithmApiResult ApiResult;
    }
}