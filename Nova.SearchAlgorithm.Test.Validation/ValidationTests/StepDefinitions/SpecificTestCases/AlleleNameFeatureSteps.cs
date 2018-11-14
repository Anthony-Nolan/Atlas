using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions.SpecificTestCases
{
    [Binding]
    public class AlleleNameFeatureSteps
    {
        [Given(@"the matching donor has a deleted allele")]
        public async Task GivenADonorWithKnownHla()
        {
            var staticDataProvider = ScenarioContext.Current.Get<IStaticDataProvider>();

            const string deletedAlleleAtA = "*02:01:82";
            const string replacementAlleleAtA = "*02:01:84";
            
            var inputDonor = new InputDonor
            {
                DonorId = DonorIdGenerator.NextId(),
                HlaNames = new PhenotypeInfo<string>
                {
                    A = {Position1 = deletedAlleleAtA, Position2 = "*01:01"},
                    B = {Position1 = "*15:01", Position2 = "*15:01"},
                    Drb1 = {Position1 = "*15:03", Position2 = "*15:03"}
                },
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult
            };
            
            var patientHla = new PhenotypeInfo<string>
            {
                A = {Position1 = replacementAlleleAtA, Position2 = inputDonor.HlaNames.A.Position2},
                B = {Position1 = inputDonor.HlaNames.B.Position1, Position2 = inputDonor.HlaNames.B.Position2},
                Drb1 = {Position1 = inputDonor.HlaNames.Drb1.Position1, Position2 = inputDonor.HlaNames.Drb1.Position2}
            };

            await AlgorithmTestingService.AddDonors(new[]
            {
                inputDonor
            });

            staticDataProvider.SetExpectedDonorIds(new[] {inputDonor.DonorId});
            staticDataProvider.SetPatientHla(patientHla);

            ScenarioContext.Current.Set(staticDataProvider);
            ScenarioContext.Current.Set((IExpectedDonorProvider) staticDataProvider);
            ScenarioContext.Current.Set((IPatientDataProvider) staticDataProvider);
        }
    }
}