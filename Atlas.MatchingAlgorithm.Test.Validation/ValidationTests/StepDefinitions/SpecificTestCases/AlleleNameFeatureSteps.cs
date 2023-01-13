using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
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
            (
                valueA: new LocusInfo<string>(deletedAlleleAtA, "*01:01"),
                valueB: new LocusInfo<string>("*15:01", "*15:01"),
                valueDrb1: new LocusInfo<string>("*15:03", "*15:03")
            );
            var patientHla = new PhenotypeInfo<string>
            (
                valueA: new LocusInfo<string>(replacementAlleleAtA, donorHla.A.Position2),
                valueB: new LocusInfo<string>(donorHla.B.Position1, donorHla.B.Position2),
                valueDrb1: new LocusInfo<string>(donorHla.Drb1.Position1, donorHla.Drb1.Position2)
            );

            await SpecificTestDataSteps.GivenDonorAndPatientHla(donorHla, patientHla, scenarioContext);
        }

        [Given(@"the patient has a deleted allele")]
        public async Task GivenAPatientWithADeletedAllele()
        {
            const string deletedAlleleAtA = "*02:01:82";
            const string replacementAlleleAtA = "*02:01:84";

            var donorHla = new PhenotypeInfo<string>
            (
                valueA: new LocusInfo<string>(replacementAlleleAtA, "*01:01"),
                valueB: new LocusInfo<string>("*15:01", "*15:01"),
                valueDrb1: new LocusInfo<string>("*15:03", "*15:03")
            );
            var patientHla = new PhenotypeInfo<string>
            (
                valueA: new LocusInfo<string>(deletedAlleleAtA, donorHla.A.Position2),
                valueB: new LocusInfo<string>(donorHla.B.Position1, donorHla.B.Position2),
                valueDrb1: new LocusInfo<string>(donorHla.Drb1.Position1, donorHla.Drb1.Position2)
            );

            await SpecificTestDataSteps.GivenDonorAndPatientHla(donorHla, patientHla, scenarioContext);
        }

        [Given(@"the matching donor has an old version of a renamed allele")]
        public async Task GivenADonorWithAnOldVersionOfARenamedAllele()
        {
            const string oldNameAtA = "*02:09";
            const string newNameAtA = "*02:09:01:01";

            var donorHla = new PhenotypeInfo<string>
            (
                valueA: new LocusInfo<string>(oldNameAtA, "*01:01"),
                valueB: new LocusInfo<string>("*15:01", "*15:01"),
                valueDrb1: new LocusInfo<string>("*15:03", "*15:03")
            );
            var patientHla = new PhenotypeInfo<string>
            (
                valueA: new LocusInfo<string>(newNameAtA, donorHla.A.Position2),
                valueB: new LocusInfo<string>(donorHla.B.Position1, donorHla.B.Position2),
                valueDrb1: new LocusInfo<string>(donorHla.Drb1.Position1, donorHla.Drb1.Position2)
            );

            await SpecificTestDataSteps.GivenDonorAndPatientHla(donorHla, patientHla, scenarioContext);
        }

        [Given(@"the patient has an old version of a renamed allele")]
        public async Task GivenAPatientWithAnOldVersionOfARenamedAllele()
        {
            const string oldNameAtA = "*02:09";
            const string newNameAtA = "*02:09:01:01";

            var donorHla = new PhenotypeInfo<string>
            (
                valueA: new LocusInfo<string>(newNameAtA, "*01:01"),
                valueB: new LocusInfo<string>("*15:01", "*15:01"),
                valueDrb1: new LocusInfo<string>("*15:03", "*15:03")
            );
            var patientHla = new PhenotypeInfo<string>
            (
                valueA: new LocusInfo<string>(oldNameAtA, donorHla.A.Position2),
                valueB: new LocusInfo<string>(donorHla.B.Position1, donorHla.B.Position2),
                valueDrb1: new LocusInfo<string>(donorHla.Drb1.Position1, donorHla.Drb1.Position2)
            );

            await SpecificTestDataSteps.GivenDonorAndPatientHla(donorHla, patientHla, scenarioContext);
        }
    }
}