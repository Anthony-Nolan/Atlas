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
        public async Task GivenADonorWithADeletedAllele()
        {
            const string deletedAlleleAtA = "*02:01:82";
            const string replacementAlleleAtA = "*02:01:84";

            var donorHla = new PhenotypeInfo<string>
            {
                A = {Position1 = deletedAlleleAtA, Position2 = "*01:01"},
                B = {Position1 = "*15:01", Position2 = "*15:01"},
                Drb1 = {Position1 = "*15:03", Position2 = "*15:03"}
            };
            var patientHla = new PhenotypeInfo<string>
            {
                A = {Position1 = replacementAlleleAtA, Position2 = donorHla.A.Position2},
                B = {Position1 = donorHla.B.Position1, Position2 = donorHla.B.Position2},
                Drb1 = {Position1 = donorHla.Drb1.Position1, Position2 = donorHla.Drb1.Position2}
            };

            await GivenDonorAndPatientHla(donorHla, patientHla);
        }

        [Given(@"the patient has a deleted allele")]
        public async Task GivenAPatientWithADeletedAllele()
        {
            const string deletedAlleleAtA = "*02:01:82";
            const string replacementAlleleAtA = "*02:01:84";

            var donorHla = new PhenotypeInfo<string>
            {
                A = {Position1 = replacementAlleleAtA, Position2 = "*01:01"},
                B = {Position1 = "*15:01", Position2 = "*15:01"},
                Drb1 = {Position1 = "*15:03", Position2 = "*15:03"}
            };
            var patientHla = new PhenotypeInfo<string>
            {
                A = {Position1 = deletedAlleleAtA, Position2 = donorHla.A.Position2},
                B = {Position1 = donorHla.B.Position1, Position2 = donorHla.B.Position2},
                Drb1 = {Position1 = donorHla.Drb1.Position1, Position2 = donorHla.Drb1.Position2}
            };
            
            await GivenDonorAndPatientHla(donorHla, patientHla);
        }
        
        [Given(@"the matching donor has an old version of a renamed allele")]
        public async Task GivenADonorWithAnOldVersionOfARenamedAllele()
        {
            const string oldNameAtA = "*02:09";
            const string newNameAtA = "*02:09:01:01";

            var donorHla = new PhenotypeInfo<string>
            {
                A = {Position1 = oldNameAtA, Position2 = "*01:01"},
                B = {Position1 = "*15:01", Position2 = "*15:01"},
                Drb1 = {Position1 = "*15:03", Position2 = "*15:03"}
            };
            var patientHla = new PhenotypeInfo<string>
            {
                A = {Position1 = newNameAtA, Position2 = donorHla.A.Position2},
                B = {Position1 = donorHla.B.Position1, Position2 = donorHla.B.Position2},
                Drb1 = {Position1 = donorHla.Drb1.Position1, Position2 = donorHla.Drb1.Position2}
            };
            
            await GivenDonorAndPatientHla(donorHla, patientHla);
        }
        
        [Given(@"the patient has an old version of a renamed allele")]
        public async Task GivenAPatientWithAnOldVersionOfARenamedAllele()
        {
            const string oldNameAtA = "*02:09";
            const string newNameAtA = "*02:09:01:01";

            var donorHla = new PhenotypeInfo<string>
            {
                A = {Position1 = newNameAtA, Position2 = "*01:01"},
                B = {Position1 = "*15:01", Position2 = "*15:01"},
                Drb1 = {Position1 = "*15:03", Position2 = "*15:03"}
            };
            var patientHla = new PhenotypeInfo<string>
            {
                A = {Position1 = oldNameAtA, Position2 = donorHla.A.Position2},
                B = {Position1 = donorHla.B.Position1, Position2 = donorHla.B.Position2},
                Drb1 = {Position1 = donorHla.Drb1.Position1, Position2 = donorHla.Drb1.Position2}
            };
            
            await GivenDonorAndPatientHla(donorHla, patientHla);
        }

        private static async Task GivenDonorAndPatientHla(Utils.PhenotypeInfo.PhenotypeInfo<string> donorHla, PhenotypeInfo<string> patientHla)
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
            staticDataProvider.SetPatientHla(patientHla);

            ScenarioContext.Current.Set(staticDataProvider);
            ScenarioContext.Current.Set((IExpectedDonorProvider) staticDataProvider);
            ScenarioContext.Current.Set((IPatientDataProvider) staticDataProvider);
        }
    }
}