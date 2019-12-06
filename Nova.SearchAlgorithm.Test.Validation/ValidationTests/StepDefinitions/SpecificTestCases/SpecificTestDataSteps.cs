using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models.DonorInfo;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions.SpecificTestCases
{
    public static class SpecificTestDataSteps
    {
        public static async Task GivenDonorAndPatientHla(
            Utils.PhenotypeInfo.PhenotypeInfo<string> donorHla,
            PhenotypeInfo<string> patientHla,
            ScenarioContext scenarioContext)
        {
            await GivenDonorHla(donorHla, scenarioContext);
            GivenPatientHla(patientHla, scenarioContext);
        }
        
        public static async Task GivenDonorHla(Utils.PhenotypeInfo.PhenotypeInfo<string> donorHla, ScenarioContext scenarioContext)
        {
            var staticDataProvider = scenarioContext.Get<IStaticDataProvider>();
            var inputDonor = new InputDonor
            {
                DonorId = DonorIdGenerator.NextId(),
                HlaNames = donorHla,
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult
            };

            await AlgorithmTestingService.AddDonors(new[]
            {
                inputDonor
            });

            staticDataProvider.SetExpectedDonorIds(new[] {inputDonor.DonorId});

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