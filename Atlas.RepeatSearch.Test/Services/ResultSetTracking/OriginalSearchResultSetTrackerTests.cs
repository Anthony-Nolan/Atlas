using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.RepeatSearch.Data.Repositories;
using Atlas.RepeatSearch.Data.Settings;
using Atlas.RepeatSearch.Services.ResultSetTracking;
using Atlas.RepeatSearch.Test.TestHelpers.Builders;
using AwesomeAssertions;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.RepeatSearch.Test.Services.ResultSetTracking
{
    [TestFixture]
    public class OriginalSearchResultSetTrackerTests
    {
        private IBlobDownloader blobDownloader;
        private ICanonicalResultSetRepository canonicalResultSetRepository;
        private IAtlasLogger logger;
        private List<List<string>> insertedChunks;

        private IOriginalSearchResultSetTracker originalSearchResultSetTracker;

        private const string DefaultSearchRequestId = "default-request-id";
        private const string Container = "results-container";
        private const string ResultsFileName = "results.json";
        private const string BatchFolderName = "batch-folder";
        private const int ChunkSize = 2;

        private static readonly CanonicalResultSetStoreClaim Claim = new(1, Guid.NewGuid(), false);

        [SetUp]
        public void SetUp()
        {
            blobDownloader = Substitute.For<IBlobDownloader>();
            canonicalResultSetRepository = Substitute.For<ICanonicalResultSetRepository>();
            logger = Substitute.For<IAtlasLogger>();

            canonicalResultSetRepository.StartStoringResultSet(default).ReturnsForAnyArgs(Claim);
            // Copies each chunk, so the test does not depend on whether the tracker reuses its list.
            insertedChunks = new List<List<string>>();
            canonicalResultSetRepository
                .When(r => r.AddResultsChunk(Arg.Any<CanonicalResultSetStoreClaim>(), Arg.Any<IReadOnlyCollection<string>>()))
                .Do(call => insertedChunks.Add(call.ArgAt<IReadOnlyCollection<string>>(1).ToList()));

            originalSearchResultSetTracker = new OriginalSearchResultSetTracker(
                blobDownloader,
                canonicalResultSetRepository,
                new StoreOriginalSearchResultsBulkCopySettings { ChunkSize = ChunkSize },
                logger);
        }

        [Test]
        public async Task StoreOriginalSearchResults_ForSingleResultsFile_StreamsDonorCodesInChunksThenCompletesSet()
        {
            SetUpResultsFile("donor-1", "donor-2", "donor-3");

            await originalSearchResultSetTracker.StoreOriginalSearchResults(BuildNotification(3, batched: false));

            insertedChunks.Should().BeEquivalentTo(
                new[] { new[] { "donor-1", "donor-2" }, new[] { "donor-3" } }, o => o.WithStrictOrdering());
            Received.InOrder(() =>
            {
                canonicalResultSetRepository.StartStoringResultSet(DefaultSearchRequestId);
                canonicalResultSetRepository.AddResultsChunk(Claim, Arg.Any<IReadOnlyCollection<string>>());
                canonicalResultSetRepository.AddResultsChunk(Claim, Arg.Any<IReadOnlyCollection<string>>());
                canonicalResultSetRepository.CompleteResultSet(Claim);
            });
        }

        [Test]
        public async Task StoreOriginalSearchResults_ForBatchedResults_ReadsBatchFilesInChunksThenCompletesSet()
        {
            blobDownloader.DownloadFolderContentsFileByFile<DonorCodeOnlyResult>(Container, BatchFolderName)
                .Returns(ToAsyncEnumerable(
                    new[] { new DonorCodeOnlyResult { DonorCode = "donor-1" } },
                    new[] { new DonorCodeOnlyResult { DonorCode = "donor-2" }, new DonorCodeOnlyResult { DonorCode = "donor-3" } }));

            await originalSearchResultSetTracker.StoreOriginalSearchResults(BuildNotification(3, batched: true));

            insertedChunks.Should().BeEquivalentTo(
                new[] { new[] { "donor-1", "donor-2" }, new[] { "donor-3" } }, o => o.WithStrictOrdering());
            await blobDownloader.DidNotReceiveWithAnyArgs().OpenRead(default, default);
            await canonicalResultSetRepository.Received(1).CompleteResultSet(Claim);
        }

        [Test]
        public async Task StoreOriginalSearchResults_WhenSetIsAlreadyComplete_DoesNotReadOrInsert()
        {
            canonicalResultSetRepository.StartStoringResultSet(default).ReturnsForAnyArgs((CanonicalResultSetStoreClaim)null);

            await originalSearchResultSetTracker.StoreOriginalSearchResults(BuildNotification(3, batched: false));

            await blobDownloader.DidNotReceiveWithAnyArgs().OpenRead(default, default);
            await canonicalResultSetRepository.DidNotReceiveWithAnyArgs().AddResultsChunk(default, default);
            await canonicalResultSetRepository.DidNotReceiveWithAnyArgs().CompleteResultSet(default);
        }

        [Test]
        public async Task StoreOriginalSearchResults_WithNoResults_CompletesEmptySetWithoutReading()
        {
            await originalSearchResultSetTracker.StoreOriginalSearchResults(BuildNotification(0, batched: false));

            await blobDownloader.DidNotReceiveWithAnyArgs().OpenRead(default, default);
            await canonicalResultSetRepository.DidNotReceiveWithAnyArgs().AddResultsChunk(default, default);
            await canonicalResultSetRepository.Received(1).CompleteResultSet(Claim);
        }

        [Test]
        public async Task StoreOriginalSearchResults_WhenFileHasFewerDonorsThanNotification_ThrowsAndDoesNotCompleteSet()
        {
            SetUpResultsFile("donor-1", "donor-2");

            await originalSearchResultSetTracker
                .Invoking(t => t.StoreOriginalSearchResults(BuildNotification(3, batched: false)))
                .Should().ThrowAsync<InvalidOperationException>();

            await canonicalResultSetRepository.DidNotReceiveWithAnyArgs().CompleteResultSet(default);
        }

        [Test]
        public async Task StoreOriginalSearchResults_SendsStoredEventWithCounts()
        {
            SetUpResultsFile("donor-1", "donor-2", "donor-3");

            await originalSearchResultSetTracker.StoreOriginalSearchResults(BuildNotification(3, batched: false));

            logger.Received(1).SendEvent(
                OriginalSearchResultSetTracker.StoredEventName,
                Arg.Any<LogLevel>(),
                Arg.Is<Dictionary<string, string>>(p => p["SearchRequestId"] == DefaultSearchRequestId),
                Arg.Is<Dictionary<string, double>>(m => m["DonorCount"] == 3 && m["ChunkCount"] == 2));
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

        private void SetUpResultsFile(params string[] donorCodes)
        {
            var json = JsonConvert.SerializeObject(new
            {
                SearchRequestId = DefaultSearchRequestId,
                Results = donorCodes.Select(c => new { DonorCode = c, ScoringResult = new { GradeScore = 1 } })
            });
            blobDownloader.OpenRead(Container, ResultsFileName).Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes(json)));
        }

        private static MatchingResultsNotification BuildNotification(int numberOfResults, bool batched) => new()
        {
            SearchRequestId = DefaultSearchRequestId,
            WasSuccessful = true,
            NumberOfResults = numberOfResults,
            BlobStorageContainerName = Container,
            ResultsFileName = ResultsFileName,
            ResultsBatched = batched,
            BatchFolderName = batched ? BatchFolderName : null
        };

#pragma warning disable CS1998 // Test data: nothing to await.
        private static async IAsyncEnumerable<IEnumerable<T>> ToAsyncEnumerable<T>(params IEnumerable<T>[] files)
#pragma warning restore CS1998
        {
            foreach (var file in files)
            {
                yield return file;
            }
        }
    }
}