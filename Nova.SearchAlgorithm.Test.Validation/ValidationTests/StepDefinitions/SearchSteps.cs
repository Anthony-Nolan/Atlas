using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
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
                .WithLocusMatchHla(Locus.A, TypePositions.One, "01:01")
                .WithLocusMatchHla(Locus.A, TypePositions.Two, "11:02")
                .WithLocusMatchHla(Locus.B, TypePositions.One, "07:02")
                .WithLocusMatchHla(Locus.B, TypePositions.Two, "08:41")
                .WithLocusMatchHla(Locus.Drb1, TypePositions.One, "15:09")
                .WithLocusMatchHla(Locus.Drb1, TypePositions.Two, "12:02")
                .WithLocusMatchHla(Locus.C, TypePositions.One, "04:01")
                .WithLocusMatchHla(Locus.C, TypePositions.Two, "15:02")
                .WithLocusMatchHla(Locus.Dqb1, TypePositions.One, "05:01")
                .WithLocusMatchHla(Locus.Dqb1, TypePositions.Two, "06:01");
            
            ScenarioContext.Current.Set(searchRequestBuilder);
        }

        [Given(@"The search type is (.*)")]
        public void GivenTheSearchTypeIs(string searchType)
        {
            var donorType = (DonorType) Enum.Parse(typeof(DonorType), searchType, true);
            var searchRequest = ScenarioContext.Current.Get<SearchRequestBuilder>();
            ScenarioContext.Current.Set(searchRequest.WithSearchType(donorType));
        }

        [Given(@"The search is run for Anthony Nolan's registry only")]
        public void GivenTheSearchIsRunForAnthonyNolanSRegistryOnly()
        {
            var searchRequest = ScenarioContext.Current.Get<SearchRequestBuilder>();
            ScenarioContext.Current.Set(searchRequest.ForRegistries(new []{ RegistryCode.AN }));
        }

        [When(@"I run a 6/6 search")]
        public async Task WhenIRunASixOutOfSixSearch()
        {
            var searchRequest = ScenarioContext.Current.Get<SearchRequestBuilder>()
                .WithTotalMismatchCount(0)
                .WithLocusMismatchCount(Locus.A, 0)
                .WithLocusMismatchCount(Locus.B, 0)
                .WithLocusMismatchCount(Locus.Drb1, 0)
                .Build();

            ScenarioContext.Current.Set(await AlgorithmTestingService.Search(searchRequest));
        }

        [Then(@"The result should contain at least one donor")]
        public void ThenTheResultShouldContainAtLeastOneDonor()
        {
            var results = ScenarioContext.Current.Get<SearchResultSet>();
            results.SearchResults.Count().Should().BeGreaterThan(0);
        }
    }
}