using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.RepeatSearch.Data.Models;
using Atlas.RepeatSearch.Data.Repositories;
using Atlas.RepeatSearch.Services.Search;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.RepeatSearch.Test.Services.Search
{
    [TestFixture]
    public class RepeatSearchDifferentialCalculatorTests
    {
        private IDonorReader donorReader;
        private ICanonicalResultSetRepository canonicalResultSetRepository;

        private IRepeatSearchDifferentialCalculator differentialCalculator;

        private const string DefaultSearchRequestId = "default-search-id";
        private readonly DateTimeOffset defaultCutoff = DateTimeOffset.UtcNow;

        [SetUp]
        public void SetUp()
        {
            donorReader = Substitute.For<IDonorReader>();
            canonicalResultSetRepository = Substitute.For<ICanonicalResultSetRepository>();

            // By default, no donors have been deleted - can be overridden test-by-test
            donorReader.GetDonorsByExternalDonorCodes(default)
                .ReturnsForAnyArgs(c => c.Arg<IEnumerable<string>>().ToDictionary(x => x, x => new Donor {ExternalDonorCode = x}));

            differentialCalculator = new RepeatSearchDifferentialCalculator(donorReader, canonicalResultSetRepository);
        }

        [Test]
        public async Task CalculateDifferential_WithNoChange_ReturnsEmptyDiff()
        {
            var diff = await differentialCalculator.CalculateDifferential(DefaultSearchRequestId, new List<MatchingAlgorithmResult>(), defaultCutoff);

            diff.NewResults.Should().BeEmpty();
            diff.UpdatedResults.Should().BeEmpty();
            diff.RemovedResults.Should().BeEmpty();
        }

        [Test]
        public async Task CalculateDifferential_WithNewResults_ReturnsNewDonors()
        {
            var changedDonorCodes = new[] {"donor1", "donor2"};
            donorReader.GetDonorIdsUpdatedSince(defaultCutoff)
                .Returns(changedDonorCodes.ToDictionary(code => code, _ => IncrementingIdGenerator.NextIntId()));

            var matchingResults = changedDonorCodes.Select(code => new MatchingAlgorithmResult {ExternalDonorCode = code}).ToList();

            var diff = await differentialCalculator.CalculateDifferential(DefaultSearchRequestId, matchingResults, defaultCutoff);

            diff.NewResults.Select(x => x.ExternalDonorCode).Should().BeEquivalentTo(changedDonorCodes);
            diff.UpdatedResults.Should().BeEmpty();
            diff.RemovedResults.Should().BeEmpty();
        }

        [Test]
        public async Task CalculateDifferential_WithNoChangesToResults_OnlyPopulatesUpdatedDonors()
        {
            var changedDonorCodes = new[] {"donor1", "donor2"};
            donorReader.GetDonorIdsUpdatedSince(defaultCutoff)
                .Returns(changedDonorCodes.ToDictionary(code => code, _ => IncrementingIdGenerator.NextIntId()));
            canonicalResultSetRepository.GetCanonicalResults(DefaultSearchRequestId)
                .Returns(changedDonorCodes.Select(code => new SearchResult {ExternalDonorCode = code}).ToList());

            var matchingResults = changedDonorCodes.Select(code => new MatchingAlgorithmResult {ExternalDonorCode = code}).ToList();

            var diff = await differentialCalculator.CalculateDifferential(DefaultSearchRequestId, matchingResults, defaultCutoff);

            diff.NewResults.Should().BeEmpty();
            diff.UpdatedResults.Select(x => x.ExternalDonorCode).Should().BeEquivalentTo(changedDonorCodes);
            diff.RemovedResults.Should().BeEmpty();
        }

        [Test]
        public async Task CalculateDifferential_WithDonorsNoLongerMatching_PopulatesRemovedResults()
        {
            var newlyInvalidDonors = new[] {"donor1", "donor2"};
            donorReader.GetDonorIdsUpdatedSince(defaultCutoff)
                .Returns(newlyInvalidDonors.ToDictionary(code => code, _ => IncrementingIdGenerator.NextIntId()));
            canonicalResultSetRepository.GetCanonicalResults(DefaultSearchRequestId)
                .Returns(newlyInvalidDonors.Select(code => new SearchResult {ExternalDonorCode = code}).ToList());

            var matchingResults = new List<MatchingAlgorithmResult>();

            var diff = await differentialCalculator.CalculateDifferential(DefaultSearchRequestId, matchingResults, defaultCutoff);

            diff.NewResults.Should().BeEmpty();
            diff.UpdatedResults.Should().BeEmpty();
            diff.RemovedResults.Should().BeEquivalentTo(newlyInvalidDonors);
        }

        [Test]
        public async Task CalculateDifferential_WithDonorsRemovedEntirely_PopulatesRemovedResults()
        {
            var removedDonors = new[] {"donor1", "donor2"};
            donorReader.GetDonorIdsUpdatedSince(defaultCutoff).Returns(new Dictionary<string, int>());
            donorReader.GetDonorsByExternalDonorCodes(removedDonors).ReturnsForAnyArgs(new Dictionary<string, Donor>());
            canonicalResultSetRepository.GetCanonicalResults(DefaultSearchRequestId)
                .Returns(removedDonors.Select(code => new SearchResult {ExternalDonorCode = code}).ToList());

            var matchingResults = new List<MatchingAlgorithmResult>();

            var diff = await differentialCalculator.CalculateDifferential(DefaultSearchRequestId, matchingResults, defaultCutoff);

            diff.NewResults.Should().BeEmpty();
            diff.UpdatedResults.Should().BeEmpty();
            diff.RemovedResults.Should().BeEquivalentTo(removedDonors);
        }

        [Test]
        public async Task CalculateDifferential_WithACombinationOfRemovedAndNoLongerMatchingDonors_CombinesDonorsInRemovedResults()
        {
            const string removedDonor = "removed-donor-code";
            const string noLongerMatchingDonor = "no-longer-matching-donor-code";

            var bothDonors = new[] {removedDonor, noLongerMatchingDonor};

            donorReader.GetDonorIdsUpdatedSince(defaultCutoff)
                .Returns(new Dictionary<string, int> {{noLongerMatchingDonor, IncrementingIdGenerator.NextIntId()}});
            donorReader.GetDonorsByExternalDonorCodes(bothDonors)
                .ReturnsForAnyArgs(new Dictionary<string, Donor> {{noLongerMatchingDonor, new Donor {ExternalDonorCode = noLongerMatchingDonor}}});
            canonicalResultSetRepository.GetCanonicalResults(DefaultSearchRequestId)
                .Returns(bothDonors.Select(code => new SearchResult {ExternalDonorCode = code}).ToList());

            var matchingResults = new List<MatchingAlgorithmResult>();

            var diff = await differentialCalculator.CalculateDifferential(DefaultSearchRequestId, matchingResults, defaultCutoff);

            diff.NewResults.Should().BeEmpty();
            diff.UpdatedResults.Should().BeEmpty();
            diff.RemovedResults.Should().BeEquivalentTo(bothDonors);
        }

        [Test]
        public async Task CalculateDifferential_WithACombinationOfDonorTypes_PopulatesCorrectDiff()
        {
            const string removedDonor = "removed-donor-code";
            const string noLongerMatchingDonor = "no-longer-matching-donor-code";
            const string updatedButStillMatchingDonor = "updated-matching-donor-code";
            const string nonUpdatedMatchingDonor = "non-updated-matching-donor-code";
            const string nonUpdatedNonMatchingDonor = "non-updated-non-matching-donor-code";
            const string newDonor = "new-donor-code";
            const string newlyMatchingDonor = "newly-matching-donor-code";

            var updatedDonors = new[] {noLongerMatchingDonor, updatedButStillMatchingDonor, newDonor, newlyMatchingDonor};
            var previouslyReturnedDonors = new[] {removedDonor, noLongerMatchingDonor, updatedButStillMatchingDonor, nonUpdatedMatchingDonor};
            var stillExistingDonors = new[]
            {
                noLongerMatchingDonor, updatedButStillMatchingDonor, nonUpdatedMatchingDonor, nonUpdatedNonMatchingDonor, newDonor, newlyMatchingDonor
            };
            var repeatSearchResults = new[] {updatedButStillMatchingDonor, newDonor, newlyMatchingDonor};

            donorReader.GetDonorIdsUpdatedSince(defaultCutoff)
                .Returns(updatedDonors.ToDictionary(d => d, _ => IncrementingIdGenerator.NextIntId()));

            canonicalResultSetRepository.GetCanonicalResults(DefaultSearchRequestId)
                .Returns(previouslyReturnedDonors.Select(code => new SearchResult {ExternalDonorCode = code}).ToList());

            donorReader.GetDonorsByExternalDonorCodes(previouslyReturnedDonors)
                .ReturnsForAnyArgs(stillExistingDonors.ToDictionary(d => d, d => new Donor {ExternalDonorCode = d}));

            var matchingResults = repeatSearchResults.Select(code => new MatchingAlgorithmResult {ExternalDonorCode = code}).ToList();

            var diff = await differentialCalculator.CalculateDifferential(DefaultSearchRequestId, matchingResults, defaultCutoff);

            diff.NewResults.Select(x => x.ExternalDonorCode).Should().BeEquivalentTo(newDonor, newlyMatchingDonor);
            diff.UpdatedResults.Select(x => x.ExternalDonorCode).Should().BeEquivalentTo(updatedButStillMatchingDonor);
            diff.RemovedResults.Should().BeEquivalentTo(removedDonor, noLongerMatchingDonor);
        }
    }
}