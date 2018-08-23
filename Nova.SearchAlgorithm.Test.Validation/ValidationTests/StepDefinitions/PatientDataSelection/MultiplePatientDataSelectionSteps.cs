using System.Linq;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions
{
    /// <summary>
    /// Contains step definitions for selecting patient data when searches should be run for multiple patients
    /// </summary>
    [Binding]
    public class MultiplePatientDataSelectionSteps
    {
        [Given(@"a set of (.*) patients with matching donors")]
        public void GivenASetOfXPatientsWithMatchingDonors(int numberOfPatients)
        {
            var multiplePatientDataSelector = ScenarioContext.Current.Get<IMultiplePatientDataSelector>();
            multiplePatientDataSelector.SetNumberOfPatients(numberOfPatients);
            ScenarioContext.Current.Set(multiplePatientDataSelector);
        }

        [Given(@"a set of patients with matching donors")]
        public void GivenASetOfPatientsWithMatchingDonors()
        {
            var multiplePatientDataSelector = ScenarioContext.Current.Get<IMultiplePatientDataSelector>();
            ScenarioContext.Current.Set(multiplePatientDataSelector);
        }

        [Given(@"each matching donor is a (.*) match")]
        public void GivenEachMatchingDonorIsOfMatchType(string matchType)
        {
            var selector = ScenarioContext.Current.Get<IMultiplePatientDataSelector>();
            selector.PatientDataSelectors =
                selector.PatientDataSelectors.Select(s => (SingleDonorPatientDataSelector) s.SetMatchType(matchType)).ToList();
            ScenarioContext.Current.Set(selector);
        }

        [Given(@"each matching donor is of type (.*)")]
        public void GivenEachMatchingDonorIsOfDonorType(string donorType)
        {
            var selector = ScenarioContext.Current.Get<IMultiplePatientDataSelector>();
            selector.PatientDataSelectors = selector.PatientDataSelectors.Select(s => (SingleDonorPatientDataSelector) s.SetMatchDonorType(donorType))
                .ToList();
            ScenarioContext.Current.Set(selector);
        }

        [Given(@"each matching donor is (.*) typed at (.*)")]
        public void GivenEachMatchingDonorIsHlaTyped(string typingCategory, string locus)
        {
            var selector = ScenarioContext.Current.Get<IMultiplePatientDataSelector>();
            selector.PatientDataSelectors = selector.PatientDataSelectors
                .Select(s => (SingleDonorPatientDataSelector) s.SetMatchTypingCategories(typingCategory, locus)).ToList();
            ScenarioContext.Current.Set(selector);
        }

        [Given(@"each matching donor is in registry: (.*)")]
        public void GivenEachMatchingDonorIsInRegistry(string registry)
        {
            var selector = ScenarioContext.Current.Get<IMultiplePatientDataSelector>();
            selector.PatientDataSelectors = selector.PatientDataSelectors
                .Select(s => (SingleDonorPatientDataSelector) s.SetMatchDonorRegistry(registry)).ToList();
            ScenarioContext.Current.Set(selector);
        }
    }
}