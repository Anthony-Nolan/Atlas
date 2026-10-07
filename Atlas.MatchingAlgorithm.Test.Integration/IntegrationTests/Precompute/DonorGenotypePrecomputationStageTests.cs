using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.ServiceBus;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Repositories;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using BatchStatus = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationBatchStatus;
using ContextFactory = Atlas.MatchingAlgorithm.Data.Context.ContextFactory;
using Injection = Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection.DependencyInjection;
using RunStatus = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationRunStatus;
using SearchAlgorithmContext = Atlas.MatchingAlgorithm.Data.Context.SearchAlgorithmContext;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Precompute;

/// <summary>
/// The donor genotype precomputation stage end to end against a real database: the stage builds the run and sends its
/// batches, the batch processor of the workers computes them, the sweeper sends failed batches again and finalises the
/// run, and the stage reports the run. The imputation, the frequency set lookup and the message bus are substituted.
/// </summary>
[TestFixture]
public class DonorGenotypePrecomputationStageTests
{
    /// <summary>
    /// The set-up of the suite makes a successful refresh of A, so A is active and B is dormant. The refresh of a test
    /// fills B.
    /// </summary>
    private const TransientDatabase DormantDatabase = TransientDatabase.DatabaseB;

    /// <summary>Few groups per batch, so a test has several batches.</summary>
    private const int GroupsPerBatch = 4;

    /// <summary>The stage reads the run this often, so it sees a completed run at most a second later.</summary>
    private const int PollIntervalSeconds = 1;

    /// <summary>The longest wait for the stage or for its messages: far longer than the work of a test.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(1);

    private Fixture fixture;
    private IServiceScope scope;
    private ITestDataRefreshHistoryRepository dataRefreshHistoryRepository;
    private StaticallyChosenTransientSqlConnectionStringProviderFactory connectionStringFactory;
    private IDonorGenotypePrecomputationRepository repository;
    private IGenotypeSetService genotypeSetService;
    private IDataRefreshSupportNotificationSender notificationSender;
    private ConcurrentQueue<DonorGenotypePrecomputationBatchRequest> publishedRequests;
    private DataRefreshRecord record;
    private int nextDonorId;

    private DonorGenotypePrecomputationStage stage;
    private DonorGenotypePrecomputationSweeper sweeper;
    private DonorGenotypePrecomputationBatchProcessor processor;

    [SetUp]
    public async Task SetUp()
    {
        fixture = new Fixture();
        nextDonorId = 1;
        DatabaseManager.ClearTransientDatabases();

        // A scope of its own, so that the active database is read after the records of other tests are gone.
        scope = Injection.BackingProvider.CreateScope();
        var services = scope.ServiceProvider;
        dataRefreshHistoryRepository = services.GetRequiredService<ITestDataRefreshHistoryRepository>();
        connectionStringFactory = services.GetRequiredService<StaticallyChosenTransientSqlConnectionStringProviderFactory>();
        var repositoryFactory = services.GetRequiredService<IStaticallyChosenDatabaseRepositoryFactory>();
        repository = repositoryFactory.GetDonorGenotypePrecomputationRepositoryForDatabase(DormantDatabase);

        // An open refresh of the dormant database that has recreated its indexes: the record that the stage and the sweeps act on.
        record = DataRefreshRecordBuilder.New
            .WithDatabase(DormantDatabase)
            .WithStagesCompletedUpToAndIncluding(DataRefreshStage.IndexRecreation)
            .Build();
        await dataRefreshHistoryRepository.Create(record);

        var settings = fixture.Build<DonorGenotypePrecomputationSettings>()
            .With(s => s.GroupsPerBatch, GroupsPerBatch)
            .With(s => s.PollIntervalSeconds, PollIntervalSeconds)
            .With(s => s.MaxFailedDonorFraction, 1.0 / fixture.Create<int>())
            .Create();

        publishedRequests = new ConcurrentQueue<DonorGenotypePrecomputationBatchRequest>();
        var publisher = Substitute.For<IMessageBatchPublisher<DonorGenotypePrecomputationBatchRequest>>();
        publisher.BatchPublish(default).ReturnsForAnyArgs(callInfo =>
        {
            foreach (var request in callInfo.Arg<IEnumerable<DonorGenotypePrecomputationBatchRequest>>())
            {
                publishedRequests.Enqueue(request);
            }

            return Task.CompletedTask;
        });
        var dispatcher = new DonorGenotypePrecomputationBatchDispatcher(
            repositoryFactory,
            publisher,
            NullLogger<DonorGenotypePrecomputationBatchDispatcher>.Instance);

        notificationSender = Substitute.For<IDataRefreshSupportNotificationSender>();
        stage = new DonorGenotypePrecomputationStage(
            services.GetRequiredService<IDormantRepositoryFactory>(),
            services.GetRequiredService<IActiveDatabaseProvider>(),
            dispatcher,
            notificationSender,
            settings,
            TimeProvider.System,
            NullLogger<DonorGenotypePrecomputationStage>.Instance);

        sweeper = new DonorGenotypePrecomputationSweeper(
            services.GetRequiredService<IDataRefreshHistoryRepository>(),
            repositoryFactory,
            dispatcher,
            settings,
            NullLogger<DonorGenotypePrecomputationSweeper>.Instance);

        genotypeSetService = Substitute.For<IGenotypeSetService>();
        genotypeSetService.GetGenotypeSet(default, default).ReturnsForAnyArgs(_ => new SubjectGenotypeSet(true, [], 0m));
        var frequencySetLookup = Substitute.For<IHaplotypeFrequencyLookupService>();
        frequencySetLookup.GetSingleHaplotypeFrequencySet(default).ReturnsForAnyArgs(new HaplotypeFrequencySet { Id = fixture.Create<int>() });

        processor = new DonorGenotypePrecomputationBatchProcessor(
            repositoryFactory,
            new DonorGenotypePrecomputationTarget(record.Id, DormantDatabase),
            new SubjectGenotypeSetValueService(repositoryFactory, genotypeSetService),
            frequencySetLookup,
            fixture.Build<DonorGenotypePrecomputationWorkerSettings>().With(s => s.MaxGroupFailuresPerBatch, GroupsPerBatch).Create(),
            Substitute.For<IDonorGenotypePrecomputationMetrics>(),
            NullLogger<DonorGenotypePrecomputationBatchProcessor>.Instance);
    }

    [TearDown]
    public async Task TearDown()
    {
        await dataRefreshHistoryRepository.RemoveAllDataRefreshRecords();
        IntegrationTestSetUp.RunInitialDataRefresh();
        scope.Dispose();
    }

    [Test]
    public async Task Run_WithABatchThatIsSentAgain_GivesEachDonorOneRowPerKey_AndCompletesTheRun()
    {
        var donors = DonorsOfTwoPairs();
        await InsertDonors(donors);
        // The first imputation of one typing fails with an error of unknown cause. Its batch writes the donor rows of its
        // other groups, fails, and is sent again: the retry writes those rows a second time.
        GivenTheImputationOf(donors[0]).Returns(
            _ => throw new InvalidOperationException(fixture.Create<string>()),
            _ => new SubjectGenotypeSet(true, [], 0m));

        var stageRun = stage.Run(record, DataRefreshStageExecutionMode.FromScratch, CancellationToken.None);
        await WaitForPublishedRequests(stageRun);
        await ProcessThePublishedRequests();
        await sweeper.RequeueFailedBatches();
        var resentRequests = await ProcessThePublishedRequests();
        await sweeper.FinaliseCompletedRuns();
        await stageRun.WaitAsync(Timeout);

        resentRequests.Should().ContainSingle();
        (await StoredKeysOfEachDonor()).Should().BeEquivalentTo(EveryKeyOfEach(donors));
        var run = await repository.GetRun(record.Id);
        run.Status.Should().Be(RunStatus.Completed);
        (await repository.GetBatchCounts(run.Id)).CountOf(BatchStatus.ResultsReceived).Should().Be(run.TotalBatchCount);
        (await repository.HasStagingData()).Should().BeFalse();
        await notificationSender.DidNotReceiveWithAnyArgs().SendPrecomputationFailureSummary(default, default, default, default);
    }

    [Test]
    public async Task Run_WithAMessageThatIsDeliveredAgainWhileItsFirstCopyRuns_GivesEachDonorOneRowPerKey_AndCompletesTheRun()
    {
        var donors = DonorsOfTwoPairs();
        await InsertDonors(donors);
        // The first imputation of one typing waits until the test lets it go. So the first copy of its message holds its batch,
        // as a worker does when it loses the lock of the message.
        var imputationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var imputationReleased = new TaskCompletionSource<SubjectGenotypeSet>(TaskCreationOptions.RunContinuationsAsynchronously);
        GivenTheImputationOf(donors[0]).Returns(
            _ =>
            {
                imputationStarted.TrySetResult();
                return imputationReleased.Task;
            },
            _ => Task.FromResult(new SubjectGenotypeSet(true, [], 0m)));

        var stageRun = stage.Run(record, DataRefreshStageExecutionMode.FromScratch, CancellationToken.None);
        await WaitForPublishedRequests(stageRun);
        var (heldRequest, firstCopy) = await ProcessThePublishedRequestsAndHoldTheOneThatReaches(imputationStarted.Task);
        // Service Bus delivers the held message again. The second copy takes the batch from the first, and completes it.
        var secondCopyResult = await processor.ProcessBatch(heldRequest, true);
        imputationReleased.SetResult(new SubjectGenotypeSet(true, [], 0m));
        var firstCopyResult = await firstCopy.WaitAsync(Timeout);
        await sweeper.FinaliseCompletedRuns();
        await stageRun.WaitAsync(Timeout);

        secondCopyResult.Should().Be(DonorGenotypePrecomputationBatchResult.ResultsReceived);
        // The first copy writes the donor rows of the batch again before it finds that it lost its lease.
        firstCopyResult.Should().Be(DonorGenotypePrecomputationBatchResult.LeaseLost);
        (await StoredKeysOfEachDonor()).Should().BeEquivalentTo(EveryKeyOfEach(donors));
        var run = await repository.GetRun(record.Id);
        run.Status.Should().Be(RunStatus.Completed);
        (await repository.HasStagingData()).Should().BeFalse();
    }

    [Test]
    public async Task Run_AfterAManualRetry_SendsOnlyTheResetBatches_DoesNoNewBuild_AndGivesTheFailedDonorsTheirRows()
    {
        var donors = DonorsOfTwoPairs();
        await InsertDonors(donors);
        // A known permanent error fails the groups of one typing, not their batches, until the cause is fixed.
        GivenTheImputationOf(donors[0])
            .ThrowsAsync(new HlaMetadataDictionaryException(fixture.Create<string>(), fixture.Create<string>(), fixture.Create<string>()));

        // The first attempt stops before the run is finalised. The run then completes with failures and keeps its staging data,
        // as a pause for a manual decision leaves it.
        using var firstAttempt = new CancellationTokenSource();
        var firstStageRun = stage.Run(record, DataRefreshStageExecutionMode.FromScratch, firstAttempt.Token);
        await WaitForPublishedRequests(firstStageRun);
        await ProcessThePublishedRequests();
        await firstAttempt.CancelAsync();
        var stopTheFirstAttempt = () => firstStageRun;
        await stopTheFirstAttempt.Should().ThrowAsync<OperationCanceledException>();
        await sweeper.FinaliseCompletedRuns();
        var failedRun = await repository.GetRun(record.Id);
        failedRun.Status.Should().Be(RunStatus.CompletedWithFailures);
        var resetBatchCount = await repository.ResetFailedBatchesForManualRetry(failedRun.Id);
        GivenTheImputationOf(donors[0]).Returns(new SubjectGenotypeSet(true, [], 0m));

        var secondStageRun = stage.Run(record, DataRefreshStageExecutionMode.Continuation, CancellationToken.None);
        await WaitForPublishedRequests(secondStageRun);
        var resentRequests = await ProcessThePublishedRequests();
        await sweeper.FinaliseCompletedRuns();
        await secondStageRun.WaitAsync(Timeout);

        resentRequests.Should().HaveCount(resetBatchCount);
        var run = await repository.GetRun(record.Id);
        run.Should().BeEquivalentTo(new { failedRun.Id, failedRun.CreatedUtc, Status = RunStatus.Completed, ManualRetryCount = 1 });
        (await StoredKeysOfEachDonor()).Should().BeEquivalentTo(EveryKeyOfEach(donors));
        (await repository.HasStagingData()).Should().BeFalse();
    }

    [Test]
    public async Task Run_WhenTheStageRunsAgainAfterItCompleted_ChangesNothing()
    {
        var donors = DonorsOfTwoPairs();
        await InsertDonors(donors);
        var firstStageRun = stage.Run(record, DataRefreshStageExecutionMode.FromScratch, CancellationToken.None);
        await WaitForPublishedRequests(firstStageRun);
        await ProcessThePublishedRequests();
        await sweeper.FinaliseCompletedRuns();
        await firstStageRun.WaitAsync(Timeout);
        var completedRun = await repository.GetRun(record.Id);

        await stage.Run(record, DataRefreshStageExecutionMode.Continuation, CancellationToken.None).WaitAsync(Timeout);

        publishedRequests.Should().BeEmpty();
        (await repository.GetRun(record.Id)).Should().BeEquivalentTo(completedRun);
        (await StoredKeysOfEachDonor()).Should().BeEquivalentTo(EveryKeyOfEach(donors));
    }

    /// <summary>
    /// Two registry and ethnicity pairs, three typings in each, and a copy of the first donor, so one group of each key has
    /// two donors.
    /// </summary>
    private List<Donor> DonorsOfTwoPairs()
    {
        var donors = new List<Donor>();
        foreach (var _ in Enumerable.Range(0, 2))
        {
            var registryCode = fixture.Create<string>();
            var ethnicityCode = fixture.Create<string>();
            donors.AddRange(Enumerable.Range(0, 3).Select(_ => NewDonor(registryCode, ethnicityCode)));
        }

        donors.Add(CopyOf(donors[0]));
        return donors;
    }

    private Donor NewDonor(string registryCode, string ethnicityCode) => new()
    {
        DonorId = nextDonorId++,
        DonorType = fixture.Create<DonorType>(),
        IsAvailableForSearch = true,
        ExternalDonorCode = fixture.Create<string>(),
        RegistryCode = registryCode,
        EthnicityCode = ethnicityCode,
        A_1 = fixture.Create<string>(),
        A_2 = fixture.Create<string>(),
        B_1 = fixture.Create<string>(),
        B_2 = fixture.Create<string>(),
        C_1 = fixture.Create<string>(),
        C_2 = fixture.Create<string>(),
        DPB1_1 = fixture.Create<string>(),
        DPB1_2 = fixture.Create<string>(),
        DQB1_1 = fixture.Create<string>(),
        DQB1_2 = fixture.Create<string>(),
        DRB1_1 = fixture.Create<string>(),
        DRB1_2 = fixture.Create<string>()
    };

    private Donor CopyOf(Donor donor)
    {
        var copy = NewDonor(donor.RegistryCode, donor.EthnicityCode);
        copy.A_1 = donor.A_1;
        copy.A_2 = donor.A_2;
        copy.B_1 = donor.B_1;
        copy.B_2 = donor.B_2;
        copy.C_1 = donor.C_1;
        copy.C_2 = donor.C_2;
        copy.DPB1_1 = donor.DPB1_1;
        copy.DPB1_2 = donor.DPB1_2;
        copy.DQB1_1 = donor.DQB1_1;
        copy.DQB1_2 = donor.DQB1_2;
        copy.DRB1_1 = donor.DRB1_1;
        copy.DRB1_2 = donor.DRB1_2;
        return copy;
    }

    private Task<SubjectGenotypeSet> GivenTheImputationOf(Donor donor)
    {
        var typing = donor.ToDonorInfo().HlaNames;
        return genotypeSetService.GetGenotypeSet(
            Arg.Is<SubjectData>(subject => subject.HlaTyping.Equals(typing)),
            Arg.Any<MatchPredictionParameters>());
    }

    /// <summary>Waits until the stage has sent its batches. Throws the error of the stage if it stopped first.</summary>
    private async Task WaitForPublishedRequests(Task stageRun)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (publishedRequests.IsEmpty)
        {
            if (stageRun.IsCompleted)
            {
                await stageRun;
                throw new InvalidOperationException("The stage ended before it sent any batch.");
            }

            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("The stage sent no batch in time.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }
    }

    /// <summary>What the workers do with the messages: processes each batch message that was sent, once.</summary>
    private async Task<List<DonorGenotypePrecomputationBatchRequest>> ProcessThePublishedRequests()
    {
        var processedRequests = new List<DonorGenotypePrecomputationBatchRequest>();
        while (publishedRequests.TryDequeue(out var request))
        {
            await processor.ProcessBatch(request, false);
            processedRequests.Add(request);
        }

        return processedRequests;
    }

    /// <summary>
    /// Processes each batch message that was sent, once, as <see cref="ProcessThePublishedRequests"/> does. The processing of
    /// the first message that reaches <paramref name="hold"/> is not awaited: it is returned, with its message.
    /// </summary>
    private async Task<(DonorGenotypePrecomputationBatchRequest Request, Task<DonorGenotypePrecomputationBatchResult> Processing)>
        ProcessThePublishedRequestsAndHoldTheOneThatReaches(Task hold)
    {
        (DonorGenotypePrecomputationBatchRequest Request, Task<DonorGenotypePrecomputationBatchResult> Processing)? held = null;
        while (publishedRequests.TryDequeue(out var request))
        {
            var processing = processor.ProcessBatch(request, false);
            if (held == null && await Task.WhenAny(processing, hold) != processing)
            {
                held = (request, processing);
                continue;
            }

            await processing;
        }

        return held ?? throw new InvalidOperationException("No batch message reached the hold.");
    }

    private static Dictionary<int, List<AllowedLociKey>> EveryKeyOfEach(IEnumerable<Donor> donors) =>
        donors.ToDictionary(donor => donor.DonorId, _ => AllowedLociKeyExtensions.All.Order().ToList());

    /// <summary>The keys of the donor rows of each donor.</summary>
    private async Task<Dictionary<int, List<AllowedLociKey>>> StoredKeysOfEachDonor()
    {
        await using var context = NewContext();
        var rows = await context.DonorSubjectGenotypeSets.AsNoTracking().ToListAsync();
        return rows
            .GroupBy(row => row.DonorId)
            .ToDictionary(donorRows => donorRows.Key, donorRows => donorRows.Select(row => row.AllowedLociKey).Order().ToList());
    }

    private async Task InsertDonors(IEnumerable<Donor> donors)
    {
        await using var context = NewContext();
        context.Donors.AddRange(donors);
        await context.SaveChangesAsync();
    }

    private SearchAlgorithmContext NewContext() =>
        new ContextFactory().Create(connectionStringFactory.GenerateConnectionStringProvider(DormantDatabase).GetConnectionString());
}
