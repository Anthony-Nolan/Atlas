using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.Common.Public.Models.MatchPrediction;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.Extensions;
using NUnit.Framework;
using BatchStatus = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationBatchStatus;
using ContextFactory = Atlas.MatchingAlgorithm.Data.Context.ContextFactory;
using Injection = Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection.DependencyInjection;
using RunStatus = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationRunStatus;
using SearchAlgorithmContext = Atlas.MatchingAlgorithm.Data.Context.SearchAlgorithmContext;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Precompute;

/// <summary>
/// The batch processor of the precomputation workers against a real database: what one batch message writes, and that a
/// batch that runs again writes nothing twice. The imputation and the frequency set lookup are substituted.
/// </summary>
[TestFixture]
public class DonorGenotypePrecomputationBatchProcessorTests
{
    private const TransientDatabase Database = TransientDatabase.DatabaseA;
    private const int MaxBatchRetries = 3;

    private Fixture fixture;
    private StaticallyChosenTransientSqlConnectionStringProviderFactory connectionStringFactory;
    private IDonorGenotypePrecomputationRepository repository;
    private IGenotypeSetService genotypeSetService;
    private int nextGroupId;
    private int nextBatchNumber;

    private IDonorGenotypePrecomputationBatchProcessor processor;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        nextGroupId = 1;
        nextBatchNumber = 0;

        connectionStringFactory = Injection.Provider.GetService<StaticallyChosenTransientSqlConnectionStringProviderFactory>();
        var repositoryFactory = Injection.Provider.GetService<IStaticallyChosenDatabaseRepositoryFactory>();
        repository = repositoryFactory.GetDonorGenotypePrecomputationRepositoryForDatabase(Database);

        genotypeSetService = Substitute.For<IGenotypeSetService>();
        genotypeSetService.GetGenotypeSet(default, default).ReturnsForAnyArgs(_ => new SubjectGenotypeSet(true, [], 0m));

        var frequencySetLookup = Substitute.For<IHaplotypeFrequencyLookupService>();
        frequencySetLookup.GetSingleHaplotypeFrequencySet(default).ReturnsForAnyArgs(new HaplotypeFrequencySet { Id = fixture.Create<int>() });

        processor = new DonorGenotypePrecomputationBatchProcessor(
            repositoryFactory,
            new SubjectGenotypeSetValueService(repositoryFactory, genotypeSetService),
            frequencySetLookup,
            fixture.Build<DonorGenotypePrecomputationWorkerSettings>().With(settings => settings.MaxGroupFailuresPerBatch, 10).Create(),
            Substitute.For<IDonorGenotypePrecomputationMetrics>(),
            NullLogger<DonorGenotypePrecomputationBatchProcessor>.Instance);

        DatabaseManager.ClearTransientDatabases();
    }

    [Test]
    public async Task ProcessBatch_StoresAValuePerGroup_WritesARowPerDonor_AndCompletesTheBatch()
    {
        var run = await InsertRun();
        var sharingDonors = new[] { await InsertDonor(), await InsertDonor() };
        var otherDonor = await InsertDonor();
        var sharedGroup = await InsertGroup(run.Id, sharingDonors);
        var otherGroup = await InsertGroup(run.Id, otherDonor);
        var batch = await InsertBatch(run.Id, sharedGroup.Id, otherGroup.Id);

        var result = await processor.ProcessBatch(RequestFor(run, batch), Database, false);

        result.Should().Be(DonorGenotypePrecomputationBatchResult.ResultsReceived);
        var groups = await StoredGroups();
        groups.Values.Should().OnlyContain(group => group.SubjectGenotypeSetValueId != null && group.FailureMessage == null);
        (await StoredDonorRows()).Should().BeEquivalentTo(
            sharingDonors.Select(donor => DonorRow(donor, groups[sharedGroup.Id]))
                .Append(DonorRow(otherDonor, groups[otherGroup.Id])));
        var storedBatch = await StoredBatch(batch.Id);
        storedBatch.Status.Should().Be(BatchStatus.ResultsReceived);
        storedBatch.FailedGroupCount.Should().Be(0);
    }

    [Test]
    public async Task ProcessBatch_ForASecondCopyOfTheMessage_SkipsTheBatch_AndWritesNothingTwice()
    {
        var run = await InsertRun();
        var group = await InsertGroup(run.Id, await InsertDonor(), await InsertDonor());
        var batch = await InsertBatch(run.Id, group.Id, group.Id);
        var request = RequestFor(run, batch);
        await processor.ProcessBatch(request, Database, false);

        var result = await processor.ProcessBatch(request, Database, false);

        result.Should().Be(DonorGenotypePrecomputationBatchResult.Skipped);
        await genotypeSetService.Received(1).GetGenotypeSet(Arg.Any<SubjectData>(), Arg.Any<MatchPredictionParameters>());
        (await StoredValueCount()).Should().Be(1);
        (await StoredDonorRows()).Should().HaveCount(2);
    }

    [Test]
    public async Task ProcessBatch_ForAMessageOfAnotherRefresh_SkipsTheBatch_AndLeavesIt()
    {
        var run = await InsertRun();
        var group = await InsertGroup(run.Id, await InsertDonor());
        var batch = await InsertBatch(run.Id, group.Id, group.Id);
        var request = RequestFor(run, batch);
        request.DataRefreshRecordId = fixture.Create<int>();

        var result = await processor.ProcessBatch(request, Database, false);

        result.Should().Be(DonorGenotypePrecomputationBatchResult.Skipped);
        (await StoredBatch(batch.Id)).Status.Should().Be(BatchStatus.Pending);
        (await StoredValueCount()).Should().Be(0);
    }

    [Test]
    public async Task ProcessBatch_ForAGroupWithABadTyping_FailsOnlyThatGroup_AndCompletesTheBatch()
    {
        var run = await InsertRun();
        var goodDonor = await InsertDonor();
        var badDonor = await InsertDonor();
        var goodGroup = await InsertGroup(run.Id, goodDonor);
        var badGroup = await InsertGroup(run.Id, badDonor);
        var batch = await InsertBatch(run.Id, goodGroup.Id, badGroup.Id);
        var failure = new HlaMetadataDictionaryException(fixture.Create<string>(), fixture.Create<string>(), fixture.Create<string>());
        GivenTheImputationOf(badDonor).ThrowsAsync(failure);

        var result = await processor.ProcessBatch(RequestFor(run, batch), Database, false);

        result.Should().Be(DonorGenotypePrecomputationBatchResult.ResultsReceived);
        var groups = await StoredGroups();
        groups[badGroup.Id].SubjectGenotypeSetValueId.Should().BeNull();
        groups[badGroup.Id].FailureMessage.Should().Be(failure.Message);
        (await StoredDonorRows()).Select(row => row.DonorId).Should().Equal(goodDonor.DonorId);
        (await StoredBatch(batch.Id)).FailedGroupCount.Should().Be(1);
    }

    [Test]
    public async Task ProcessBatch_AfterAnErrorOfUnknownCause_FailsTheBatch_AndTheRetryComputesOnlyTheGroupWithNoValue()
    {
        var run = await InsertRun();
        var firstDonor = await InsertDonor();
        var secondDonor = await InsertDonor();
        var firstGroup = await InsertGroup(run.Id, firstDonor);
        var secondGroup = await InsertGroup(run.Id, secondDonor);
        var batch = await InsertBatch(run.Id, firstGroup.Id, secondGroup.Id);
        GivenTheImputationOf(secondDonor).ThrowsAsync(new InvalidOperationException(fixture.Create<string>()));

        var firstResult = await processor.ProcessBatch(RequestFor(run, batch), Database, false);

        firstResult.Should().Be(DonorGenotypePrecomputationBatchResult.Failed);
        var failedBatch = await StoredBatch(batch.Id);
        failedBatch.Status.Should().Be(BatchStatus.Failed);
        (await StoredDonorRows()).Select(row => row.DonorId).Should().Equal(firstDonor.DonorId);

        await repository.RequeueRetryableBatches(run.Id, MaxBatchRetries);
        // Configure(): without it, the set-up call would run the throw of the first set-up.
        var secondTyping = secondDonor.ToDonorInfo().HlaNames;
        genotypeSetService.Configure()
            .GetGenotypeSet(Arg.Is<SubjectData>(subject => subject.HlaTyping.Equals(secondTyping)), Arg.Any<MatchPredictionParameters>())
            .Returns(new SubjectGenotypeSet(true, [], 0m));

        var retryResult = await processor.ProcessBatch(RequestFor(run, batch), Database, false);

        retryResult.Should().Be(DonorGenotypePrecomputationBatchResult.ResultsReceived);
        ImputationCountOf(firstDonor).Should().Be(1);
        ImputationCountOf(secondDonor).Should().Be(2);
        (await StoredDonorRows()).Select(row => row.DonorId).Should().BeEquivalentTo([firstDonor.DonorId, secondDonor.DonorId]);
    }

    private int ImputationCountOf(Donor donor)
    {
        var typing = donor.ToDonorInfo().HlaNames;
        return genotypeSetService.ReceivedCalls().Count(call => ((SubjectData)call.GetArguments()[0]).HlaTyping.Equals(typing));
    }

    /// <summary>The imputation of the typing of the donor: the typing that the group reads from <c>Donors</c>.</summary>
    private Task<SubjectGenotypeSet> GivenTheImputationOf(Donor donor)
    {
        var typing = donor.ToDonorInfo().HlaNames;
        return genotypeSetService.GetGenotypeSet(
            Arg.Is<SubjectData>(subject => subject.HlaTyping.Equals(typing)),
            Arg.Any<MatchPredictionParameters>());
    }

    /// <summary>The donor row that a donor of the group has once the group has its value.</summary>
    private static object DonorRow(Donor donor, DonorGenotypePrecomputationGroup group) => new
    {
        donor.DonorId,
        group.AllowedLociKey,
        group.SubjectGenotypeSetValueId
    };

    private static DonorGenotypePrecomputationBatchRequest RequestFor(DonorGenotypePrecomputationRun run, DonorGenotypePrecomputationBatch batch) => new()
    {
        DataRefreshRecordId = run.DataRefreshRecordId,
        RunId = run.Id,
        BatchId = batch.Id
    };

    private async Task<DonorGenotypePrecomputationRun> InsertRun()
    {
        var run = new DonorGenotypePrecomputationRun
        {
            DataRefreshRecordId = fixture.Create<int>(),
            // The column holds 32 characters; a whole AutoFixture string is 36.
            HlaNomenclatureVersion = fixture.Create<string>()[..8],
            Status = RunStatus.Running,
            GroupsPerBatch = fixture.Create<int>(),
            CreatedUtc = DateTime.UtcNow,
            StatusDateUtc = DateTime.UtcNow
        };

        await Insert(run);
        return run;
    }

    /// <summary>A group of the donors, as the build makes it: the lowest donor id is the representative.</summary>
    private async Task<DonorGenotypePrecomputationGroup> InsertGroup(int runId, params Donor[] donors)
    {
        var group = new DonorGenotypePrecomputationGroup
        {
            Id = nextGroupId++,
            RunId = runId,
            AllowedLociKey = fixture.Create<AllowedLociKey>(),
            RepresentativeDonorId = donors.Min(donor => donor.DonorId),
            DonorCount = donors.Length
        };

        await using var context = NewContext();
        context.DonorGenotypePrecomputationGroups.Add(group);
        context.DonorGenotypePrecomputationGroupDonors.AddRange(
            donors.Select(donor => new DonorGenotypePrecomputationGroupDonor { GroupId = group.Id, DonorId = donor.DonorId }));
        await context.SaveChangesAsync();

        return group;
    }

    private async Task<DonorGenotypePrecomputationBatch> InsertBatch(int runId, int firstGroupId, int lastGroupId)
    {
        var batch = new DonorGenotypePrecomputationBatch
        {
            RunId = runId,
            BatchNumber = nextBatchNumber++,
            FirstGroupId = firstGroupId,
            LastGroupId = lastGroupId,
            GroupCount = lastGroupId - firstGroupId + 1,
            DonorAssignmentCount = fixture.Create<int>(),
            Status = BatchStatus.Pending,
            StatusDateUtc = DateTime.UtcNow
        };

        await Insert(batch);
        return batch;
    }

    private async Task<Donor> InsertDonor()
    {
        var donor = new Donor
        {
            DonorId = fixture.Create<int>(),
            DonorType = fixture.Create<DonorType>(),
            IsAvailableForSearch = true,
            ExternalDonorCode = fixture.Create<string>(),
            RegistryCode = fixture.Create<string>(),
            EthnicityCode = fixture.Create<string>(),
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

        await Insert(donor);
        return donor;
    }

    private async Task<DonorGenotypePrecomputationBatch> StoredBatch(int batchId)
    {
        await using var context = NewContext();
        return await context.DonorGenotypePrecomputationBatches.AsNoTracking().SingleAsync(batch => batch.Id == batchId);
    }

    private async Task<Dictionary<int, DonorGenotypePrecomputationGroup>> StoredGroups()
    {
        await using var context = NewContext();
        return await context.DonorGenotypePrecomputationGroups.AsNoTracking().ToDictionaryAsync(group => group.Id);
    }

    private async Task<List<DonorSubjectGenotypeSet>> StoredDonorRows()
    {
        await using var context = NewContext();
        return await context.DonorSubjectGenotypeSets.AsNoTracking().ToListAsync();
    }

    private async Task<int> StoredValueCount()
    {
        await using var context = NewContext();
        return await context.SubjectGenotypeSetValues.CountAsync();
    }

    private async Task Insert<T>(T entity) where T : class
    {
        await using var context = NewContext();
        context.Set<T>().Add(entity);
        await context.SaveChangesAsync();
    }

    private SearchAlgorithmContext NewContext() =>
        new ContextFactory().Create(connectionStringFactory.GenerateConnectionStringProvider(Database).GetConnectionString());
}
