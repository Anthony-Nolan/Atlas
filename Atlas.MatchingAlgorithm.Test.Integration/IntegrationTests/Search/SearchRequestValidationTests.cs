using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Test.Integration.Resources.TestData;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Search
{
    /// <summary>
    /// Tests search request validation, except for missing search HLA rules which are covered by their own fixture.
    /// </summary>
    [TestFixture]
    public class SearchRequestValidationTests
    {
        private ISearchDispatcher searchDispatcher;
        private MatchingRequest matchingRequest;

        [SetUp]
        public void SetUp()
        {
            var searchServiceBusClient = DependencyInjection.DependencyInjection.Provider.GetService<ISearchServiceBusClient>();

            searchDispatcher = new SearchDispatcher(searchServiceBusClient);

            matchingRequest = new MatchingRequestBuilder()
                .WithSearchType(DonorType.Adult)
                .WithTotalMismatchCount(0)
                .WithMismatchCountAtLoci(new List<Locus> { Locus.A, Locus.B, Locus.Drb1 }, 0)
                .WithSearchHla(new SampleTestHlas.HeterozygousSet1().SixLocus_SingleExpressingAlleles)
                .WithLociToScore(new List<Locus>())
                .WithLociExcludedFromScoringAggregates(new List<Locus>())
                .Build();
        }

        [Test]
        public void DispatchSearch_AllMandatoryFieldsHaveValidValues_DoesNotThrowValidationError()
        {
            Assert.DoesNotThrowAsync(async () => await searchDispatcher.DispatchSearch(matchingRequest));
        }

        [Test]
        public void DispatchSearch_SearchTypeIsEmpty_ThrowsValidationError()
        {
            var searchRequestWithoutSearchType = new MatchingRequest
            {
                MatchCriteria = matchingRequest.MatchCriteria,
                SearchHlaData = matchingRequest.SearchHlaData,
                LociToExcludeFromAggregateScore = matchingRequest.LociToExcludeFromAggregateScore
            };

            Assert.ThrowsAsync<ValidationException>(async () => await searchDispatcher.DispatchSearch(searchRequestWithoutSearchType));
        }

        [Test]
        public void DispatchSearch_MatchCriteriaIsEmpty_ThrowsValidationError()
        {
            matchingRequest.MatchCriteria = null;

            Assert.ThrowsAsync<ValidationException>(async () => await searchDispatcher.DispatchSearch(matchingRequest));
        }

        [Test]
        public void DispatchSearch_MatchCriteriaIsEmptyAtLocusA_ThrowsValidationError()
        {
            matchingRequest.MatchCriteria.LocusMismatchCounts.A = null;

            Assert.ThrowsAsync<ValidationException>(async () => await searchDispatcher.DispatchSearch(matchingRequest));
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void DispatchSearch_MismatchCountInvalidAtLocusA_ThrowsValidationError(int mismatchCount)
        {
            matchingRequest.MatchCriteria.LocusMismatchCounts.A = mismatchCount;

            Assert.ThrowsAsync<ValidationException>(async () => await searchDispatcher.DispatchSearch(matchingRequest));
        }

        [Test]
        public void DispatchSearch_MatchCriteriaIsEmptyAtLocusB_ThrowsValidationError()
        {
            matchingRequest.MatchCriteria.LocusMismatchCounts.B = null;

            Assert.ThrowsAsync<ValidationException>(async () => await searchDispatcher.DispatchSearch(matchingRequest));
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void DispatchSearch_MismatchCountInvalidAtLocusB_ThrowsValidationError(int mismatchCount)
        {
            matchingRequest.MatchCriteria.LocusMismatchCounts.B = mismatchCount;

            Assert.ThrowsAsync<ValidationException>(async () => await searchDispatcher.DispatchSearch(matchingRequest));
        }

        [Test]
        public void DispatchSearch_MatchCriteriaIsEmptyAtLocusDrb1_ThrowsValidationError()
        {
            matchingRequest.MatchCriteria.LocusMismatchCounts.Drb1 = null;

            Assert.ThrowsAsync<ValidationException>(async () => await searchDispatcher.DispatchSearch(matchingRequest));
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void DispatchSearch_MismatchCountInvalidAtLocusDrb1_ThrowsValidationError(int mismatchCount)
        {
            matchingRequest.MatchCriteria.LocusMismatchCounts.Drb1 = mismatchCount;

            Assert.ThrowsAsync<ValidationException>(async () => await searchDispatcher.DispatchSearch(matchingRequest));
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void DispatchSearch_MismatchCountInvalidAtLocusC_ThrowsValidationError(int mismatchCount)
        {
            matchingRequest.MatchCriteria.LocusMismatchCounts.C = mismatchCount;

            Assert.ThrowsAsync<ValidationException>(async () => await searchDispatcher.DispatchSearch(matchingRequest));
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void DispatchSearch_MismatchCountInvalidAtLocusDqb1_ThrowsValidationError(int mismatchCount)
        {
            matchingRequest.MatchCriteria.LocusMismatchCounts.Dqb1 = mismatchCount;

            Assert.ThrowsAsync<ValidationException>(async () => await searchDispatcher.DispatchSearch(matchingRequest));
        }

        [TestCase(-1)]
        [TestCase(5)]
        public void DispatchSearch_DonorMismatchCountIsInvalid_ThrowsValidationError(int donorMismatchCount)
        {
            matchingRequest.MatchCriteria.DonorMismatchCount = donorMismatchCount;

            Assert.ThrowsAsync<ValidationException>(async () => await searchDispatcher.DispatchSearch(matchingRequest));
        }

        [Test]
        public void DispatchSearch_LociToScoreIsNull_ThrowsValidationError()
        {
            matchingRequest.LociToScore = null;

            Assert.ThrowsAsync<ValidationException>(async () => await searchDispatcher.DispatchSearch(matchingRequest));
        }

        [Test]
        public void DispatchSearch_LociToScoreContainsAlgorithmLocus_DoesNotThrowValidationError()
        {
            matchingRequest.LociToScore = new List<Locus> { Locus.Dpb1 };

            Assert.DoesNotThrowAsync(async () => await searchDispatcher.DispatchSearch(matchingRequest));
        }

        [Test]
        public void DispatchSearch_LociToExcludeFromAggregateScoreIsNull_ThrowsValidationError()
        {
            matchingRequest.LociToExcludeFromAggregateScore = null;

            Assert.ThrowsAsync<ValidationException>(async () => await searchDispatcher.DispatchSearch(matchingRequest));
        }

        [Test]
        public void DispatchSearch_LociToExcludeFromAggregateScoreContainsAlgorithmLocus_DoesNotThrowValidationError()
        {
            matchingRequest.LociToExcludeFromAggregateScore = new List<Locus> { Locus.Dpb1 };

            Assert.DoesNotThrowAsync(async () => await searchDispatcher.DispatchSearch(matchingRequest));
        }
    }
}