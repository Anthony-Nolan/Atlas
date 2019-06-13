using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.PatientFactories;
using System.Linq;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions.PatientDataSelection
{
    /// <summary>
    /// Contains step definitions for selecting patient data when searches should be run for multiple patients
    /// </summary>
    [Binding]
    public class MultiplePatientDataSelectionSteps
    {
        private readonly ScenarioContext scenarioContext;

        public MultiplePatientDataSelectionSteps(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }

        [Given(@"a set of (.*) patients with matching donors")]
        public void GivenASetOfXPatientsWithMatchingDonors(int numberOfPatients)
        {
            var factory = scenarioContext.Get<IMultiplePatientDataFactory>();
            factory.SetNumberOfPatients(numberOfPatients);
            scenarioContext.Set(factory);
        }

        [Given(@"a set of patients with matching donors")]
        public void GivenASetOfPatientsWithMatchingDonors()
        {
            var factory = scenarioContext.Get<IMultiplePatientDataFactory>();
            scenarioContext.Set(factory);
        }

        [Given(@"each matching donor is a (.*) match")]
        public void GivenEachMatchingDonorIsOfMatchType(string matchType)
        {
            var factory = scenarioContext.Get<IMultiplePatientDataFactory>();
            factory.PatientDataFactories = factory.PatientDataFactories.Select(s => (PatientDataFactory) s.SetMatchType(matchType)).ToList();
            scenarioContext.Set(factory);
        }

        [Given(@"each matching donor is of type (.*)")]
        public void GivenEachMatchingDonorIsOfDonorType(string donorType)
        {
            var factory = scenarioContext.Get<IMultiplePatientDataFactory>();
            factory.PatientDataFactories = factory.PatientDataFactories.Select(s => (PatientDataFactory) s.SetMatchDonorType(donorType)).ToList();
            scenarioContext.Set(factory);
        }

        [Given(@"each matching donor is (.*) typed at (.*)")]
        public void GivenEachMatchingDonorIsHlaTyped(string typingCategory, string locus)
        {
            var factory = scenarioContext.Get<IMultiplePatientDataFactory>();
            factory.PatientDataFactories = factory.PatientDataFactories.Select(s => (PatientDataFactory) s.SetMatchTypingCategories(typingCategory, locus)).ToList();
            scenarioContext.Set(factory);
        }

        [Given(@"each matching donor is in registry: (.*)")]
        public void GivenEachMatchingDonorIsInRegistry(string registry)
        {
            var factory = scenarioContext.Get<IMultiplePatientDataFactory>();
            factory.PatientDataFactories = factory.PatientDataFactories.Select(s => (PatientDataFactory) s.SetMatchDonorRegistry(registry)).ToList();
            scenarioContext.Set(factory);
        }
    }
}