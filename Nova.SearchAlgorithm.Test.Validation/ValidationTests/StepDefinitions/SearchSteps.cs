using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation
{
    [Binding]
    public class SearchSteps : BaseStepDefinitions
    {
        private SearchRequestBuilder searchRequestBuilder = new SearchRequestBuilder();
        private HttpResponseMessage result;


        [Given(@"I search for recognised hla")]
        public void GivenISearchForRecognisedHla()
        {
            searchRequestBuilder = searchRequestBuilder
                .WithLocusMatchHla(Locus.A, TypePositions.One, "2")
                .WithLocusMatchHla(Locus.A, TypePositions.Two, "68")
                .WithLocusMatchHla(Locus.B, TypePositions.One, "7")
                .WithLocusMatchHla(Locus.B, TypePositions.Two, "44")
                .WithLocusMatchHla(Locus.Drb1, TypePositions.One, "01:MS")
                .WithLocusMatchHla(Locus.Drb1, TypePositions.Two, "12:MN")
                .WithLocusMatchHla(Locus.C, TypePositions.One, "04:01")
                .WithLocusMatchHla(Locus.C, TypePositions.Two, "15:02")
                .WithLocusMatchHla(Locus.Dqb1, TypePositions.One, "05:01")
                .WithLocusMatchHla(Locus.Dqb1, TypePositions.Two, "06:01");
        }

        [Given(@"The search type is adult")]
        public void GivenTheSearchTypeIsAdult()
        {
            // TODO
        }

        [Given(@"The search is run for Anthony Nolan's registry only")]
        public void GivenTheSearchIsRunForAnthonyNolanSRegistryOnly()
        {
            // TODO
        }

        [When(@"I run a 6/6 search")]
        public async Task WhenIRunASearch()
        {
            var searchRequest = searchRequestBuilder
                .WithTotalMismatchCount(0)
                .WithLocusMismatchCount(Locus.A, 0)
                .WithLocusMismatchCount(Locus.B, 0)
                .WithLocusMismatchCount(Locus.Drb1, 0)
                .Build();
            
            result = await Server.CreateRequest("/search")
                .AddHeader("X-Samples-ApiKey", "test-key")
                .And(request => request.Content = new StringContent(JsonConvert.SerializeObject(searchRequest), Encoding.UTF8, "application/json"))
                .PostAsync();
        }

        [Then(@"The result should contain at least one donor")]
        public async Task ThenTheResultShouldContainAtLeastOneDonor()
        {
            var content = await result.Content.ReadAsStringAsync();
            var deserialisedContent = JsonConvert.DeserializeObject<SearchResultSet>(content);
            deserialisedContent.SearchResults.Count().Should().BeGreaterThan(0);
        }
    }
}