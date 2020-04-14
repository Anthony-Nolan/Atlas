using Atlas.MatchingAlgorithm.Common.Models;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationTests.StepDefinitions.SpecificTestCases
{
    [Binding]
    public class AlleleNameFeatureSteps
    {
        private readonly ScenarioContext scenarioContext;
        
        public AlleleNameFeatureSteps(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }

        
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

            await SpecificTestDataSteps.GivenDonorAndPatientHla(donorHla, patientHla, scenarioContext);
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
            
            await SpecificTestDataSteps.GivenDonorAndPatientHla(donorHla, patientHla, scenarioContext);
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
            
            await SpecificTestDataSteps.GivenDonorAndPatientHla(donorHla, patientHla, scenarioContext);
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
            
            await SpecificTestDataSteps.GivenDonorAndPatientHla(donorHla, patientHla, scenarioContext);
        }
    }
}