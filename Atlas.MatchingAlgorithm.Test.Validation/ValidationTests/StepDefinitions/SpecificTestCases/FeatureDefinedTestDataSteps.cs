using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationTests.StepDefinitions.SpecificTestCases
{
    [Binding]
    public class FeatureDefinedTestDataSteps
    {
        private readonly ScenarioContext scenarioContext;

        public FeatureDefinedTestDataSteps(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }

        [Given(@"the matching donor has the following HLA:")]
        public async Task GivenTheMatchingDonorHasTheFollowingHla(Table donorHlaTable)
        {
            var hla = donorHlaTable.CreateInstance<Hla>().ToPhenotypeInfo();
            await SpecificTestDataSteps.GivenDonorHla(hla, scenarioContext);
        }

        [Given(@"the patient has the following HLA:")]
        public void GivenThePatientHasTheFollowingHla(Table donorHlaTable)
        {
            var hla = donorHlaTable.CreateInstance<Hla>().ToPhenotypeInfo();
            SpecificTestDataSteps.GivenPatientHla(new PhenotypeInfo<string>(hla), scenarioContext);
        }

        // Use a private class for serialisation from a SpecFlow DataTable - as individual positions are not settable in PhenotypeInfo 
        private class Hla
        {
            public string A1 { get; set; }
            public string A2 { get; set; }
            public string B1 { get; set; }
            public string B2 { get; set; }
            public string C1 { get; set; }
            public string C2 { get; set; }
            public string Dpb11 { get; set; }
            public string Dpb12 { get; set; }
            public string Dqb11 { get; set; }
            public string Dqb12 { get; set; }
            public string Drb11 { get; set; }
            public string Drb12 { get; set; }

            public PhenotypeInfo<string> ToPhenotypeInfo()
            {
                return new PhenotypeInfo<string>
                (
                    valueA: new LocusInfo<string>(A1, A2),
                    valueB: new LocusInfo<string>(B1, B2),
                    valueC: new LocusInfo<string>(C1, C2),
                    valueDpb1: new LocusInfo<string>(Dpb11, Dpb12),
                    valueDqb1: new LocusInfo<string>(Dqb11, Dqb12),
                    valueDrb1: new LocusInfo<string>(Drb11, Drb12)
                );
            }
        }
    }
}