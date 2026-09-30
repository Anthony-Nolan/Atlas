using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using AutoFixture;
using AwesomeAssertions;
using Azure.Messaging.ServiceBus;
using EnumStringValues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using BatchStatus = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationBatchStatus;
using RunStatus = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationRunStatus;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh.Precompute;

[TestFixture]
public class DonorGenotypePrecomputationSweeperTests
{
    private const string Finalise = nameof(IDonorGenotypePrecomputationSweeper.FinaliseCompletedRuns);
    private const string Abandon = nameof(IDonorGenotypePrecomputationSweeper.MarkAbandonedBatches);
    private const string Requeue = nameof(IDonorGenotypePrecomputationSweeper.RequeueFailedBatches);

    /// <summary>The application property names that Service Bus gives the dead-letter reason and description.</summary>
    private const string DeadLetterReasonProperty = "DeadLetterReason";
    private const string DeadLetterErrorDescriptionProperty = "DeadLetterErrorDescription";

    private Fixture fixture;
    private List<DataRefreshRecord> openRecords;
    private IDataRefreshHistoryRepository dataRefreshHistoryRepository;
    private Dictionary<TransientDatabase, IDonorGenotypePrecomputationRepository> repositories;
    private IStaticallyChosenDatabaseRepositoryFactory repositoryFactory;
    private IDonorGenotypePrecomputationBatchDispatcher dispatcher;
    private DonorGenotypePrecomputationSettings settings;
    private FakeLogger<DonorGenotypePrecomputationSweeper> logger;

    private IDonorGenotypePrecomputationSweeper sweeper;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();

        openRecords = [];
        dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();
        dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns(_ => openRecords);

        repositories = EnumExtensions.EnumerateValues<TransientDatabase>()
            .ToDictionary(database => database, _ => Substitute.For<IDonorGenotypePrecomputationRepository>());
        repositoryFactory = Substitute.For<IStaticallyChosenDatabaseRepositoryFactory>();
        repositoryFactory.GetDonorGenotypePrecomputationRepositoryForDatabase(default)
            .ReturnsForAnyArgs(callInfo => repositories[callInfo.Arg<TransientDatabase>()]);

        dispatcher = Substitute.For<IDonorGenotypePrecomputationBatchDispatcher>();
        settings = fixture.Create<DonorGenotypePrecomputationSettings>();
        logger = new FakeLogger<DonorGenotypePrecomputationSweeper>();

        sweeper = new DonorGenotypePrecomputationSweeper(dataRefreshHistoryRepository, repositoryFactory, dispatcher, settings, logger);
    }

    [TestCase(Finalise)]
    [TestCase(Abandon)]
    [TestCase(Requeue)]
    public async Task Sweep_WhenNoRefreshIsOpen_ReadsNoTransientDatabase(string sweep)
    {
        await RunSweep(sweep);

        repositoryFactory.DidNotReceiveWithAnyArgs().GetDonorGenotypePrecomputationRepositoryForDatabase(default);
    }

    [TestCase(Finalise)]
    [TestCase(Abandon)]
    [TestCase(Requeue)]
    public async Task Sweep_BeforeTheOpenRefreshHasRecreatedItsIndexes_ReadsNoTransientDatabase(string sweep)
    {
        // No run exists yet, and the dormant database can be scaled down and paused: a query every few minutes would keep
        // it awake.
        openRecords.Add(DataRefreshRecordBuilder.New
            .With(record => record.Id, fixture.Create<int>())
            .WithStagesCompletedUpToButNotIncluding(DataRefreshStage.IndexRecreation)
            .Build());

        await RunSweep(sweep);

        repositoryFactory.DidNotReceiveWithAnyArgs().GetDonorGenotypePrecomputationRepositoryForDatabase(default);
    }

    [Test]
    public async Task Sweep_WhenTheRunIsNotRunning_ChangesNothing(
        [Values(Finalise, Abandon, Requeue)] string sweep,
        [Values(RunStatus.Building, RunStatus.Completed, RunStatus.CompletedWithFailures, RunStatus.Cancelled)] RunStatus status)
    {
        // A building run has no batches to sweep yet. A finished or cancelled run must not change: a completed run is
        // what the stage waits for, and the workers skip the messages of a cancelled one.
        var (record, _) = GivenAnOpenRefreshWithARun(status: status);

        await RunSweep(sweep);

        await ShouldHaveChangedNothing(Repository(record));
    }

    [TestCase(Finalise)]
    [TestCase(Abandon)]
    [TestCase(Requeue)]
    public async Task Sweep_WhenTheRefreshHasNoRun_ChangesNothing(string sweep)
    {
        var (record, _) = GivenAnOpenRefreshWithARun();
        Repository(record).GetRun(record.Id).Returns((DonorGenotypePrecomputationRun)null);

        await RunSweep(sweep);

        await ShouldHaveChangedNothing(Repository(record));
    }

    [TestCase(Finalise)]
    [TestCase(Abandon)]
    [TestCase(Requeue)]
    public async Task Sweep_WhenOneRefreshFails_StillSweepsTheOthers_AndThenThrows(string sweep)
    {
        // Only one refresh is open at a time, but a failure must not hide the work of the others if that ever changes.
        var (failingRecord, _) = GivenAnOpenRefreshWithARun(TransientDatabase.DatabaseB);
        var (healthyRecord, healthyRun) = GivenAnOpenRefreshWithARun();
        Repository(failingRecord).GetRun(failingRecord.Id).ThrowsAsync(new Exception(fixture.Create<string>()));

        var act = () => RunSweep(sweep);

        (await act.Should().ThrowAsync<AggregateException>()).Which.InnerExceptions.Should().ContainSingle();
        await ShouldHaveSwept(sweep, Repository(healthyRecord), healthyRun);
    }

    [Test]
    public async Task FinaliseCompletedRuns_FinalisesTheRunInTheDatabaseOfItsRefresh()
    {
        var (record, run) = GivenAnOpenRefreshWithARun(TransientDatabase.DatabaseB);

        await sweeper.FinaliseCompletedRuns();

        await Repository(record).Received(1).TryFinaliseRun(run.Id);
        await repositories[TransientDatabase.DatabaseA].DidNotReceiveWithAnyArgs().TryFinaliseRun(default);
    }

    [TestCase(RunStatus.Completed, LogLevel.Information)]
    [TestCase(RunStatus.CompletedWithFailures, LogLevel.Warning)]
    public async Task FinaliseCompletedRuns_WhenTheRunIsFinalised_LogsTheCountsOnce(RunStatus finalStatus, LogLevel expectedLevel)
    {
        var (record, run) = GivenAnOpenRefreshWithARun();
        Repository(record).TryFinaliseRun(run.Id).Returns(finalStatus);
        var counts = new DonorGenotypePrecomputationBatchCounts(
            new Dictionary<BatchStatus, int>
            {
                [BatchStatus.ResultsReceived] = fixture.Create<int>(),
                [BatchStatus.PermanentlyFailed] = fixture.Create<int>()
            },
            fixture.Create<int>());
        Repository(record).GetBatchCounts(run.Id).Returns(counts);

        await sweeper.FinaliseCompletedRuns();

        var log = logger.Collector.GetSnapshot().Should().ContainSingle().Which;
        log.Level.Should().Be(expectedLevel);
        log.GetStructuredStateValue("Status").Should().Be(finalStatus.ToString());
        log.GetStructuredStateValue("RunId").Should().Be(run.Id.ToString());
        log.GetStructuredStateValue("BatchCount").Should().Be(counts.TotalBatchCount.ToString());
        log.GetStructuredStateValue("PermanentlyFailedBatchCount").Should().Be(counts.CountOf(BatchStatus.PermanentlyFailed).ToString());
        log.GetStructuredStateValue("FailedGroupCount").Should().Be(counts.FailedGroupCount.ToString());
    }

    [Test]
    public async Task FinaliseCompletedRuns_WhileABatchIsNotDone_LogsNothing()
    {
        // The overwhelmingly common case: the timer runs every few minutes for the whole of the stage.
        var (record, run) = GivenAnOpenRefreshWithARun();
        Repository(record).TryFinaliseRun(run.Id).Returns((RunStatus?)null);

        await sweeper.FinaliseCompletedRuns();

        logger.Collector.GetSnapshot().Should().BeEmpty();
        await Repository(record).DidNotReceiveWithAnyArgs().GetBatchCounts(default);
    }

    [Test]
    public async Task MarkAbandonedBatches_AbandonsTheBatchesWhoseLeaseExpired()
    {
        var (record, run) = GivenAnOpenRefreshWithARun();

        await sweeper.MarkAbandonedBatches();

        await Repository(record).Received(1).MarkExpiredBatchesAbandoned(run.Id);
    }

    [Test]
    public async Task MarkAbandonedBatches_WhenNoLeaseExpired_LogsNothing()
    {
        GivenAnOpenRefreshWithARun();

        await sweeper.MarkAbandonedBatches();

        logger.Collector.GetSnapshot().Should().BeEmpty();
    }

    [Test]
    public async Task RequeueFailedBatches_GivesUp_ThenRequeues_ThenSendsTheRequeuedBatches()
    {
        // Giving up first leaves only retryable batches to requeue, and a batch is sent only after it is back to pending.
        var (record, run) = GivenAnOpenRefreshWithARun();
        var repository = Repository(record);

        await sweeper.RequeueFailedBatches();

        Received.InOrder(() =>
        {
            repository.MarkBatchesPermanentlyFailed(run.Id, settings.MaxBatchRetries);
            repository.RequeueRetryableBatches(run.Id, settings.MaxBatchRetries);
            dispatcher.DispatchPendingBatches(Location(record, run), PendingBatchSelection.Requeued);
        });
    }

    [Test]
    public async Task RequeueFailedBatches_SendsOnlyTheBatchesThatASweepSentBack()
    {
        // The other pending batches are the stage's to send: after a manual retry, only once the refresh continues and
        // has scaled the database up again.
        GivenAnOpenRefreshWithARun();

        await sweeper.RequeueFailedBatches();

        await dispatcher.DidNotReceive().DispatchPendingBatches(Arg.Any<DonorGenotypePrecomputationRunLocation>(), PendingBatchSelection.All);
    }

    [Test]
    public async Task RequeueFailedBatches_WhenNoBatchIsSentBack_StillSendsTheBatchesThatASweepSentBack()
    {
        // A sweep that stopped between its requeue and its publish left its batches pending. The next sweep sends them.
        var (record, run) = GivenAnOpenRefreshWithARun();
        Repository(record).RequeueRetryableBatches(run.Id, settings.MaxBatchRetries).Returns([]);

        await sweeper.RequeueFailedBatches();

        await dispatcher.Received(1).DispatchPendingBatches(Location(record, run), PendingBatchSelection.Requeued);
    }

    [Test]
    public async Task RequeueFailedBatches_LogsOnceForAllTheBatchesThatItRequeued()
    {
        // Telemetry is sampled. One log per batch would lose most of a burst, such as the failures of an outage.
        var (record, run) = GivenAnOpenRefreshWithARun();
        var requeued = fixture.CreateMany<SweptDonorGenotypePrecomputationBatch>(DonorGenotypePrecomputationSweeper.MaxDescribedBatchCount + 5).ToList();
        Repository(record).RequeueRetryableBatches(run.Id, settings.MaxBatchRetries).Returns(requeued);

        await sweeper.RequeueFailedBatches();

        var log = logger.Collector.GetSnapshot().Should().ContainSingle().Which;
        log.Level.Should().Be(LogLevel.Warning);
        log.GetStructuredStateValue("BatchCount").Should().Be(requeued.Count.ToString());
        log.GetStructuredStateValue("Batches").Should().EndWith("and 5 more");
    }

    [Test]
    public async Task RequeueFailedBatches_LogsOneErrorForAllTheBatchesThatItGaveUpOn()
    {
        var (record, run) = GivenAnOpenRefreshWithARun();
        var givenUp = fixture.CreateMany<SweptDonorGenotypePrecomputationBatch>().ToList();
        Repository(record).MarkBatchesPermanentlyFailed(run.Id, settings.MaxBatchRetries).Returns(givenUp);

        await sweeper.RequeueFailedBatches();

        var log = logger.Collector.GetSnapshot().Should().ContainSingle().Which;
        log.Level.Should().Be(LogLevel.Error);
        log.GetStructuredStateValue("BatchCount").Should().Be(givenUp.Count.ToString());
        log.GetStructuredStateValue("Batches").Should().ContainAll(givenUp.Select(batch => $"batch {batch.BatchId},"));
    }

    [Test]
    public async Task RequeueFailedBatches_TellsTheAttemptsOfEachBatch()
    {
        // The retry count of a swept batch is its count before the move. A batch with no retries has made one attempt,
        // and once requeued, it waits for its second.
        var (record, run) = GivenAnOpenRefreshWithARun();
        var retryCount = fixture.Create<int>();
        var givenUp = fixture.Build<SweptDonorGenotypePrecomputationBatch>().With(batch => batch.RetryCount, retryCount).Create();
        var requeued = fixture.Build<SweptDonorGenotypePrecomputationBatch>().With(batch => batch.RetryCount, retryCount).Create();
        Repository(record).MarkBatchesPermanentlyFailed(run.Id, settings.MaxBatchRetries).Returns([givenUp]);
        Repository(record).RequeueRetryableBatches(run.Id, settings.MaxBatchRetries).Returns([requeued]);

        await sweeper.RequeueFailedBatches();

        // The batches that it gave up on are logged as an error, and the requeued batches as a warning.
        var logs = logger.Collector.GetSnapshot();
        logs.Should().ContainSingle(log => log.Level == LogLevel.Error)
            .Which.GetStructuredStateValue("Batches").Should().Contain($"{retryCount + 1} attempt(s)");
        logs.Should().ContainSingle(log => log.Level == LogLevel.Warning)
            .Which.GetStructuredStateValue("Batches").Should().Contain($"next attempt {retryCount + 2}");
    }

    [Test]
    public async Task RequeueFailedBatches_WhenNothingFailed_LogsNothing()
    {
        GivenAnOpenRefreshWithARun();

        await sweeper.RequeueFailedBatches();

        logger.Collector.GetSnapshot().Should().BeEmpty();
    }

    [Test]
    public async Task RequeueFailedBatches_WithANegativeRetryLimit_ThrowsBeforeItReadsAnything()
    {
        // A sweep with a bad setting must not give up on batches.
        GivenAnOpenRefreshWithARun();
        settings.MaxBatchRetries = -fixture.Create<int>();

        var act = () => sweeper.RequeueFailedBatches();

        await act.Should().ThrowAsync<InvalidDataRefreshConfigurationException>();
        dataRefreshHistoryRepository.DidNotReceive().GetIncompleteRefreshJobs();
    }

    [TestCase(TransientDatabase.DatabaseA)]
    [TestCase(TransientDatabase.DatabaseB)]
    public async Task AbandonDeadLetteredBatch_AbandonsTheBatchThatTheBodyNames_InTheDatabaseOfItsRefresh(TransientDatabase database)
    {
        // The message names no database: the record has it.
        var (record, run) = GivenAnOpenRefreshWithARun(database);
        var request = RequestOf(record, run);

        await sweeper.AbandonDeadLetteredBatch(DeadLetter(JsonConvert.SerializeObject(request)));

        await repositories[database].Received(1)
            .TryMarkBatchAbandoned(request.DataRefreshRecordId, request.RunId, request.BatchId, Arg.Any<string>());
        await repositories[database.Other()].DidNotReceiveWithAnyArgs().TryMarkBatchAbandoned(default, default, default, default);
    }

    [Test]
    public async Task AbandonDeadLetteredBatch_RecordsTheDeadLetterReasonAndDescription()
    {
        // Support reads the reason on the batch row, and a retry that fails again replaces it.
        var (record, run) = GivenAnOpenRefreshWithARun();
        var request = RequestOf(record, run);
        var reason = fixture.Create<string>();
        var description = fixture.Create<string>();
        var properties = new Dictionary<string, object>
        {
            [DeadLetterReasonProperty] = reason,
            [DeadLetterErrorDescriptionProperty] = description
        };

        await sweeper.AbandonDeadLetteredBatch(DeadLetter(JsonConvert.SerializeObject(request), properties));

        await Repository(record).Received(1).TryMarkBatchAbandoned(
            request.DataRefreshRecordId,
            request.RunId,
            request.BatchId,
            Arg.Is<string>(recorded => recorded.Contains(reason) && recorded.Contains(description)));
    }

    [Test]
    public async Task AbandonDeadLetteredBatch_WhenTheRefreshOfTheMessageIsNotOpen_ReadsNoTransientDatabase()
    {
        // Only the runs of open refreshes are swept, so the batch of a refresh that has ended needs nothing.
        GivenAnOpenRefreshWithARun();
        var request = fixture.Create<DonorGenotypePrecomputationBatchRequest>();

        await sweeper.AbandonDeadLetteredBatch(DeadLetter(JsonConvert.SerializeObject(request)));

        repositoryFactory.DidNotReceiveWithAnyArgs().GetDonorGenotypePrecomputationRepositoryForDatabase(default);
    }

    [Test]
    public async Task AbandonDeadLetteredBatch_WhenTheBodyIsNotARequest_AbandonsNothingAndLogsAnError()
    {
        // Thrown, the message would come back forever. Its batch stays requested until someone abandons it by hand.
        var message = DeadLetter(fixture.Create<string>());

        var act = () => sweeper.AbandonDeadLetteredBatch(message);

        await act.Should().NotThrowAsync();
        repositoryFactory.DidNotReceiveWithAnyArgs().GetDonorGenotypePrecomputationRepositoryForDatabase(default);
        logger.Collector.GetSnapshot().Should().ContainSingle(log => log.Level == LogLevel.Error);
    }

    [Test]
    public async Task AbandonDeadLetteredBatch_WhenTheBodyHasNoIds_AbandonsNothing()
    {
        // An empty object reads as a batch with the ids 0, which no batch has.
        var message = DeadLetter("{}");

        await sweeper.AbandonDeadLetteredBatch(message);

        repositoryFactory.DidNotReceiveWithAnyArgs().GetDonorGenotypePrecomputationRepositoryForDatabase(default);
    }

    private Task RunSweep(string sweep) => sweep switch
    {
        Finalise => sweeper.FinaliseCompletedRuns(),
        Abandon => sweeper.MarkAbandonedBatches(),
        Requeue => sweeper.RequeueFailedBatches(),
        _ => throw new ArgumentOutOfRangeException(nameof(sweep), sweep, null)
    };

    private async Task ShouldHaveSwept(string sweep, IDonorGenotypePrecomputationRepository repository, DonorGenotypePrecomputationRun run)
    {
        switch (sweep)
        {
            case Finalise:
                await repository.Received(1).TryFinaliseRun(run.Id);
                break;
            case Abandon:
                await repository.Received(1).MarkExpiredBatchesAbandoned(run.Id);
                break;
            case Requeue:
                await repository.Received(1).RequeueRetryableBatches(run.Id, settings.MaxBatchRetries);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(sweep), sweep, null);
        }
    }

    private async Task ShouldHaveChangedNothing(IDonorGenotypePrecomputationRepository repository)
    {
        await repository.DidNotReceiveWithAnyArgs().TryFinaliseRun(default);
        await repository.DidNotReceiveWithAnyArgs().MarkExpiredBatchesAbandoned(default);
        await repository.DidNotReceiveWithAnyArgs().MarkBatchesPermanentlyFailed(default, default);
        await repository.DidNotReceiveWithAnyArgs().RequeueRetryableBatches(default, default);
        await dispatcher.DidNotReceiveWithAnyArgs().DispatchPendingBatches(default, default);
    }

    /// <summary>An open refresh that has recreated its indexes, and its run.</summary>
    private (DataRefreshRecord Record, DonorGenotypePrecomputationRun Run) GivenAnOpenRefreshWithARun(
        TransientDatabase database = TransientDatabase.DatabaseA,
        RunStatus status = RunStatus.Running)
    {
        var record = DataRefreshRecordBuilder.New
            .With(r => r.Id, fixture.Create<int>())
            .WithDatabase(database)
            .WithStagesCompletedUpToAndIncluding(DataRefreshStage.IndexRecreation)
            .Build();
        var run = fixture.Build<DonorGenotypePrecomputationRun>()
            .With(r => r.DataRefreshRecordId, record.Id)
            .With(r => r.Status, status)
            .Create();

        repositories[database].GetRun(record.Id).Returns(run);
        openRecords.Add(record);

        return (record, run);
    }

    private IDonorGenotypePrecomputationRepository Repository(DataRefreshRecord record) =>
        repositories[record.Database.ParseToEnum<TransientDatabase>()];

    private static DonorGenotypePrecomputationRunLocation Location(DataRefreshRecord record, DonorGenotypePrecomputationRun run) =>
        new(record.Id, record.Database.ParseToEnum<TransientDatabase>(), run.Id);

    /// <summary>The request of a batch of the run of the refresh.</summary>
    private DonorGenotypePrecomputationBatchRequest RequestOf(DataRefreshRecord record, DonorGenotypePrecomputationRun run) =>
        fixture.Build<DonorGenotypePrecomputationBatchRequest>()
            .With(request => request.DataRefreshRecordId, record.Id)
            .With(request => request.RunId, run.Id)
            .Create();

    private static ServiceBusReceivedMessage DeadLetter(string body, IDictionary<string, object> properties = null) =>
        ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(body),
            messageId: Guid.NewGuid().ToString(),
            properties: properties);
}
