using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Test.Integration.Resources.TestData;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Collections.Generic;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;
using Atlas.SearchTracking.Common.Clients;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Search
{
    /// <summary>
    /// Tests search request validation, except for missing search HLA rules which are covered by their own fixture.
    /// </summary>
    [TestFixture]
    public class SearchRequestValidationTests
    {
        private ISearchDispatcher searchDispatcher;
        private SearchRequestBuilder defaultSearchRequestBuilder;

        [SetUp]
        public void SetUp()
        {
            var searchServiceBusClient = DependencyInjection.DependencyInjection.Provider.GetService<ISearchServiceBusClient>();
            var searchTrackingServiceBusClient = DependencyInjection.DependencyInjection.Provider.GetService<ISearchTrackingServiceBusClient>();

            searchDispatcher = new SearchDispatcher(searchServiceBusClient, searchTrackingServiceBusClient);

            defaultSearchRequestBuilder = new SearchRequestBuilder()
                .WithSearchType(DonorType.Adult)
                .WithTotalMismatchCount(0)
                .WithMismatchCountAtLoci(new List<Locus> {Locus.A, Locus.B, Locus.Drb1}, 0)
                .WithSearchHla(new SampleTestHlas.HeterozygousSet1().SixLocus_SingleExpressingAlleles)
                .WithLociToScore(new List<Locus>())
                .WithLociExcludedFromScoringAggregates(new List<Locus>());
        }

        [Test]
        public void DispatchSearch_AllMandatoryFieldsHaveValidValues_DoesNotThrowValidationError()
        {
            Assert.DoesNotThrowAsync(async () => await searchDispatcher.DispatchSearch(defaultSearchRequestBuilder.Build()));
        }

        [Test]
        public void DispatchSearch_SearchTypeIsEmpty_ThrowsValidationError()
        {
            var defaultRequest = defaultSearchRequestBuilder.Build();
            var searchRequestWithoutSearchType = new SearchRequest
            {
                SearchDonorType = (Atlas.Client.Models.Search.DonorType) 0,
                MatchCriteria = defaultRequest.MatchCriteria,
                SearchHlaData = defaultRequest.SearchHlaData,
                ScoringCriteria = defaultRequest.ScoringCriteria
            };

            Assert.ThrowsAsync<ValidationException>(async () => await searchDispatcher.DispatchSearch(searchRequestWithoutSearchType));
        }

        [Test]
        public void DispatchSearch_MatchCriteriaIsEmpty_ThrowsValidationError()
        {
            var matchRequest = defaultSearchRequestBuilder.Build();
            matchRequest.MatchCriteria = null;

            Assert.ThrowsAsync<ValidationException>(async () => await searchDispatcher.DispatchSearch(matchRequest));
        }

        [TestCase(Locus.A)]
        [TestCase(Locus.B)]
        [TestCase(Locus.Drb1)]
        public void DispatchSearch_MatchCriteriaIsEmptyAtRequiredLocus_ThrowsValidationError(Locus locus)
        {
            var matchRequest = defaultSearchRequestBuilder.WithLocusMismatchCount(locus, null).Build();

            Assert.ThrowsAsync<ValidationException>(async () => await searchDispatcher.DispatchSearch(matchRequest));
        }

        [TestCase(-1, Locus.A)]
        [TestCase(3, Locus.A)]
        [TestCase(-1, Locus.B)]
        [TestCase(3, Locus.B)]
        [TestCase(-1, Locus.C)]
        [TestCase(3, Locus.C)]
        [TestCase(-1, Locus.Dqb1)]
        [TestCase(3, Locus.Dqb1)]
        [TestCase(-1, Locus.Drb1)]
        [TestCase(3, Locus.Drb1)]
        public void DispatchSearch_MismatchCountInvalidAtSingleLocus_ThrowsValidationError(int mismatchCount, Locus locus)
        {
            var matchRequest = defaultSearchRequestBuilder.WithLocusMismatchCount(locus, mismatchCount).Build();

            Assert.ThrowsAsync<ValidationException>(async () => await searchDispatcher.DispatchSearch(matchRequest));
        }

        [TestCase(-1)]
        // TODO ATLAS-865
        [TestCase(6)]
        public void DispatchSearch_DonorMismatchCountIsInvalid_ThrowsValidationError(int donorMismatchCount)
        {
            var matchRequest = defaultSearchRequestBuilder.Build();
            matchRequest.MatchCriteria.DonorMismatchCount = donorMismatchCount;

            Assert.ThrowsAsync<ValidationException>(async () => await searchDispatcher.DispatchSearch(matchRequest));
        }

        [Test]
        public void DispatchSearch_LociToScoreIsNull_ThrowsValidationError()
        {
            var matchRequest = defaultSearchRequestBuilder.Build();
            matchRequest.ScoringCriteria.LociToScore = null;

            Assert.ThrowsAsync<ValidationException>(async () => await searchDispatcher.DispatchSearch(matchRequest));
        }

        [Test]
        public void DispatchSearch_LociToScoreContainsAlgorithmLocus_DoesNotThrowValidationError()
        {
            var matchRequest = defaultSearchRequestBuilder.Build();
            matchRequest.ScoringCriteria.LociToScore = new List<Locus> {Locus.Dpb1};

            Assert.DoesNotThrowAsync(async () => await searchDispatcher.DispatchSearch(matchRequest));
        }

        [Test]
        public void DispatchSearch_LociToExcludeFromAggregateScoreIsNull_ThrowsValidationError()
        {
            var matchRequest = defaultSearchRequestBuilder.Build();
            matchRequest.ScoringCriteria.LociToExcludeFromAggregateScore = null;

            Assert.ThrowsAsync<ValidationException>(async () => await searchDispatcher.DispatchSearch(matchRequest));
        }

        [Test]
        public void DispatchSearch_LociToExcludeFromAggregateScoreContainsAlgorithmLocus_DoesNotThrowValidationError()
        {
            var matchRequest = defaultSearchRequestBuilder.Build();
            matchRequest.ScoringCriteria.LociToExcludeFromAggregateScore = new List<Locus> {Locus.Dpb1};

            Assert.DoesNotThrowAsync(async () => await searchDispatcher.DispatchSearch(matchRequest));
        }
    }
}