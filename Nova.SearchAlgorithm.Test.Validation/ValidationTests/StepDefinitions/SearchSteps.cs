using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using Nova.SearchAlgorithm.Test.Validation.ValidationTests;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation
{
    [Binding]
    public class SearchSteps
    {
        [Given(@"I search for recognised hla")]
        public void GivenISearchForRecognisedHla()
        {
            var searchRequestBuilder = new SearchRequestBuilder()
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
            
            ScenarioContext.Current.Set(searchRequestBuilder);
        }

        [Given(@"The search type is (.*)")]
        public void GivenTheSearchTypeIs(string searchType)
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
            var searchRequest = ScenarioContext.Current.Get<SearchRequestBuilder>()
                .WithTotalMismatchCount(0)
                .WithLocusMismatchCount(Locus.A, 0)
                .WithLocusMismatchCount(Locus.B, 0)
                .WithLocusMismatchCount(Locus.Drb1, 0)
                .Build();

            ScenarioContext.Current.Set(await AlgorithmService.Search(searchRequest));
        }

        [Then(@"The result should contain at least one donor")]
        public void ThenTheResultShouldContainAtLeastOneDonor()
        {
            var results = ScenarioContext.Current.Get<SearchResultSet>();
            results.SearchResults.Count().Should().BeGreaterThan(0);
        }
    }
}