using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Services.Search;
using Nova.SearchAlgorithm.Test.Integration.TestData;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using Nova.Utils.Models;
using NUnit.Framework;
using System.Collections.Generic;
using Locus = Nova.SearchAlgorithm.Common.Models.Locus;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests.Search
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
        public void ResolveSearchService()
        {
            searchDispatcher = DependencyInjection.DependencyInjection.Provider.GetService<ISearchDispatcher>();

            searchRequest = new SearchRequestBuilder()
                .WithSearchType(DonorType.Adult)
                .WithTotalMismatchCount(0)
                .WithMismatchCountAtLoci(new List<Locus> { Locus.A, Locus.B, Locus.Drb1 }, 0)
                .WithSearchHla(new TestHla.HeterozygousSet1().SixLocus_SingleExpressingAlleles)
                .ForRegistries(new List<RegistryCode> { RegistryCode.AN })
                .WithLociExcludedFromScoringAggregates(new List<LocusType>())
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
                LociToExcludeFromAggregateScore = searchRequest.LociToExcludeFromAggregateScore,
                RegistriesToSearch = searchRequest.RegistriesToSearch
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

        [TestCase(null)]
        [TestCase(-1)]
        [TestCase(5)]
        public void DispatchSearch_DonorMismatchCountIsInvalid_ThrowsValidationError(int? donorMismatchCount)
        {
            searchRequest.MatchCriteria.DonorMismatchCount = donorMismatchCount;

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [Test]
        public void DispatchSearch_RegistriesToSearchIsNull_ThrowsValidationError()
        {
            searchRequest.RegistriesToSearch = null;

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [Test]
        public void DispatchSearch_RegistriesToSearchIsEmpty_ThrowsValidationError()
        {
            searchRequest.RegistriesToSearch = new List<RegistryCode>();

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
            searchRequest.LociToExcludeFromAggregateScore = new List<LocusType> { LocusType.Dpb1 };

            Assert.DoesNotThrowAsync(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }

        [Test]
        public void DispatchSearch_LociToExcludeFromAggregateScoreContainsNonAlgorithmLocus_ThrowsValidationError()
        {
            searchRequest.LociToExcludeFromAggregateScore = new List<LocusType> { LocusType.Drb3 };

            Assert.ThrowsAsync<ValidationException>(
                async () => await searchDispatcher.DispatchSearch(searchRequest));
        }
    }
}
