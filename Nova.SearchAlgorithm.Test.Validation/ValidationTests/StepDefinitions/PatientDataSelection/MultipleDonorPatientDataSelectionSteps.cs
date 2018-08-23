using System.Linq;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions
{
    /// <summary>
    /// Contains step definitions for selecting patient data when a single patient corresponds to a multiple (database level) donors
    /// </summary>
    [Binding]
    public class MultipleDonorPatientDataSelectionSteps
    {
        [Given(@"a patient has multiple matches at different typing resolutions")]
        public void GivenAPatientHasMultipleMatchesAtDifferentTypingResolutions()
        {
            var patientDataSelector = ScenarioContext.Current.Get<IMultipleDonorPatientDataSelector>();

            var allResolutions = new[]
            {
                HlaTypingResolution.Tgs,
                HlaTypingResolution.ThreeFieldTruncatedAllele,
                HlaTypingResolution.TwoFieldTruncatedAllele,
                HlaTypingResolution.NmdpCode,
                HlaTypingResolution.XxCode,
                HlaTypingResolution.Serology,
                HlaTypingResolution.Arbitrary
            };
            var resolutionSets = allResolutions.Select(r => new PhenotypeInfo<HlaTypingResolution>(r));
            foreach (var resolutionSet in resolutionSets)
            {
                patientDataSelector.AddFullDonorTypingResolution(resolutionSet);
            }

            ScenarioContext.Current.Set(patientDataSelector);
            ScenarioContext.Current.Set((IPatientHlaContainer) patientDataSelector);
        }
    }
}