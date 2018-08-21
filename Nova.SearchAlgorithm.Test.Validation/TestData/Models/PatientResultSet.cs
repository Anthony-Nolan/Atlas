using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models
{
    public class PatientResultSet
    {
        public IPatientDataSelector PatientDataSelector;
        public SearchResultSet SearchResultSet;
    }
}