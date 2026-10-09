using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using BatchResult = Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute.DonorGenotypePrecomputationBatchResult;
using Outcome = Atlas.MatchingAlgorithm.Data.Models.Precompute.DonorGenotypePrecomputationGroupOutcome;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh.Precompute;

[TestFixture]
public class DonorGenotypePrecomputationBatchProcessorTests
{
    private Fixture fixture;
    private TransientDatabase database;
    private DonorGenotypePrecomputationBatchRequest request;
    private ClaimedDonorGenotypePrecomputationBatch claimedBatch;
    private DonorGenotypePrecomputationWorkerSettings settings;
    private int nextValueId;

    private IDonorGenotypePrecomputationRepository repository;
    private ISubjectGenotypeSetRepository subjectGenotypeSetRepository;
    private IStaticallyChosenDatabaseRepositoryFactory repositoryFactory;
    private ISubjectGenotypeSetValueService valueService;
    private IHaplotypeFrequencyLookupService frequencySetLookup;
    private IDonorGenotypePrecomputationMetrics metrics;

    /// <summary>The frequency set that the lookup gives each registry and ethnicity pair.</summary>
    private Dictionary<(string RegistryCode, string EthnicityCode), HaplotypeFrequencySet> frequencySets;

    /// <summary>The value service stores every value, unless a test gives the typing of a group another outcome.</summary>
    private Dictionary<PhenotypeInfo<string>, SubjectGenotypeSetValueOutcome> outcomesByTyping;

    private IDonorGenotypePrecomputationBatchProcessor processor;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        database = fixture.Create<TransientDatabase>();
        request = fixture.Create<DonorGenotypePrecomputationBatchRequest>();
        claimedBatch = fixture.Build<ClaimedDonorGenotypePrecomputationBatch>()
            .With(batch => batch.BatchId, request.BatchId)
            .With(batch => batch.RunId, request.RunId)
            .Create();
        settings = fixture.Create<DonorGenotypePrecomputationWorkerSettings>();
        nextValueId = fixture.Create<int>();
        frequencySets = [];
        outcomesByTyping = [];

        repository = Substitute.For<IDonorGenotypePrecomputationRepository>();
        repository.TryClaimBatch(default).ReturnsForAnyArgs(claimedBatch);
        repository.GetGroupsToCompute(default, default, default).ReturnsForAnyArgs(new List<DonorGenotypePrecomputationGroupToCompute>());
        repository.GetDonorAssignments(default, default, default).ReturnsForAnyArgs(new List<DonorSubjectGenotypeSetAssignment>());
        repository.TryMarkBatchResultsReceived(default, default, default).ReturnsForAnyArgs(true);
        repository.TryMarkBatchFailed(default, default, default).ReturnsForAnyArgs(true);

        subjectGenotypeSetRepository = Substitute.For<ISubjectGenotypeSetRepository>();

        repositoryFactory = Substitute.For<IStaticallyChosenDatabaseRepositoryFactory>();
        repositoryFactory.GetDonorGenotypePrecomputationRepositoryForDatabase(database).Returns(repository);
        repositoryFactory.GetSubjectGenotypeSetRepositoryForDatabase(database).Returns(subjectGenotypeSetRepository);

        valueService = Substitute.For<ISubjectGenotypeSetValueService>();
        EveryValueIsStored();

        frequencySetLookup = Substitute.For<IHaplotypeFrequencyLookupService>();
        frequencySetLookup.GetSingleHaplotypeFrequencySet(default).ReturnsForAnyArgs(callInfo =>
        {
            // Null only when a test sets up the lookup again: that call is not a lookup.
            var metadata = callInfo.Arg<FrequencySetMetadata>();
            return metadata == null ? null : frequencySets[(metadata.RegistryCode, metadata.EthnicityCode)];
        });

        metrics = Substitute.For<IDonorGenotypePrecomputationMetrics>();

        processor = new DonorGenotypePrecomputationBatchProcessor(
            repositoryFactory,
            valueService,
            frequencySetLookup,
            settings,
            metrics,
            NullLogger<DonorGenotypePrecomputationBatchProcessor>.Instance);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ProcessBatch_ClaimsTheBatchOfTheMessage_ForTheLeaseOfTheSettings(bool isRedelivery)
    {
        await processor.ProcessBatch(request, database, isRedelivery);

        await repository.Received(1).TryClaimBatch(Arg.Is<DonorGenotypePrecomputationBatchClaim>(claim =>
            claim.DataRefreshRecordId == request.DataRefreshRecordId
            && claim.RunId == request.RunId
            && claim.BatchId == request.BatchId
            && claim.IsRedelivery == isRedelivery
            && claim.LeaseDuration == TimeSpan.FromMinutes(settings.BatchLeaseMinutes)));
    }

    [Test]
    public async Task ProcessBatch_UsesTheGivenDatabase()
    {
        // The message names no database: the worker reads the database of the refresh record of the message.
        await processor.ProcessBatch(request, database, false);

        repositoryFactory.DidNotReceive().GetDonorGenotypePrecomputationRepositoryForDatabase(database.Other());
        repositoryFactory.DidNotReceive().GetSubjectGenotypeSetRepositoryForDatabase(database.Other());
    }

    [Test]
    public async Task ProcessBatch_WhenTheBatchIsNotTheWorkersToTake_SkipsIt_AndWritesNothing()
    {
        repository.TryClaimBatch(default).ReturnsForAnyArgs((ClaimedDonorGenotypePrecomputationBatch)null);

        var result = await processor.ProcessBatch(request, database, false);

        result.Should().Be(BatchResult.Skipped);
        await repository.DidNotReceiveWithAnyArgs().GetGroupsToCompute(default, default, default);
        await repository.DidNotReceiveWithAnyArgs().RecordGroupOutcomes(default, default);
        await repository.DidNotReceiveWithAnyArgs().TryMarkBatchResultsReceived(default, default, default);
        metrics.Received(1).RecordBatch(BatchResult.Skipped, Arg.Any<TimeSpan>(), 0, 0);
    }

    [Test]
    public async Task ProcessBatch_ReadsTheGroupsOfTheRangeOfTheBatch()
    {
        await processor.ProcessBatch(request, database, false);

        await repository.Received(1).GetGroupsToCompute(claimedBatch.RunId, claimedBatch.FirstGroupId, claimedBatch.LastGroupId);
    }

    [Test]
    public async Task ProcessBatch_StoresTheValueOfEachGroup_AndRecordsItOnTheGroup()
    {
        var groups = GivenGroups(3);

        await processor.ProcessBatch(request, database, false);

        var outcomes = RecordedOutcomes();
        outcomes.Select(outcome => outcome.GroupId).Should().BeEquivalentTo(groups.Select(group => group.GroupId));
        outcomes.Should().OnlyContain(outcome => outcome.SubjectGenotypeSetValueId != null && outcome.FailureMessage == null);
    }

    [Test]
    public async Task ProcessBatch_ImputesEachGroupAgainstTheFrequencySetOfItsPair_AtTheVersionOfTheRun()
    {
        var group = GivenGroups(1).Single();
        var frequencySet = frequencySets[(group.RegistryCode, group.EthnicityCode)];

        await processor.ProcessBatch(request, database, false);

        await valueService.Received(1).GetOrComputeValueIds(
            Arg.Is<IReadOnlyList<SubjectGenotypeSetValueRequest>>(requests =>
                requests.Single().FrequencySet == frequencySet
                && requests.Single().HlaTyping.Equals(group.HlaTyping)
                && requests.Single().AllowedLociKey == group.AllowedLociKey),
            claimedBatch.HlaNomenclatureVersion,
            database);
    }

    [Test]
    public async Task ProcessBatch_LooksUpTheFrequencySetOfEachPairOnce()
    {
        var pair = NewPair();
        GivenGroups(3, pair);

        await processor.ProcessBatch(request, database, false);

        await frequencySetLookup.Received(1).GetSingleHaplotypeFrequencySet(Arg.Is<FrequencySetMetadata>(metadata =>
            metadata.RegistryCode == pair.RegistryCode && metadata.EthnicityCode == pair.EthnicityCode));
    }

    [Test]
    public async Task ProcessBatch_ComputesTheGroupsInTheOrderOfTheirFrequencySets()
    {
        // A batch across the boundary of two pairs then loads each set once.
        var laterSetPair = NewPair(frequencySetId: 2);
        var earlierSetPair = NewPair(frequencySetId: 1);
        var groups = new[] { NewGroup(laterSetPair), NewGroup(earlierSetPair), NewGroup(laterSetPair), NewGroup(earlierSetPair) };
        GivenGroups(groups);

        await processor.ProcessBatch(request, database, false);

        await valueService.Received(1).GetOrComputeValueIds(
            Arg.Is<IReadOnlyList<SubjectGenotypeSetValueRequest>>(requests =>
                requests.Select(r => r.FrequencySet.Id).SequenceEqual(new[] { 1, 1, 2, 2 })),
            Arg.Any<string>(),
            Arg.Any<TransientDatabase>());
    }

    [Test]
    public async Task ProcessBatch_WritesTheDonorRowsOfTheRange_InChunks()
    {
        var assignments = fixture.CreateMany<DonorSubjectGenotypeSetAssignment>(DonorGenotypePrecomputationBatchProcessor.DonorAssignmentChunkSize + 1).ToList();
        repository.GetDonorAssignments(claimedBatch.RunId, claimedBatch.FirstGroupId, claimedBatch.LastGroupId).Returns(assignments);

        await processor.ProcessBatch(request, database, false);

        var upserts = subjectGenotypeSetRepository.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ISubjectGenotypeSetRepository.UpsertDonorAssignments))
            .Select(call => (IReadOnlyCollection<DonorSubjectGenotypeSetAssignment>)call.GetArguments()[0])
            .ToList();
        upserts.Select(chunk => chunk.Count).Should().Equal(DonorGenotypePrecomputationBatchProcessor.DonorAssignmentChunkSize, 1);
        upserts.SelectMany(chunk => chunk).Should().Equal(assignments);
    }

    [Test]
    public async Task ProcessBatch_WritesTheValuesBeforeTheDonorRows_AndTheDonorRowsBeforeTheResult()
    {
        GivenGroups(2);
        repository.GetDonorAssignments(default, default, default).ReturnsForAnyArgs(fixture.CreateMany<DonorSubjectGenotypeSetAssignment>().ToList());

        await processor.ProcessBatch(request, database, false);

        Received.InOrder(() =>
        {
            repository.RecordGroupOutcomes(claimedBatch.RunId, Arg.Any<IReadOnlyCollection<Outcome>>());
            subjectGenotypeSetRepository.UpsertDonorAssignments(Arg.Any<IReadOnlyCollection<DonorSubjectGenotypeSetAssignment>>());
            repository.TryMarkBatchResultsReceived(claimedBatch.BatchId, Arg.Any<Guid>(), Arg.Any<int>());
        });
    }

    [Test]
    public async Task ProcessBatch_WhenEveryGroupHasItsValue_RecordsResultsReceived_UnderTheLeaseOfTheClaim()
    {
        GivenGroups(2);

        var result = await processor.ProcessBatch(request, database, false);

        result.Should().Be(BatchResult.ResultsReceived);
        await repository.Received(1).TryMarkBatchResultsReceived(claimedBatch.BatchId, ClaimedLeaseOwner(), 0);
    }

    [Test]
    public async Task ProcessBatch_ForAGroupThatFailsPermanently_RecordsItsFailure_AndTheBatchSucceeds()
    {
        // A bad typing fails its own group, not the other groups of its batch.
        var groups = GivenGroups(3);
        var failure = new HlaMetadataDictionaryException(fixture.Create<string>(), fixture.Create<string>(), fixture.Create<string>());
        GivenTheValueOf(groups[1], new SubjectGenotypeSetValueOutcome(null, new SubjectGenotypeSetValueFailure(PrecomputeErrorKind.KnownPermanent, failure)));

        var result = await processor.ProcessBatch(request, database, false);

        result.Should().Be(BatchResult.ResultsReceived);
        RecordedOutcomes().Should().ContainSingle(outcome => outcome.GroupId == groups[1].GroupId)
            .Which.Should().Be(Outcome.Failed(groups[1].GroupId, failure.Message));
        await repository.Received(1).TryMarkBatchResultsReceived(claimedBatch.BatchId, Arg.Any<Guid>(), 1);
    }

    [Test]
    public async Task ProcessBatch_ForAGroupWithNoRepresentativeDonor_FailsTheGroup_AndDoesNotComputeIt()
    {
        var group = NewGroup(NewPair()) with { HlaTyping = null };
        GivenGroups(group);

        await processor.ProcessBatch(request, database, false);

        RecordedOutcomes().Should().ContainSingle().Which.Should().Match<Outcome>(outcome =>
            outcome.GroupId == group.GroupId && outcome.FailureMessage.Contains(group.RepresentativeDonorId.ToString()));
        await valueService.Received(1).GetOrComputeValueIds(
            Arg.Is<IReadOnlyList<SubjectGenotypeSetValueRequest>>(requests => requests.Count == 0), Arg.Any<string>(), Arg.Any<TransientDatabase>());
    }

    [Test]
    public async Task ProcessBatch_ForAGroupWithAnErrorOfUnknownCause_LeavesTheGroup_WritesTheDonorRows_AndFailsTheBatch()
    {
        // The group has no value and no failure, so a retry computes it again. The other groups are done.
        var groups = GivenGroups(2);
        var failure = new InvalidOperationException(fixture.Create<string>());
        GivenTheValueOf(groups[0], new SubjectGenotypeSetValueOutcome(null, new SubjectGenotypeSetValueFailure(PrecomputeErrorKind.Unknown, failure)));

        var result = await processor.ProcessBatch(request, database, false);

        result.Should().Be(BatchResult.Failed);
        RecordedOutcomes().Should().ContainSingle().Which.GroupId.Should().Be(groups[1].GroupId);
        await repository.Received(1).GetDonorAssignments(claimedBatch.RunId, claimedBatch.FirstGroupId, claimedBatch.LastGroupId);
        await repository.Received(1).TryMarkBatchFailed(claimedBatch.BatchId, ClaimedLeaseOwner(),
            Arg.Is<DonorGenotypePrecomputationBatchFailure>(batchFailure =>
                batchFailure.FailureMessage.Contains(failure.Message) && batchFailure.FailureException.Contains(failure.Message)));
    }

    [Test]
    public async Task ProcessBatch_WhenTheValuesStop_KeepsTheDoneWork_WritesNoDonorRows_AndFailsTheBatch()
    {
        // One pair, so the groups are computed in id order.
        var groups = GivenGroups(2, NewPair());
        var firstComputed = groups.MinBy(group => group.GroupId);
        var stop = new TimeoutException(fixture.Create<string>());
        valueService.GetOrComputeValueIds(default, default, default).ReturnsForAnyArgs(new SubjectGenotypeSetValueResults(
            [new SubjectGenotypeSetValueOutcome(nextValueId, null), new SubjectGenotypeSetValueOutcome(null, null)],
            stop));

        var result = await processor.ProcessBatch(request, database, false);

        result.Should().Be(BatchResult.Failed);
        RecordedOutcomes().Should().ContainSingle().Which.Should().Be(Outcome.Stored(firstComputed.GroupId, nextValueId));
        await repository.DidNotReceiveWithAnyArgs().GetDonorAssignments(default, default, default);
        await repository.Received(1).TryMarkBatchFailed(claimedBatch.BatchId, Arg.Any<Guid>(),
            Arg.Is<DonorGenotypePrecomputationBatchFailure>(batchFailure => batchFailure.FailureMessage.Contains(stop.Message)));
    }

    [Test]
    public async Task ProcessBatch_WhenMoreGroupsFailThanABatchCanHave_FailsTheBatch()
    {
        // So many bad typings in one batch are a sign that something else is wrong, such as missing dictionary data.
        settings.MaxGroupFailuresPerBatch = 1;
        var groups = GivenGroups(2);
        foreach (var group in groups)
        {
            GivenTheValueOf(group, PermanentFailure());
        }

        var result = await processor.ProcessBatch(request, database, false);

        result.Should().Be(BatchResult.Failed);
        await repository.Received(1).TryMarkBatchFailed(claimedBatch.BatchId, Arg.Any<Guid>(),
            Arg.Is<DonorGenotypePrecomputationBatchFailure>(batchFailure => batchFailure.FailedGroupCount == 2));
    }

    [Test]
    public async Task ProcessBatch_AtTheLimitOfGroupFailures_Succeeds()
    {
        settings.MaxGroupFailuresPerBatch = 2;
        foreach (var group in GivenGroups(2))
        {
            GivenTheValueOf(group, PermanentFailure());
        }

        var result = await processor.ProcessBatch(request, database, false);

        result.Should().Be(BatchResult.ResultsReceived);
    }

    [Test]
    public async Task ProcessBatch_WhenAFrequencySetLookupHasATemporaryError_StopsBeforeItComputes()
    {
        GivenGroups(2);
        var stop = new TimeoutException(fixture.Create<string>());
        frequencySetLookup.GetSingleHaplotypeFrequencySet(default).ThrowsAsyncForAnyArgs(stop);

        var result = await processor.ProcessBatch(request, database, false);

        result.Should().Be(BatchResult.Failed);
        await valueService.DidNotReceiveWithAnyArgs().GetOrComputeValueIds(default, default, default);
        await repository.Received(1).TryMarkBatchFailed(claimedBatch.BatchId, Arg.Any<Guid>(),
            Arg.Is<DonorGenotypePrecomputationBatchFailure>(batchFailure => batchFailure.FailureMessage.Contains(stop.Message)));
    }

    [Test]
    public async Task ProcessBatch_WhenAPairHasNoFrequencySet_LeavesItsGroups_ComputesTheOthers_AndFailsTheBatch()
    {
        // For example no global set: a retry looks again, and the failure of the batch shows the message.
        var pairWithNoSet = NewPair();
        var groupWithNoSet = NewGroup(pairWithNoSet);
        var otherGroup = NewGroup(NewPair());
        GivenGroups(groupWithNoSet, otherGroup);
        var failure = new Exception(fixture.Create<string>());
        frequencySetLookup.GetSingleHaplotypeFrequencySet(Arg.Is<FrequencySetMetadata>(metadata => metadata.RegistryCode == pairWithNoSet.RegistryCode))
            .ThrowsAsync(failure);

        var result = await processor.ProcessBatch(request, database, false);

        result.Should().Be(BatchResult.Failed);
        RecordedOutcomes().Should().ContainSingle().Which.GroupId.Should().Be(otherGroup.GroupId);
        await repository.Received(1).TryMarkBatchFailed(claimedBatch.BatchId, Arg.Any<Guid>(),
            Arg.Is<DonorGenotypePrecomputationBatchFailure>(batchFailure => batchFailure.FailureMessage.Contains(failure.Message)));
    }

    [Test]
    public async Task ProcessBatch_WhenTheLeaseIsLost_ReturnsLeaseLost()
    {
        // A sweep or a redelivered message took the batch. The work is kept, and the other claim records the result.
        GivenGroups(1);
        repository.TryMarkBatchResultsReceived(default, default, default).ReturnsForAnyArgs(false);

        var result = await processor.ProcessBatch(request, database, false);

        result.Should().Be(BatchResult.LeaseLost);
    }

    [Test]
    public async Task ProcessBatch_WhenAWriteFails_Throws()
    {
        // The worker then abandons the message, and Service Bus delivers it again.
        GivenGroups(1);
        var failure = new InvalidOperationException(fixture.Create<string>());
        repository.RecordGroupOutcomes(default, default).ThrowsAsyncForAnyArgs(failure);

        var act = () => processor.ProcessBatch(request, database, false);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
    }

    [Test]
    public async Task ProcessBatch_WithNoGroupsToCompute_WritesTheDonorRows_AndSucceeds()
    {
        // A batch whose values an earlier attempt stored, but which stopped before its donor rows or its result.
        GivenGroups();

        var result = await processor.ProcessBatch(request, database, false);

        result.Should().Be(BatchResult.ResultsReceived);
        await repository.Received(1).GetDonorAssignments(claimedBatch.RunId, claimedBatch.FirstGroupId, claimedBatch.LastGroupId);
    }

    [Test]
    public async Task ProcessBatch_RecordsTheMetricsOfTheBatch()
    {
        var sharedSetPair = NewPair(frequencySetId: fixture.Create<int>());
        var otherPairOfTheSameSet = NewPair(frequencySetId: frequencySets[sharedSetPair].Id);
        GivenGroups(NewGroup(sharedSetPair), NewGroup(otherPairOfTheSameSet), NewGroup(NewPair()));

        await processor.ProcessBatch(request, database, false);

        metrics.Received(1).RecordBatch(BatchResult.ResultsReceived, Arg.Any<TimeSpan>(), 3, 2);
    }

    private Guid ClaimedLeaseOwner() =>
        ((DonorGenotypePrecomputationBatchClaim)repository.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(IDonorGenotypePrecomputationRepository.TryClaimBatch))
            .GetArguments()[0]).LeaseOwner;

    private IReadOnlyCollection<Outcome> RecordedOutcomes() =>
        (IReadOnlyCollection<Outcome>)repository.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(IDonorGenotypePrecomputationRepository.RecordGroupOutcomes))
            .GetArguments()[1];

    /// <remarks>The requests are null only when a test sets up the value service again.</remarks>
    private void EveryValueIsStored() =>
        valueService.GetOrComputeValueIds(default, default, default).ReturnsForAnyArgs(callInfo =>
            new SubjectGenotypeSetValueResults(
                callInfo.Arg<IReadOnlyList<SubjectGenotypeSetValueRequest>>()?
                    .Select(valueRequest => outcomesByTyping.GetValueOrDefault(valueRequest.HlaTyping) ?? new SubjectGenotypeSetValueOutcome(nextValueId++, null))
                    .ToList() ?? [],
                null));

    private void GivenTheValueOf(DonorGenotypePrecomputationGroupToCompute group, SubjectGenotypeSetValueOutcome outcome) =>
        outcomesByTyping[group.HlaTyping] = outcome;

    private SubjectGenotypeSetValueOutcome PermanentFailure() => new(null, new SubjectGenotypeSetValueFailure(
        PrecomputeErrorKind.KnownPermanent,
        new HlaMetadataDictionaryException(fixture.Create<string>(), fixture.Create<string>(), fixture.Create<string>())));

    private List<DonorGenotypePrecomputationGroupToCompute> GivenGroups(int count, (string RegistryCode, string EthnicityCode)? pair = null)
    {
        var groups = Enumerable.Range(0, count).Select(_ => NewGroup(pair ?? NewPair())).ToList();
        GivenGroups(groups.ToArray());
        return groups;
    }

    private void GivenGroups(params DonorGenotypePrecomputationGroupToCompute[] groups) =>
        repository.GetGroupsToCompute(claimedBatch.RunId, claimedBatch.FirstGroupId, claimedBatch.LastGroupId).Returns(groups.ToList());

    /// <summary>A registry and ethnicity pair, and the frequency set that the lookup gives it.</summary>
    private (string RegistryCode, string EthnicityCode) NewPair(int? frequencySetId = null)
    {
        var pair = (fixture.Create<string>(), fixture.Create<string>());
        frequencySets[pair] = new HaplotypeFrequencySet { Id = frequencySetId ?? fixture.Create<int>() };
        return pair;
    }

    private DonorGenotypePrecomputationGroupToCompute NewGroup((string RegistryCode, string EthnicityCode) pair) => new(
        fixture.Create<int>(),
        fixture.Create<AllowedLociKey>(),
        fixture.Create<int>(),
        pair.RegistryCode,
        pair.EthnicityCode,
        Typing());

    private PhenotypeInfo<string> Typing()
    {
        var builder = new PhenotypeInfoBuilder<string>();
        foreach (var locus in new[] { Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1 })
        {
            builder = builder.WithDataAt(locus, fixture.Create<string>(), fixture.Create<string>());
        }

        return builder.Build();
    }
}
