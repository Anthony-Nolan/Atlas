using System.Threading.Tasks;
using Nova.Utils.PhenotypeInfo;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions.SpecificTestCases
{
    [Binding]
    public class FeatureDefinedTestDataSteps
    {
        [Given(@"the matching donor has the following HLA:")]
        public async Task GivenTheMatchingDonorHasTheFollowingHla(Table donorHlaTable)
        {
            var hla = donorHlaTable.CreateInstance<PhenotypeInfo<string>>();
            await SpecificTestDataSteps.GivenDonorHla(hla);
        }
        
        [Given(@"the patient has the following HLA:")]
        public void GivenThePatientHasTheFollowingHla(Table donorHlaTable)
        {
            var hla = donorHlaTable.CreateInstance<PhenotypeInfo<string>>();
            SpecificTestDataSteps.GivenPatientHla(new Common.Models.PhenotypeInfo<string>(hla));
        }
    }
}