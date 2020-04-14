using Atlas.MatchingAlgorithm.Client.Models;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;
using TechTalk.SpecFlow;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationTests.StepDefinitions.SpecificTestCases
{
    public static class SpecificTestDataSteps
    {
        public static async Task GivenDonorAndPatientHla(
            PhenotypeInfo<string> donorHla,
            PhenotypeInfo<string> patientHla,
            ScenarioContext scenarioContext)
        {
            await GivenDonorHla(donorHla, scenarioContext);
            GivenPatientHla(patientHla, scenarioContext);
        }
        
        public static async Task GivenDonorHla(PhenotypeInfo<string> donorHla, ScenarioContext scenarioContext)
        {
            var staticDataProvider = scenarioContext.Get<IStaticDataProvider>();
            var donorInfo = new DonorInfo
            {
                DonorId = DonorIdGenerator.NextId(),
                HlaNames = donorHla,
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult
            };

            await AlgorithmTestingService.AddDonors(new[]
            {
                donorInfo
            });

            staticDataProvider.SetExpectedDonorIds(new[] {donorInfo.DonorId});

            scenarioContext.Set(staticDataProvider);
            scenarioContext.Set((IExpectedDonorProvider) staticDataProvider);
        }
        
        public static void GivenPatientHla(PhenotypeInfo<string> patientHla, ScenarioContext scenarioContext)
        {
            var staticDataProvider = scenarioContext.Get<IStaticDataProvider>();
            
            staticDataProvider.SetPatientHla(patientHla);

            scenarioContext.Set(staticDataProvider);
            scenarioContext.Set((IPatientDataProvider) staticDataProvider);
        }
    }
}