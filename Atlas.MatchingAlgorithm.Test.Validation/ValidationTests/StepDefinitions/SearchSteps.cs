using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Config;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.PatientFactories;
using Atlas.MatchingAlgorithm.Test.Validation.TestHelpers;
using FluentAssertions;
using TechTalk.SpecFlow;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationTests.StepDefinitions
{
    [Binding]
    public class SearchSteps
    {
        private readonly ScenarioContext scenarioContext;

        public SearchSteps(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }

        [Given(@"the search type is (.*)")]
        public void GivenTheSearchTypeIs(string searchType)
        {
            var donorType = (DonorType) Enum.Parse(typeof(DonorType), searchType, true);
            var searchRequest = scenarioContext.Get<SearchRequestBuilder>();
            scenarioContext.Set(searchRequest.WithSearchType(donorType));
        }

        [Given(@"locus (.*) is excluded from aggregate scoring")]
        public void GivenLocusIsExcludedFromAggregateScoring(string locusString)
        {
            var locus = (Locus) Enum.Parse(typeof(Locus), locusString, true);
            var searchRequest = scenarioContext.Get<SearchRequestBuilder>();
            scenarioContext.Set(searchRequest.WithLociExcludedFromScoringAggregates(new List<Locus> {locus}));
        }

        [When(@"I run a 6/6 search")]
        public async Task WhenIRunASixOutOfSixSearch()
        {
            var patientDataProvider = scenarioContext.Get<IPatientDataProvider>();
            var searchRequestBuilder = scenarioContext.Get<SearchRequestBuilder>();
            var searchHla = patientDataProvider.GetPatientHla();

            var searchRequest = searchRequestBuilder
                .WithTotalMismatchCount(0)
                .WithLocusMismatchCount(Locus.A, 0)
                .WithLocusMismatchCount(Locus.B, 0)
                .WithLocusMismatchCount(Locus.Drb1, 0)
                .WithSearchHla(searchHla)
                .Build();

            scenarioContext.Set(await AlgorithmTestingService.Search(searchRequest));
        }

        [When(@"I run a 8/8 search")]
        [When(@"I run an 8/8 search")]
        public async Task WhenIRunAnEightOutOfEightSearch()
        {
            var patientDataProvider = scenarioContext.Get<IPatientDataProvider>();
            var searchRequestBuilder = scenarioContext.Get<SearchRequestBuilder>();
            var searchHla = patientDataProvider.GetPatientHla();

            var searchRequest = searchRequestBuilder
                .WithTotalMismatchCount(0)
                .WithLocusMismatchCount(Locus.A, 0)
                .WithLocusMismatchCount(Locus.B, 0)
                .WithLocusMismatchCount(Locus.Drb1, 0)
                .WithLocusMismatchCount(Locus.C, 0)
                .WithSearchHla(searchHla)
                .Build();

            scenarioContext.Set(await AlgorithmTestingService.Search(searchRequest));
        }

        [When(@"I run a 4/8 search")]
        public async Task WhenIRunAFourOutOfEightSearch()
        {
            var patientDataProvider = scenarioContext.Get<IPatientDataProvider>();
            var searchRequestBuilder = scenarioContext.Get<SearchRequestBuilder>();
            var searchHla = patientDataProvider.GetPatientHla();

            var searchRequest = searchRequestBuilder
                .WithTotalMismatchCount(4)
                .WithLocusMismatchCount(Locus.A, 2)
                .WithLocusMismatchCount(Locus.B, 2)
                .WithLocusMismatchCount(Locus.Drb1, 2)
                .WithLocusMismatchCount(Locus.C, 2)
                .WithSearchHla(searchHla)
                .Build();

            scenarioContext.Set(await AlgorithmTestingService.Search(searchRequest));
        }

        [When(@"I run a 10/10 search for each patient")]
        public async Task WhenIRunATenOutOfTenSearchForEachPatient()
        {
            var selector = scenarioContext.Get<IMultiplePatientDataFactory>();

            var patientResults = new List<PatientApiResult>();

            foreach (var patientDataFactory in selector.PatientDataFactories)
            {
                var searchHla = patientDataFactory.GetPatientHla();
                var searchRequestBuilder = scenarioContext.Get<SearchRequestBuilder>();

                var searchRequest = searchRequestBuilder
                    .WithTotalMismatchCount(0)
                    .WithLocusMismatchCount(Locus.A, 0)
                    .WithLocusMismatchCount(Locus.B, 0)
                    .WithLocusMismatchCount(Locus.Drb1, 0)
                    .WithLocusMismatchCount(Locus.C, 0)
                    .WithLocusMismatchCount(Locus.Dqb1, 0)
                    .WithSearchHla(searchHla)
                    .Build();

                var results = await AlgorithmTestingService.Search(searchRequest);
                patientResults.Add(new PatientApiResult
                {
                    ExpectedDonorProvider = patientDataFactory,
                    ApiResult = results,
                });
            }

            scenarioContext.Set(patientResults);
        }

        [When(@"I run a 10/10 search")]
        public async Task WhenIRunATenOutOfTenSearch()
        {
            var patientDataProvider = scenarioContext.Get<IPatientDataProvider>();
            var searchRequestBuilder = scenarioContext.Get<SearchRequestBuilder>();
            var searchHla = patientDataProvider.GetPatientHla();

            var searchRequest = searchRequestBuilder
                .WithTotalMismatchCount(0)
                .WithLocusMismatchCount(Locus.A, 0)
                .WithLocusMismatchCount(Locus.B, 0)
                .WithLocusMismatchCount(Locus.Drb1, 0)
                .WithLocusMismatchCount(Locus.C, 0)
                .WithLocusMismatchCount(Locus.Dqb1, 0)
                .WithSearchHla(searchHla)
                .Build();

            scenarioContext.Set(await AlgorithmTestingService.Search(searchRequest));
        }

        [When(@"I run a 9/10 search at locus (.*)")]
        public async Task WhenIRunANineOutOfTenSearchAtLocus(string locusString)
        {
            var patientDataProvider = scenarioContext.Get<IPatientDataProvider>();
            var searchRequestBuilder = scenarioContext.Get<SearchRequestBuilder>();

            var searchHla = patientDataProvider.GetPatientHla();
            var locus = (Locus) Enum.Parse(typeof(Locus), locusString, true);
            var fullyMatchedLoci = LocusSettings.MatchingOnlyLoci.Except(new[] {locus});

            var searchRequest = searchRequestBuilder
                .WithTotalMismatchCount(1)
                .WithLocusMismatchCount(locus, 1)
                .WithMismatchCountAtLoci(fullyMatchedLoci, 0)
                .WithSearchHla(searchHla)
                .Build();

            scenarioContext.Set(await AlgorithmTestingService.Search(searchRequest));
        }

        [When(@"I run a 8/10 search")]
        [When(@"I run an 8/10 search")]
        public async Task WhenIRunAnEightOutOfTenSearch()
        {
            var patientDataProvider = scenarioContext.Get<IPatientDataProvider>();
            var searchRequestBuilder = scenarioContext.Get<SearchRequestBuilder>();
            var searchHla = patientDataProvider.GetPatientHla();
            var allowedMismatchLoci = LocusSettings.MatchingOnlyLoci;

            var searchRequest = searchRequestBuilder
                .WithTotalMismatchCount(2)
                .WithMismatchCountAtLoci(allowedMismatchLoci, 2)
                .WithSearchHla(searchHla)
                .Build();

            scenarioContext.Set(await AlgorithmTestingService.Search(searchRequest));
        }

        [Then(@"The result should contain at least one donor")]
        public void ThenTheResultShouldContainAtLeastOneDonor()
        {
            var apiResult = scenarioContext.Get<SearchAlgorithmApiResult>();
            apiResult.IsSuccess.Should().BeTrue();
            apiResult.Results.SearchResults.Count().Should().BeGreaterThan(0);
        }

        [Then(@"the results should contain the specified donor")]
        public void ThenTheResultShouldContainTheSpecifiedDonor()
        {
            var expectedDonorProvider = scenarioContext.Get<IExpectedDonorProvider>();
            var apiResult = scenarioContext.Get<SearchAlgorithmApiResult>();
            apiResult.IsSuccess.Should().BeTrue();

            apiResult.Results.SearchResults.Should().Contain(r => r.DonorId == expectedDonorProvider.GetExpectedMatchingDonorIds().Single());
        }

        [Then(@"the results should contain all specified donors")]
        public void ThenTheResultShouldContainAllSpecifiedDonors()
        {
            var expectedDonorProvider = scenarioContext.Get<IExpectedDonorProvider>();
            var apiResult = scenarioContext.Get<SearchAlgorithmApiResult>();
            apiResult.IsSuccess.Should().BeTrue();

            foreach (var donorId in expectedDonorProvider.GetExpectedMatchingDonorIds())
            {
                apiResult.Results.SearchResults.Should().Contain(r => r.DonorId == donorId);
            }
        }

        [Then(@"the results should not contain the specified donor")]
        public void ThenTheResultShouldNotContainTheSpecifiedDonor()
        {
            var expectedDonorProvider = scenarioContext.Get<IExpectedDonorProvider>();
            var apiResult = scenarioContext.Get<SearchAlgorithmApiResult>();
            apiResult.IsSuccess.Should().BeTrue();

            apiResult.Results.SearchResults.Should().NotContain(r => r.DonorId == expectedDonorProvider.GetExpectedMatchingDonorIds().Single());
        }

        [Then(@"each set of results should contain the specified donor")]
        public void ThenEachSetOfResultsShouldContainTheSpecifiedDonor()
        {
            var patientApiResults = scenarioContext.Get<List<PatientApiResult>>();

            foreach (var apiResult in patientApiResults)
            {
                apiResult.ApiResult.IsSuccess.Should().BeTrue();
                var searchResultsForPatient = apiResult.ApiResult.Results.SearchResults;
                var expectedDonorProvider = apiResult.ExpectedDonorProvider;
                searchResultsForPatient.Should().Contain(r => r.DonorId == expectedDonorProvider.GetExpectedMatchingDonorIds().Single());
            }
        }

        [Then(@"The result should contain no donors")]
        public void ThenTheResultShouldContainNoDonors()
        {
            var apiResult = scenarioContext.Get<SearchAlgorithmApiResult>();
            apiResult.IsSuccess.Should().BeTrue();
            apiResult.Results.SearchResults.Count().Should().Be(0);
        }
    }
}