using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Test.Integration.Resources.TestData;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Search
{
    /// <summary>
    /// Tests search request validation, except for missing search HLA rules which are covered by their own fixture.
    /// </summary>
    [TestFixture]
    public class SearchRequestValidationTests
    {
        private ISearchDispatcher searchDispatcher;
        private SearchRequest searchRequest;

        [SetUp]
        public void SetUp()
        {
            var searchServiceBusClient = DependencyInjection.DependencyInjection.Provider.GetService<ISearchServiceBusClient>();

            searchDispatcher = new SearchDispatcher(searchServiceBusClient);

            searchRequest = new SearchRequestBuilder()
                .WithSearchType(DonorType.Adult)
                .WithTotalMismatchCount(0)
                .WithMismatchCountAtLoci(new List<Locus> {Locus.A, Locus.B, Locus.Drb1}, 0)
                .WithSearchHla(new SampleTestHlas.HeterozygousSet1().SixLocus_SingleExpressingAlleles)
                .WithLociExcludedFromScoringAggregates(new List<Locus>())
                .Build();
        }

        [Test]
        public void DispatchSearch_AllMandatoryFieldsHaveValidValues_DoesNotThrowValidationError()
        {
            Assert.DoesNotThrowAsync(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [Test]
        public void DispatchSearch_SearchTypeIsEmpty_ThrowsValidationError()
        {
            var searchRequestWithoutSearchType = new SearchRequest
            {
                MatchCriteria = searchRequest.MatchCriteria,
                SearchHlaData = searchRequest.SearchHlaData,
                LociToExcludeFromAggregateScore = searchRequest.LociToExcludeFromAggregateScore
            };

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequestWithoutSearchType));
        }

        [Test]
        public void DispatchSearch_MatchCriteriaIsEmpty_ThrowsValidationError()
        {
            searchRequest.MatchCriteria = null;

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [Test]
        public void DispatchSearch_MatchCriteriaIsEmptyAtLocusA_ThrowsValidationError()
        {
            searchRequest.MatchCriteria.LocusMismatchA = null;

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void DispatchSearch_MismatchCountInvalidAtLocusA_ThrowsValidationError(int mismatchCount)
        {
            searchRequest.MatchCriteria.LocusMismatchA.MismatchCount = mismatchCount;

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [Test]
        public void DispatchSearch_MatchCriteriaIsEmptyAtLocusB_ThrowsValidationError()
        {
            searchRequest.MatchCriteria.LocusMismatchB = null;

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void DispatchSearch_MismatchCountInvalidAtLocusB_ThrowsValidationError(int mismatchCount)
        {
            searchRequest.MatchCriteria.LocusMismatchB.MismatchCount = mismatchCount;

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [Test]
        public void DispatchSearch_MatchCriteriaIsEmptyAtLocusDrb1_ThrowsValidationError()
        {
            searchRequest.MatchCriteria.LocusMismatchDrb1 = null;

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void DispatchSearch_MismatchCountInvalidAtLocusDrb1_ThrowsValidationError(int mismatchCount)
        {
            searchRequest.MatchCriteria.LocusMismatchDrb1.MismatchCount = mismatchCount;

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void DispatchSearch_MismatchCountInvalidAtLocusC_ThrowsValidationError(int mismatchCount)
        {
            searchRequest.MatchCriteria.LocusMismatchC = new LocusMismatchCriteria
            {
                MismatchCount = mismatchCount
            };

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void DispatchSearch_MismatchCountInvalidAtLocusDqb1_ThrowsValidationError(int mismatchCount)
        {
            searchRequest.MatchCriteria.LocusMismatchDqb1 = new LocusMismatchCriteria
            {
                MismatchCount = mismatchCount
            };

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [TestCase(-1)]
        [TestCase(5)]
        public void DispatchSearch_DonorMismatchCountIsInvalid_ThrowsValidationError(int donorMismatchCount)
        {
            searchRequest.MatchCriteria.DonorMismatchCount = donorMismatchCount;

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [Test]
        public void DispatchSearch_LociToExcludeFromAggregateScoreIsNull_ThrowsValidationError()
        {
            searchRequest.LociToExcludeFromAggregateScore = null;

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [Test]
        public void DispatchSearch_LociToExcludeFromAggregateScoreContainsAlgorithmLocus_DoesNotThrowValidationError()
        {
            searchRequest.LociToExcludeFromAggregateScore = new List<Locus> { Locus.Dpb1};

            Assert.DoesNotThrowAsync(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }
    }
}