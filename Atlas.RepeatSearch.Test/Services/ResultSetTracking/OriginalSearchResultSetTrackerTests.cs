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
using AutoFixture;
using AwesomeAssertions;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.RepeatSearch.Test.Services.ResultSetTracking
{
    [TestFixture]
    public class OriginalSearchResultSetTrackerTests
    {
        private const int ChunkSize = 2;

        private Fixture fixture;
        private IBlobDownloader blobDownloader;
        private ICanonicalResultSetRepository canonicalResultSetRepository;
        private IAtlasLogger logger;
        private List<List<string>> insertedChunks;
        private CanonicalResultSetStoreClaim claim;
        private string searchRequestId;
        private string container;
        private string resultsFileName;
        private string batchFolderName;

        private IOriginalSearchResultSetTracker originalSearchResultSetTracker;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();
            blobDownloader = Substitute.For<IBlobDownloader>();
            canonicalResultSetRepository = Substitute.For<ICanonicalResultSetRepository>();
            logger = Substitute.For<IAtlasLogger>();

            searchRequestId = fixture.Create<string>();
            container = fixture.Create<string>();
            resultsFileName = fixture.Create<string>();
            batchFolderName = fixture.Create<string>();
            claim = new CanonicalResultSetStoreClaim(fixture.Create<int>(), Guid.NewGuid(), false);

            canonicalResultSetRepository.StartStoringResultSet(default).ReturnsForAnyArgs(claim);
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
            var donorCodes = fixture.CreateMany<string>(3).ToList();
            SetUpResultsFile(donorCodes);

            await originalSearchResultSetTracker.StoreOriginalSearchResults(BuildNotification(3, batched: false));

            insertedChunks.Should().BeEquivalentTo(
                new[] { new[] { donorCodes[0], donorCodes[1] }, new[] { donorCodes[2] } }, o => o.WithStrictOrdering());
            Received.InOrder(() =>
            {
                canonicalResultSetRepository.StartStoringResultSet(searchRequestId);
                canonicalResultSetRepository.AddResultsChunk(claim, Arg.Any<IReadOnlyCollection<string>>());
                canonicalResultSetRepository.AddResultsChunk(claim, Arg.Any<IReadOnlyCollection<string>>());
                canonicalResultSetRepository.CompleteResultSet(claim);
            });
        }

        [Test]
        public async Task StoreOriginalSearchResults_ForBatchedResults_ReadsBatchFilesInChunksThenCompletesSet()
        {
            var donorCodes = fixture.CreateMany<string>(3).ToList();
            blobDownloader.DownloadFolderContentsFileByFile<DonorCodeOnlyResult>(container, batchFolderName)
                .Returns(ToAsyncEnumerable(
                    ToResults(donorCodes.Take(1)),
                    ToResults(donorCodes.Skip(1))));

            await originalSearchResultSetTracker.StoreOriginalSearchResults(BuildNotification(3, batched: true));

            insertedChunks.Should().BeEquivalentTo(
                new[] { new[] { donorCodes[0], donorCodes[1] }, new[] { donorCodes[2] } }, o => o.WithStrictOrdering());
            await blobDownloader.DidNotReceiveWithAnyArgs().OpenRead(default, default);
            await canonicalResultSetRepository.Received(1).CompleteResultSet(claim);
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
            await canonicalResultSetRepository.Received(1).CompleteResultSet(claim);
        }

        [Test]
        public async Task StoreOriginalSearchResults_WhenFileHasFewerDonorsThanNotification_ThrowsAndDoesNotCompleteSet()
        {
            SetUpResultsFile(fixture.CreateMany<string>(2));

            var act = () => originalSearchResultSetTracker.StoreOriginalSearchResults(BuildNotification(3, batched: false));

            await act.Should().ThrowAsync<InvalidOperationException>();
            await canonicalResultSetRepository.DidNotReceiveWithAnyArgs().CompleteResultSet(default);
        }

        [Test]
        public async Task StoreOriginalSearchResults_SendsStoredEventWithCounts()
        {
            SetUpResultsFile(fixture.CreateMany<string>(3));

            await originalSearchResultSetTracker.StoreOriginalSearchResults(BuildNotification(3, batched: false));

            logger.Received(1).SendEvent(
                OriginalSearchResultSetTracker.StoredEventName,
                Arg.Any<LogLevel>(),
                Arg.Is<Dictionary<string, string>>(p => p["SearchRequestId"] == searchRequestId),
                Arg.Is<Dictionary<string, double>>(m => m["DonorCount"] == 3 && m["ChunkCount"] == 2));
        }

        [Test]
        public async Task StoreOriginalSearchResults_SendsStartTraceWithSearchSize()
        {
            SetUpResultsFile(fixture.CreateMany<string>(3));

            await originalSearchResultSetTracker.StoreOriginalSearchResults(BuildNotification(3, batched: false));

            logger.Received(1).SendTrace(
                Arg.Is<string>(m => m.StartsWith("Storing the original search results")),
                LogLevel.Info,
                Arg.Is<Dictionary<string, string>>(p =>
                    p["SearchRequestId"] == searchRequestId && p["NumberOfResults"] == "3" && p["ResultsBatched"] == "False"));
        }

        [Test]
        public async Task StoreOriginalSearchResults_SetsSearchRequestIdForTelemetryOfTheStore()
        {
            string searchRequestIdInStore = null;
            canonicalResultSetRepository
                .When(r => r.StartStoringResultSet(Arg.Any<string>()))
                .Do(_ => searchRequestIdInStore = SearchRequestContext.SearchRequestId);

            await originalSearchResultSetTracker.StoreOriginalSearchResults(BuildNotification(0, batched: false));

            searchRequestIdInStore.Should().Be(searchRequestId);
        }

        [Test]
        public async Task StoreOriginalSearchResults_WhenStoreFails_LogsExceptionWithSearchSizeAndRethrows()
        {
            SetUpResultsFile(fixture.CreateMany<string>(3));
            var exception = new TimeoutException(fixture.Create<string>());
            canonicalResultSetRepository
                .AddResultsChunk(Arg.Any<CanonicalResultSetStoreClaim>(), Arg.Any<IReadOnlyCollection<string>>())
                .ThrowsAsync(exception);

            var act = () => originalSearchResultSetTracker.StoreOriginalSearchResults(BuildNotification(3, batched: false));

            (await act.Should().ThrowAsync<TimeoutException>()).Which.Should().BeSameAs(exception);
            logger.Received(1).SendException(
                exception,
                LogLevel.Error,
                Arg.Is<Dictionary<string, string>>(p => p["SearchRequestId"] == searchRequestId && p["NumberOfResults"] == "3"));
        }

        [Test]
        public async Task ApplySearchResultDiff_DeletesAllRemovedDonorsFromSet()
        {
            var diff = SearchResultDifferentialBuilder.New.WithRemovedResults("removed-1", "removed-2").Build();

            await originalSearchResultSetTracker.ApplySearchResultDiff(searchRequestId, diff);

            await canonicalResultSetRepository.Received().RemoveResultsFromSet(searchRequestId, diff.RemovedResults);
        }

        [Test]
        public async Task ApplySearchResultDiff_AddsAllNewDonorsToSet()
        {
            var diff = SearchResultDifferentialBuilder.New.WithNewResults("removed-1", "removed-2").Build();

            await originalSearchResultSetTracker.ApplySearchResultDiff(searchRequestId, diff);

            await canonicalResultSetRepository.Received()
                .AddResultsToSet(
                    searchRequestId,
                    Arg.Is<IReadOnlyCollection<string>>(added => added.SequenceEqual(diff.NewResults.Select(d => d.ExternalDonorCode).ToList()))
                );
        }

        [Test]
        public async Task ApplySearchResultDiff_WithUpdatedDonors_NeitherRemovesNorAddsToSet()
        {
            var diff = SearchResultDifferentialBuilder.New.WithUpdatedResults("removed-1", "removed-2").Build();

            await originalSearchResultSetTracker.ApplySearchResultDiff(searchRequestId, diff);

            await canonicalResultSetRepository.DidNotReceiveWithAnyArgs().AddResultsToSet(default, default);
            await canonicalResultSetRepository.DidNotReceiveWithAnyArgs().RemoveResultsFromSet(default, default);
        }

        private void SetUpResultsFile(IEnumerable<string> donorCodes)
        {
            var json = JsonConvert.SerializeObject(new
            {
                SearchRequestId = searchRequestId,
                Results = donorCodes.Select(c => new { DonorCode = c, ScoringResult = new { GradeScore = 1 } })
            });
            blobDownloader.OpenRead(container, resultsFileName).Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes(json)));
        }

        private MatchingResultsNotification BuildNotification(int numberOfResults, bool batched) => new()
        {
            SearchRequestId = searchRequestId,
            WasSuccessful = true,
            NumberOfResults = numberOfResults,
            BlobStorageContainerName = container,
            ResultsFileName = resultsFileName,
            ResultsBatched = batched,
            BatchFolderName = batched ? batchFolderName : null
        };

        private static DonorCodeOnlyResult[] ToResults(IEnumerable<string> donorCodes) =>
            donorCodes.Select(c => new DonorCodeOnlyResult { DonorCode = c }).ToArray();

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
