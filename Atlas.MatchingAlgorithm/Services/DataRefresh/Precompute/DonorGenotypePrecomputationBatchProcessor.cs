#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Microsoft.Extensions.Logging;
using Outcome = Atlas.MatchingAlgorithm.Data.Models.Precompute.DonorGenotypePrecomputationGroupOutcome;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;

/// <summary>What happened to the batch of one message. The worker completes the message in every case.</summary>
public enum DonorGenotypePrecomputationBatchResult
{
    /// <summary>
    /// The batch was not the worker's to take: its run is not running or belongs to another refresh, the batch is done,
    /// or another worker holds it.
    /// </summary>
    Skipped,

    /// <summary>Every group has its value or its failure, and the donor rows are written.</summary>
    ResultsReceived,

    /// <summary>The batch failed: the requeue sweep sends it again, or gives up on it when it has no retries left.</summary>
    Failed,

    /// <summary>
    /// The worker finished the batch, but it no longer held the lease: a sweep or a redelivered message took the batch.
    /// The work is kept - every write is safe to repeat - but the other claim records the result.
    /// </summary>
    LeaseLost
}

public interface IDonorGenotypePrecomputationBatchProcessor
{
    /// <summary>
    /// Processes the batch that a message names: claims it, computes the values of its groups, stores them, writes the
    /// donor rows of the batch, and records the result on the batch row.
    /// </summary>
    /// <param name="request">The message.</param>
    /// <param name="isRedelivery">
    /// True when Service Bus delivers the message again: its worker stopped without completing it, so the claim can take
    /// the batch from a live lease.
    /// </param>
    /// <returns>What happened to the batch.</returns>
    /// <exception cref="Exception">
    /// Any exception: the batch could not be finished, for example because the database failed. The worker abandons the
    /// message, so that Service Bus delivers it again.
    /// </exception>
    Task<DonorGenotypePrecomputationBatchResult> ProcessBatch(DonorGenotypePrecomputationBatchRequest request, bool isRedelivery);
}

/// <summary>
/// The work of one batch message of the donor genotype precomputation stage, in the database of the
/// <see cref="DonorGenotypePrecomputationTarget"/> of the worker.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every write is safe to repeat.</b> A value is stored once per key, a group keeps the first value that is stored
/// for it, and the donor rows are an upsert. So a batch that runs again, or runs twice at the same time, does no harm.
/// Only the result on the batch row is a compare-and-swap, and only the holder of the lease can make it.
/// </para>
///
/// <para>
/// <b>Failures:</b> a known permanent error of a group, such as a bad typing, fails only that group, and the batch
/// goes on. A known temporary error stops the batch, keeps its done work, and fails the batch. An error of unknown cause
/// leaves its group with no value and no failure, and the batch goes on; at the end the batch fails, so a retry computes
/// only the groups with no value. See <see cref="PrecomputeErrorKind"/>.
/// </para>
/// </remarks>
public class DonorGenotypePrecomputationBatchProcessor : IDonorGenotypePrecomputationBatchProcessor
{
    private const string LoggingPrefix = "DONOR GENOTYPE PRECOMPUTATION:";

    /// <summary>
    /// Donor rows per upsert: 2,000 donors times the four locus combinations, the size of the upserts of the donor
    /// import. The upsert takes an exclusive table lock, so a smaller one waits less.
    /// </summary>
    internal const int DonorAssignmentChunkSize = 8000;

    private readonly IStaticallyChosenDatabaseRepositoryFactory repositoryFactory;
    private readonly DonorGenotypePrecomputationTarget target;
    private readonly ISubjectGenotypeSetValueService valueService;
    private readonly IHaplotypeFrequencyLookupService frequencySetLookup;
    private readonly DonorGenotypePrecomputationWorkerSettings settings;
    private readonly IDonorGenotypePrecomputationMetrics metrics;
    private readonly ILogger<DonorGenotypePrecomputationBatchProcessor> logger;

    public DonorGenotypePrecomputationBatchProcessor(
        IStaticallyChosenDatabaseRepositoryFactory repositoryFactory,
        DonorGenotypePrecomputationTarget target,
        ISubjectGenotypeSetValueService valueService,
        IHaplotypeFrequencyLookupService frequencySetLookup,
        DonorGenotypePrecomputationWorkerSettings settings,
        IDonorGenotypePrecomputationMetrics metrics,
        ILogger<DonorGenotypePrecomputationBatchProcessor> logger)
    {
        this.repositoryFactory = repositoryFactory;
        this.target = target;
        this.valueService = valueService;
        this.frequencySetLookup = frequencySetLookup;
        this.settings = settings;
        this.metrics = metrics;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<DonorGenotypePrecomputationBatchResult> ProcessBatch(DonorGenotypePrecomputationBatchRequest request, bool isRedelivery)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        var repository = repositoryFactory.GetDonorGenotypePrecomputationRepositoryForDatabase(target.Database);

        var claim = NewClaim(request, isRedelivery);
        var batch = await repository.TryClaimBatch(claim);
        if (batch == null)
        {
            // Debug, like the success below: the metrics count every result, and a full run has too many batches to log
            // each one at a higher level.
            logger.LogDebug(
                LoggingPrefix + " Batch {BatchId} of run {RunId} (data refresh record {DataRefreshRecordId}, {TargetDatabase}) was skipped: " +
                "its run is not running, or the batch is done or held.",
                request.BatchId, request.RunId, request.DataRefreshRecordId, target.Database);
            metrics.RecordBatch(DonorGenotypePrecomputationBatchResult.Skipped, Stopwatch.GetElapsedTime(startTimestamp), 0, 0);
            return DonorGenotypePrecomputationBatchResult.Skipped;
        }

        var work = await ComputeGroups(repository, batch);
        await repository.RecordGroupOutcomes(batch.RunId, work.Outcomes);

        // A batch that stopped writes no donor rows: the database or the storage fails, and the retry writes them.
        if (work.StoppedBy == null)
        {
            await WriteDonorRows(repository, batch);
        }

        var result = await RecordResult(repository, batch, claim.LeaseOwner, work, request);

        metrics.RecordBatch(result, Stopwatch.GetElapsedTime(startTimestamp), work.ComputedGroupCount, work.FrequencySetCount);
        return result;
    }

    private DonorGenotypePrecomputationBatchClaim NewClaim(DonorGenotypePrecomputationBatchRequest request, bool isRedelivery) => new(
        request.DataRefreshRecordId,
        request.RunId,
        request.BatchId,
        Guid.NewGuid(),
        TimeSpan.FromMinutes(settings.BatchLeaseMinutes),
        isRedelivery);

    private async Task<BatchWork> ComputeGroups(IDonorGenotypePrecomputationRepository repository, ClaimedDonorGenotypePrecomputationBatch batch)
    {
        var groups = await repository.GetGroupsToCompute(batch.RunId, batch.FirstGroupId, batch.LastGroupId);
        var work = new BatchWork { ComputedGroupCount = groups.Count };

        // The build takes the representative from Donors, and Donors does not change during the stage. A missing donor is
        // a fault of the build, and a retry cannot fix it.
        foreach (var group in groups.Where(group => group.HlaTyping is null))
        {
            work.Outcomes.Add(Outcome.Failed(group.GroupId, $"The representative donor {group.RepresentativeDonorId} of the group is not in Donors."));
        }

        var typedGroups = groups.Where(group => group.HlaTyping is not null).ToList();
        var frequencySets = await ResolveFrequencySets(typedGroups, work);
        work.FrequencySetCount = frequencySets.Values.Select(set => set.Id).Distinct().Count();

        if (work.StoppedBy != null)
        {
            return work;
        }

        // By frequency set, so a batch across the boundary of two registry and ethnicity pairs loads each set once.
        var groupsToCompute = typedGroups
            .Where(group => frequencySets.ContainsKey(RegistryEthnicityCodesPairOf(group)))
            .OrderBy(group => frequencySets[RegistryEthnicityCodesPairOf(group)].Id)
            .ThenBy(group => group.GroupId)
            .ToList();

        var results = await valueService.GetOrComputeValueIds(
            groupsToCompute
                .Select(group => new SubjectGenotypeSetValueRequest(
                    // Not null: the groups with no typing failed above.
                    group.HlaTyping!,
                    group.AllowedLociKey,
                    frequencySets[RegistryEthnicityCodesPairOf(group)],
                    $"group {group.GroupId} (donor {group.RepresentativeDonorId})"))
                .ToList(),
            batch.HlaNomenclatureVersion,
            target.Database);

        for (var i = 0; i < groupsToCompute.Count; i++)
        {
            var group = groupsToCompute[i];
            var outcome = results.Outcomes[i];

            if (outcome.ValueId != null)
            {
                work.Outcomes.Add(Outcome.Stored(group.GroupId, outcome.ValueId.Value));
            }
            else if (outcome.Failure is { Kind: PrecomputeErrorKind.KnownPermanent } permanentFailure)
            {
                work.Outcomes.Add(Outcome.Failed(group.GroupId, permanentFailure.Exception.Message));
            }
            else if (outcome.Failure is { } failure)
            {
                work.UnknownFailures.Add(new GroupError(group.GroupId, failure.Exception));
            }
        }

        work.StoppedBy = results.StoppedBy;
        return work;
    }

    /// <summary>
    /// The frequency set of each registry and ethnicity pair of the groups, as a search chooses it. A pair whose lookup
    /// fails with a known temporary error stops the batch. A pair whose lookup fails otherwise gives its groups an error
    /// of unknown cause, and a retry looks again.
    /// </summary>
    private async Task<Dictionary<(string? RegistryCode, string? EthnicityCode), HaplotypeFrequencySet>> ResolveFrequencySets(
        IEnumerable<DonorGenotypePrecomputationGroupToCompute> groups,
        BatchWork work)
    {
        var frequencySets = new Dictionary<(string? RegistryCode, string? EthnicityCode), HaplotypeFrequencySet>();

        foreach (var pairGroups in groups.GroupBy(RegistryEthnicityCodesPairOf))
        {
            var pair = pairGroups.Key;
            try
            {
                // FrequencySetMetadata marks the codes as not null, but a donor can have no code. The lookup then takes
                // the set of the registry, or the global set.
                frequencySets[pair] = await frequencySetLookup.GetSingleHaplotypeFrequencySet(
                    new FrequencySetMetadata { RegistryCode = pair.RegistryCode!, EthnicityCode = pair.EthnicityCode! });
            }
            catch (Exception exception)
            {
                if (PrecomputeErrorClassifier.Classify(exception) == PrecomputeErrorKind.KnownTemporary)
                {
                    work.StoppedBy = exception;
                    break;
                }

                work.UnknownFailures.AddRange(pairGroups.Select(group => new GroupError(group.GroupId, exception)));
            }
        }

        return frequencySets;
    }

    private async Task WriteDonorRows(IDonorGenotypePrecomputationRepository repository, ClaimedDonorGenotypePrecomputationBatch batch)
    {
        // Every group of the range that has a value, also from an earlier attempt: a stopped attempt wrote no donor rows.
        var assignments = await repository.GetDonorAssignments(batch.RunId, batch.FirstGroupId, batch.LastGroupId);

        var subjectGenotypeSetRepository = repositoryFactory.GetSubjectGenotypeSetRepositoryForDatabase(target.Database);
        foreach (var chunk in assignments.Chunk(DonorAssignmentChunkSize))
        {
            await subjectGenotypeSetRepository.UpsertDonorAssignments(chunk);
        }
    }

    private async Task<DonorGenotypePrecomputationBatchResult> RecordResult(
        IDonorGenotypePrecomputationRepository repository,
        ClaimedDonorGenotypePrecomputationBatch batch,
        Guid leaseOwner,
        BatchWork work,
        DonorGenotypePrecomputationBatchRequest request)
    {
        var failedGroups = work.Outcomes.Where(outcome => outcome.SubjectGenotypeSetValueId == null).ToList();
        var failure = BatchFailure(work, failedGroups);

        var wasRecorded = failure == null
            ? await repository.TryMarkBatchResultsReceived(batch.BatchId, leaseOwner, failedGroups.Count)
            : await repository.TryMarkBatchFailed(batch.BatchId, leaseOwner, failure);

        if (!wasRecorded)
        {
            logger.LogWarning(
                LoggingPrefix + " Batch {BatchId} of run {RunId} (data refresh record {DataRefreshRecordId}, {TargetDatabase}) was finished, " +
                "but its lease was lost: the claim that took it records the result.",
                request.BatchId, request.RunId, request.DataRefreshRecordId, target.Database);
            return DonorGenotypePrecomputationBatchResult.LeaseLost;
        }

        if (failure != null)
        {
            logger.LogWarning(
                LoggingPrefix + " Batch {BatchId} of run {RunId} (data refresh record {DataRefreshRecordId}, {TargetDatabase}) failed, " +
                "attempt {Attempt}: {FailureMessage}",
                request.BatchId, request.RunId, request.DataRefreshRecordId, target.Database, batch.RetryCount + 1, failure.FailureMessage);
            return DonorGenotypePrecomputationBatchResult.Failed;
        }

        logger.LogDebug(
            LoggingPrefix + " Batch {BatchId} of run {RunId} (data refresh record {DataRefreshRecordId}, {TargetDatabase}) has its results: " +
            "{ComputedGroupCount} group(s) computed, {FailedGroupCount} failed.",
            request.BatchId, request.RunId, request.DataRefreshRecordId, target.Database, work.ComputedGroupCount, failedGroups.Count);
        return DonorGenotypePrecomputationBatchResult.ResultsReceived;
    }

    /// <summary>Why the batch fails, or null when it succeeds.</summary>
    private DonorGenotypePrecomputationBatchFailure? BatchFailure(BatchWork work, IReadOnlyCollection<Outcome> failedGroups)
    {
        if (work.StoppedBy != null)
        {
            return Failure($"The batch stopped at a temporary error: {work.StoppedBy.Message}", work.StoppedBy, failedGroups);
        }

        if (work.UnknownFailures.Count > 0)
        {
            var first = work.UnknownFailures[0];
            return Failure(
                $"{work.UnknownFailures.Count} group(s) failed with an error of unknown cause, and a retry computes them again. " +
                $"First, group {first.GroupId}: {first.Exception.Message}",
                first.Exception,
                failedGroups);
        }

        if (failedGroups.Count > settings.MaxGroupFailuresPerBatch)
        {
            var first = failedGroups.First();
            return Failure(
                $"{failedGroups.Count} groups failed, more than the {settings.MaxGroupFailuresPerBatch} that one batch can have. " +
                $"First, group {first.GroupId}: {first.FailureMessage}",
                null,
                failedGroups);
        }

        return null;
    }

    private static DonorGenotypePrecomputationBatchFailure Failure(string message, Exception? exception, IReadOnlyCollection<Outcome> failedGroups) =>
        new(message, exception?.ToString(), failedGroups.Count);

    private static (string? RegistryCode, string? EthnicityCode) RegistryEthnicityCodesPairOf(DonorGenotypePrecomputationGroupToCompute group) =>
        (group.RegistryCode, group.EthnicityCode);

    private sealed record GroupError(int GroupId, Exception Exception);

    /// <summary>What the computation of a batch produced.</summary>
    private sealed class BatchWork
    {
        /// <summary>The value or the permanent failure of each group that has one: what the group rows record.</summary>
        public List<Outcome> Outcomes { get; } = [];

        /// <summary>The groups that failed with an error of unknown cause. They get no value and no failure.</summary>
        public List<GroupError> UnknownFailures { get; } = [];

        /// <summary>The known temporary error that stopped the batch, or null.</summary>
        public Exception? StoppedBy { get; set; }

        public int ComputedGroupCount { get; init; }

        public int FrequencySetCount { get; set; }
    }
}
