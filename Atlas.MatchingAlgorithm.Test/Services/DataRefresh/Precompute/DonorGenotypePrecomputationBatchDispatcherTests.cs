using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ServiceBus;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh.Precompute;

[TestFixture]
public class DonorGenotypePrecomputationBatchDispatcherTests
{
    /// <summary>More reads than any dispatch in these tests needs. A dispatch that does not move on fails here, not hangs.</summary>
    private const int MaxReadsPerDispatch = 20;

    private Fixture fixture;
    private DonorGenotypePrecomputationRunLocation run;
    private IStaticallyChosenDatabaseRepositoryFactory repositoryFactory;
    private IDonorGenotypePrecomputationRepository repository;
    private IMessageBatchPublisher<DonorGenotypePrecomputationBatchRequest> publisher;
    private FakeLogger<DonorGenotypePrecomputationBatchDispatcher> logger;

    /// <summary>The messages of each publish call, in call order.</summary>
    private List<List<DonorGenotypePrecomputationBatchRequest>> publishedChunks;

    private IDonorGenotypePrecomputationBatchDispatcher dispatcher;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        run = fixture.Create<DonorGenotypePrecomputationRunLocation>();

        repository = Substitute.For<IDonorGenotypePrecomputationRepository>();
        repositoryFactory = Substitute.For<IStaticallyChosenDatabaseRepositoryFactory>();
        repositoryFactory.GetDonorGenotypePrecomputationRepositoryForDatabase(run.TargetDatabase).Returns(repository);

        publishedChunks = [];
        publisher = Substitute.For<IMessageBatchPublisher<DonorGenotypePrecomputationBatchRequest>>();
        // Null only when a test sets up the publisher again: that call is not a publish.
        publisher.BatchPublish(Arg.Do<IEnumerable<DonorGenotypePrecomputationBatchRequest>>(requests =>
        {
            if (requests != null)
            {
                publishedChunks.Add(requests.ToList());
            }
        }));

        logger = new FakeLogger<DonorGenotypePrecomputationBatchDispatcher>();

        GivenPendingBatches(0);

        dispatcher = new DonorGenotypePrecomputationBatchDispatcher(repositoryFactory, publisher, logger);
    }

    [Test]
    public async Task DispatchPendingBatches_PublishesOneMessagePerPendingBatch_WithTheIdsOfTheRun()
    {
        var batches = GivenPendingBatches(3);

        await dispatcher.DispatchPendingBatches(run, PendingBatchSelection.All);

        publishedChunks.SelectMany(chunk => chunk).Should().BeEquivalentTo(batches.Select(batch => new DonorGenotypePrecomputationBatchRequest
        {
            DataRefreshRecordId = run.DataRefreshRecordId,
            RunId = run.RunId,
            BatchId = batch.BatchId
        }));
    }

    [Test]
    public async Task DispatchPendingBatches_MovesThePublishedBatchesToRequested_WithTheRetryCountsItRead()
    {
        // The retry count lets the update leave a batch that the requeue sweep sent back after this read.
        var batches = GivenPendingBatches(3);

        await dispatcher.DispatchPendingBatches(run, PendingBatchSelection.All);

        await repository.Received(1).MarkBatchesRequested(
            run.RunId,
            Arg.Is<IReadOnlyCollection<PendingDonorGenotypePrecomputationBatch>>(marked => marked.SequenceEqual(batches)));
    }

    [Test]
    public async Task DispatchPendingBatches_PublishesTheMessagesBeforeItMovesTheBatches()
    {
        // A requested batch with no message would never finish. A pending batch with a message is sent again, and the
        // second message finds the batch taken.
        GivenPendingBatches(3);

        await dispatcher.DispatchPendingBatches(run, PendingBatchSelection.All);

        Received.InOrder(() =>
        {
            publisher.BatchPublish(Arg.Any<IEnumerable<DonorGenotypePrecomputationBatchRequest>>());
            repository.MarkBatchesRequested(run.RunId, Arg.Any<IReadOnlyCollection<PendingDonorGenotypePrecomputationBatch>>());
        });
    }

    [Test]
    public async Task DispatchPendingBatches_WhenThePublishFails_LeavesTheBatchesPendingAndThrows()
    {
        GivenPendingBatches(3);
        var failure = new Exception(fixture.Create<string>());
        publisher.BatchPublish(default).ThrowsAsyncForAnyArgs(failure);

        var act = () => dispatcher.DispatchPendingBatches(run, PendingBatchSelection.All);

        (await act.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(failure);
        await repository.DidNotReceiveWithAnyArgs().MarkBatchesRequested(default, default);
    }

    [Test]
    public async Task DispatchPendingBatches_ForMoreBatchesThanOneChunk_SendsThemAllAChunkAtATime()
    {
        const int extraBatchCount = 5;
        var batches = GivenPendingBatches(DonorGenotypePrecomputationBatchDispatcher.ChunkSize + extraBatchCount);

        var publishedCount = await dispatcher.DispatchPendingBatches(run, PendingBatchSelection.All);

        publishedChunks.Select(chunk => chunk.Count).Should().Equal(DonorGenotypePrecomputationBatchDispatcher.ChunkSize, extraBatchCount);
        publishedChunks.SelectMany(chunk => chunk).Select(request => request.BatchId).Should().Equal(batches.Select(batch => batch.BatchId));
        publishedCount.Should().Be(batches.Count);
    }

    [TestCase(PendingBatchSelection.All)]
    [TestCase(PendingBatchSelection.Requeued)]
    public async Task DispatchPendingBatches_ReadsThePendingBatchesThatTheSelectionTakes(PendingBatchSelection selection)
    {
        await dispatcher.DispatchPendingBatches(run, selection);

        await repository.Received().GetPendingBatches(run.RunId, selection, Arg.Any<int>(), Arg.Any<int>());
        await repository.DidNotReceive().GetPendingBatches(
            Arg.Any<int>(), Arg.Is<PendingBatchSelection>(other => other != selection), Arg.Any<int>(), Arg.Any<int>());
    }

    [Test]
    public async Task DispatchPendingBatches_ReadsTheDatabaseOfTheRun()
    {
        // The dormant database of the refresh, not the one that a later swap makes dormant.
        await dispatcher.DispatchPendingBatches(run, PendingBatchSelection.All);

        repositoryFactory.Received().GetDonorGenotypePrecomputationRepositoryForDatabase(run.TargetDatabase);
        repositoryFactory.DidNotReceive().GetDonorGenotypePrecomputationRepositoryForDatabase(run.TargetDatabase.Other());
    }

    [Test]
    public async Task DispatchPendingBatches_WhenNoBatchIsPending_PublishesNothingAndLogsNothing()
    {
        // The requeue sweep dispatches every few minutes, and almost always finds nothing.
        var publishedCount = await dispatcher.DispatchPendingBatches(run, PendingBatchSelection.Requeued);

        publishedCount.Should().Be(0);
        await publisher.DidNotReceiveWithAnyArgs().BatchPublish(default);
        logger.Collector.GetSnapshot().Should().BeEmpty();
    }

    [Test]
    public async Task DispatchPendingBatches_LogsOnceForTheWholeDispatch()
    {
        var batches = GivenPendingBatches(DonorGenotypePrecomputationBatchDispatcher.ChunkSize + 1);

        await dispatcher.DispatchPendingBatches(run, PendingBatchSelection.All);

        var log = logger.Collector.GetSnapshot().Should().ContainSingle().Which;
        log.Level.Should().Be(LogLevel.Information);
        log.GetStructuredStateValue("BatchCount").Should().Be(batches.Count.ToString());
        log.GetStructuredStateValue(nameof(PendingBatchSelection)).Should().Be(nameof(PendingBatchSelection.All));
        log.GetStructuredStateValue(nameof(DonorGenotypePrecomputationRunLocation.RunId)).Should().Be(run.RunId.ToString());
    }

    /// <summary>
    /// The repository reads pending batches after an id, in id order, as the real one does. The ids have gaps, like the ids
    /// of a run whose other batches are done.
    /// </summary>
    private IReadOnlyList<PendingDonorGenotypePrecomputationBatch> GivenPendingBatches(int count)
    {
        var batches = Enumerable.Range(1, count)
            .Select(i => new PendingDonorGenotypePrecomputationBatch(i * 3, fixture.Create<int>()))
            .ToList();

        var readCount = 0;
        repository.GetPendingBatches(default, default, default, default).ReturnsForAnyArgs(callInfo =>
        {
            if (++readCount > MaxReadsPerDispatch)
            {
                throw new InvalidOperationException("The dispatch reads the same batches again and again.");
            }

            var afterBatchId = callInfo.ArgAt<int>(2);
            var maxCount = callInfo.ArgAt<int>(3);
            return batches.Where(batch => batch.BatchId > afterBatchId).Take(maxCount).ToList();
        });

        return batches;
    }
}
