using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Common.Models;
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
                {
                    A = { Position1 = A1, Position2 = A2},
                    B = { Position1 = B1, Position2 = B2},
                    C = { Position1 = C1, Position2 = C2},
                    Dpb1 = { Position1 = Dpb11, Position2 = Dpb12},
                    Dqb1 = { Position1 = Dqb11, Position2 = Dqb12},
                    Drb1 = { Position1 = Drb11, Position2 = Drb12},
                };
            }
        }
    }
}