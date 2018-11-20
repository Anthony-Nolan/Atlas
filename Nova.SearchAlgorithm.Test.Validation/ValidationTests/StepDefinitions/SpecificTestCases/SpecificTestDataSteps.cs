using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions.SpecificTestCases
{
    public static class SpecificTestDataSteps
    {
        public static async Task GivenDonorAndPatientHla(Nova.Utils.PhenotypeInfo.PhenotypeInfo<string> donorHla, PhenotypeInfo<string> patientHla)
        {
            await GivenDonorHla(donorHla);
            GivenPatientHla(patientHla);
        }
        
        public static async Task GivenDonorHla(Nova.Utils.PhenotypeInfo.PhenotypeInfo<string> donorHla)
        {
            var staticDataProvider = ScenarioContext.Current.Get<IStaticDataProvider>();
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

            ScenarioContext.Current.Set(staticDataProvider);
            ScenarioContext.Current.Set((IExpectedDonorProvider) staticDataProvider);
        }
        
        public static void GivenPatientHla(PhenotypeInfo<string> patientHla)
        {
            var staticDataProvider = ScenarioContext.Current.Get<IStaticDataProvider>();
            
            staticDataProvider.SetPatientHla(patientHla);

            ScenarioContext.Current.Set(staticDataProvider);
            ScenarioContext.Current.Set((IPatientDataProvider) staticDataProvider);
        }
    }
}