using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.AzureStorage.Blob;
using Atlas.RepeatSearch.Data.Repositories;
using Atlas.RepeatSearch.Services.ResultSetTracking;
using Atlas.RepeatSearch.Test.TestHelpers.Builders;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.RepeatSearch.Test.Services.ResultSetTracking
{
    [TestFixture]
    public class OriginalSearchResultSetTrackerTests
    {
        private IBlobDownloader blobDownloader;
        private ICanonicalResultSetRepository canonicalResultSetRepository;

        private IOriginalSearchResultSetTracker originalSearchResultSetTracker;

        private const string DefaultSearchRequestId = "default-request-id";

        [SetUp]
        public void SetUp()
        {
            blobDownloader = Substitute.For<IBlobDownloader>();
            canonicalResultSetRepository = Substitute.For<ICanonicalResultSetRepository>();

            originalSearchResultSetTracker = new OriginalSearchResultSetTracker(blobDownloader, canonicalResultSetRepository);
        }

        [Test]
        public async Task ApplySearchResultDiff_DeletesAllRemovedDonorsFromSet()
        {
            var diff = SearchResultDifferentialBuilder.New.WithRemovedResults("removed-1", "removed-2").Build();

            await originalSearchResultSetTracker.ApplySearchResultDiff(DefaultSearchRequestId, diff);

            await canonicalResultSetRepository.Received().RemoveResultsFromSet(DefaultSearchRequestId, diff.RemovedResults);
        }

        [Test]
        public async Task ApplySearchResultDiff_AddsAllNewDonorsToSet()
        {
            var diff = SearchResultDifferentialBuilder.New.WithNewResults("removed-1", "removed-2").Build();

            await originalSearchResultSetTracker.ApplySearchResultDiff(DefaultSearchRequestId, diff);

            await canonicalResultSetRepository.Received()
                .AddResultsToSet(
                    DefaultSearchRequestId,
                    Arg.Is<IReadOnlyCollection<string>>(added => added.SequenceEqual(diff.NewResults.Select(d => d.ExternalDonorCode).ToList()))
                );
        }

        [Test]
        public async Task ApplySearchResultDiff_WithUpdatedDonors_NeitherRemovesNorAddsToSet()
        {
            var diff = SearchResultDifferentialBuilder.New.WithUpdatedResults("removed-1", "removed-2").Build();

            await originalSearchResultSetTracker.ApplySearchResultDiff(DefaultSearchRequestId, diff);

            await canonicalResultSetRepository.DidNotReceiveWithAnyArgs().AddResultsToSet(default, default);
            await canonicalResultSetRepository.DidNotReceiveWithAnyArgs().RemoveResultsFromSet(default, default);
        }
    }
}