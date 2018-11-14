using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions.PatientDataSelection
{
    [Binding]
    public class ExplicitDataSelectionSteps
    {
        [Given(@"the matching donor has hla")]
        public async Task GivenADonorWithKnownHla()
        {
            var staticDataProvider = ScenarioContext.Current.Get<IStaticDataProvider>();

            var inputDonor = new InputDonor
            {
                DonorId = DonorIdGenerator.NextId(),
                HlaNames = new PhenotypeInfo<string>
                {
                    A = {Position1 = "*01:01", Position2 = "*01:01"},
                    B = {Position1 = "*15:01", Position2 = "*15:01"},
                    Drb1 = {Position1 = "*15:03", Position2 = "*15:03"}
                },
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

        [Given(@"the patient has hla")]
        public void GivenAPatientWithKnownHla()
        {
            var staticDataProvider = ScenarioContext.Current.Get<IStaticDataProvider>();
            var patientHla = new PhenotypeInfo<string>
            {
                A = {Position1 = "*01:01", Position2 = "*01:01"},
                B = {Position1 = "*15:01", Position2 = "*15:01"},
                Drb1 = {Position1 = "*15:03", Position2 = "*15:03"}
            };
            
            staticDataProvider.SetPatientHla(patientHla);
            ScenarioContext.Current.Set(staticDataProvider);
            ScenarioContext.Current.Set((IPatientDataProvider) staticDataProvider);
        }
    }
}