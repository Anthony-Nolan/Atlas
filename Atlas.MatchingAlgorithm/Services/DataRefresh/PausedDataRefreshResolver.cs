#nullable enable

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Models.DataRefresh;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Settings;
using Microsoft.Extensions.Logging;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh;

/// <summary>
/// Takes a manual decision for a data refresh that is paused, because more donors failed in the donor genotype precomputation
/// stage than the threshold allows. Support calls it through the <see cref="ResolvePausedDataRefreshRequest.EndpointName"/>
/// endpoint.
/// </summary>
public interface IPausedDataRefreshResolver
{
    /// <summary>
    /// Takes the decision with one compare-and-swap update of the record, which ends the pause.
    /// <see cref="DataRefreshManualDecision.Complete"/> and <see cref="DataRefreshManualDecision.RetryFailedBatches"/> are
    /// recorded on the record, and a refresh request is published: the resumed refresh applies the decision.
    /// <see cref="DataRefreshManualDecision.Fail"/> closes the record as failed, then notifies, then cancels the run.
    /// </summary>
    /// <remarks>
    /// For Complete and RetryFailedBatches, only the persistent database is written, so the dormant database, which is
    /// scaled down and can be paused, is not needed. A Fail decision also removes the staging data from the dormant
    /// database, and that is best effort.
    /// </remarks>
    /// <param name="request">The body of the request. Null when the body is empty.</param>
    Task<PausedDataRefreshResolution> Resolve(ResolvePausedDataRefreshRequest? request);
}

/// <summary>What happened to a manual decision.</summary>
public enum PausedDataRefreshResolutionOutcome
{
    /// <summary>The decision is taken.</summary>
    Accepted,

    /// <summary>The body is not valid. Nothing changed.</summary>
    InvalidRequest,

    /// <summary>There is no record with the id. Nothing changed.</summary>
    NotFound,

    /// <summary>The record is not paused, is closed, or has a decision already. Nothing changed.</summary>
    Conflict,

    /// <summary>The decision is recorded, but the refresh request was not published. The watchdog requests the refresh later.</summary>
    NotPublished
}

/// <summary>The result of a manual decision, for the response of the endpoint.</summary>
/// <param name="Message">What happened, and what happens next.</param>
public sealed record PausedDataRefreshResolution(
    PausedDataRefreshResolutionOutcome Outcome,
    string Message,
    int? DataRefreshRecordId = null,
    DataRefreshManualDecision? Decision = null);

/// <inheritdoc />
internal class PausedDataRefreshResolver : IPausedDataRefreshResolver
{
    private const string LoggingPrefix = "DATA REFRESH MANUAL DECISION:";

    /// <summary>The longest <see cref="ResolvePausedDataRefreshRequest.DecidedBy"/> that goes into the support comments.</summary>
    internal const int MaxDecidedByLength = 100;

    private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
    private readonly IDataRefreshServiceBusClient serviceBusClient;
    private readonly IDataRefreshCompletionNotifier completionNotifier;
    private readonly IDonorGenotypePrecomputationRunCanceller precomputationRunCanceller;
    private readonly IDataRefreshManualDecisionEvents manualDecisionEvents;
    private readonly DataRefreshSettings dataRefreshSettings;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<PausedDataRefreshResolver> logger;

    public PausedDataRefreshResolver(
        IDataRefreshHistoryRepository dataRefreshHistoryRepository,
        IDataRefreshServiceBusClient serviceBusClient,
        IDataRefreshCompletionNotifier completionNotifier,
        IDonorGenotypePrecomputationRunCanceller precomputationRunCanceller,
        IDataRefreshManualDecisionEvents manualDecisionEvents,
        DataRefreshSettings dataRefreshSettings,
        TimeProvider timeProvider,
        ILogger<PausedDataRefreshResolver> logger)
    {
        this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
        this.serviceBusClient = serviceBusClient;
        this.completionNotifier = completionNotifier;
        this.precomputationRunCanceller = precomputationRunCanceller;
        this.manualDecisionEvents = manualDecisionEvents;
        this.dataRefreshSettings = dataRefreshSettings;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<PausedDataRefreshResolution> Resolve(ResolvePausedDataRefreshRequest? request)
    {
        if (request is not { DataRefreshRecordId: > 0, Decision: { } decision } || !Enum.IsDefined(decision))
        {
            return new PausedDataRefreshResolution(
                PausedDataRefreshResolutionOutcome.InvalidRequest,
                $"The body must have a {nameof(ResolvePausedDataRefreshRequest.DataRefreshRecordId)} above 0, and a " +
                $"{nameof(ResolvePausedDataRefreshRequest.Decision)}: one of " +
                $"{string.Join(", ", Enum.GetNames<DataRefreshManualDecision>())}.");
        }

        var recordId = request.DataRefreshRecordId;
        var record = await dataRefreshHistoryRepository.FindRecord(recordId);
        if (record == null)
        {
            return new PausedDataRefreshResolution(
                PausedDataRefreshResolutionOutcome.NotFound, $"There is no data refresh record {recordId}.", recordId, decision);
        }

        if (record.JustPrecompute && decision == DataRefreshManualDecision.Complete)
        {
            // ATL-491 experiment only: Complete would swap in a database with the precomputed data of a sample of the donors.
            return new PausedDataRefreshResolution(
                PausedDataRefreshResolutionOutcome.InvalidRequest,
                $"Data refresh record {recordId} is a JustPrecompute refresh, so it cannot be completed. Send " +
                $"{DataRefreshManualDecision.RetryFailedBatches} to run the stage again, or {DataRefreshManualDecision.Fail} to end it.",
                recordId,
                decision);
        }

        var decidedBy = CleanDecidedBy(request.DecidedBy);
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (!await dataRefreshHistoryRepository.TryTakeManualDecision(recordId, decision, nowUtc, SupportComment(decision, decidedBy, nowUtc)))
        {
            return await DescribeConflict(recordId, decision);
        }

        manualDecisionEvents.DecisionTaken(recordId, decision, decidedBy, record.AwaitingManualDecisionSinceUtc);
        logger.LogWarning(
            LoggingPrefix + " The manual decision {Decision} was taken for data refresh record {DataRefreshRecordId}, paused since " +
            "{PausedSinceUtc:u}. Decided by: {DecidedBy}.",
            decision, recordId, record.AwaitingManualDecisionSinceUtc, decidedBy);

        return decision == DataRefreshManualDecision.Fail
            ? await CloseAsFailed(record)
            : await RequestTheResumedRefresh(recordId, decision);
    }

    private async Task<PausedDataRefreshResolution> RequestTheResumedRefresh(int recordId, DataRefreshManualDecision decision)
    {
        try
        {
            await serviceBusClient.PublishToRequestTopic(new ValidatedDataRefreshRequest { DataRefreshRecordId = recordId });
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                LoggingPrefix + " The manual decision {Decision} for data refresh record {DataRefreshRecordId} is recorded, but the " +
                "refresh request could not be published. The watchdog requests the refresh later.",
                decision, recordId);
            return new PausedDataRefreshResolution(
                PausedDataRefreshResolutionOutcome.NotPublished,
                $"The decision {decision} for data refresh record {recordId} is recorded, but the refresh request could not be published. " +
                "Do not send the decision again. The watchdog requests the refresh again, within " +
                $"{dataRefreshSettings.WatchdogGraceDurationMinutes} minutes plus one watchdog interval (schedule " +
                $"'{dataRefreshSettings.WatchdogCronSchedule}').",
                recordId,
                decision);
        }

        var next = decision == DataRefreshManualDecision.Complete
            ? "The refresh resumes, marks the donor genotype precomputation stage as complete, and continues with the later stages."
            : "The refresh resumes, scales the database up, and computes the failed batches again. It can pause again.";
        return new PausedDataRefreshResolution(
            PausedDataRefreshResolutionOutcome.Accepted,
            $"The decision {decision} for data refresh record {recordId} is taken, and the refresh is requested. {next}",
            recordId,
            decision);
    }

    /// <summary>
    /// The record is closed already. Notifies, then cancels the run: the same order as for a refresh that fails, and both
    /// best effort, because the record holds the result.
    /// </summary>
    private async Task<PausedDataRefreshResolution> CloseAsFailed(DataRefreshRecord record)
    {
        try
        {
            await completionNotifier.NotifyOfFailure(record.Id);
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                LoggingPrefix + " Data refresh record {DataRefreshRecordId} is closed as failed, but its failure notifications could not " +
                "be sent. Consumers of the completion topic get no message for this job.",
                record.Id);
        }

        // The run is complete with failures, so this only removes the staging data. It does not throw.
        await precomputationRunCanceller.CancelRun(record);

        return new PausedDataRefreshResolution(
            PausedDataRefreshResolutionOutcome.Accepted,
            $"The decision {DataRefreshManualDecision.Fail} for data refresh record {record.Id} is taken. The record is closed as failed, " +
            "and there is no swap. A new data refresh can be requested.",
            record.Id,
            DataRefreshManualDecision.Fail);
    }

    private async Task<PausedDataRefreshResolution> DescribeConflict(int recordId, DataRefreshManualDecision decision)
    {
        var record = await dataRefreshHistoryRepository.FindRecord(recordId);
        var reason = record switch
        {
            null => "it does not exist any more.",
            { RefreshEndUtc: not null } => $"it is closed, as {(record.WasSuccessful == true ? "successful" : "failed")}, since {record.RefreshEndUtc:u}.",
            { PendingManualDecision: { } pending } => $"the decision {pending} is taken already, and the refresh applies it.",
            _ => "it is not paused. If a paused alert came for it, the refresh still scales the database down for the pause: try again later."
        };

        return new PausedDataRefreshResolution(
            PausedDataRefreshResolutionOutcome.Conflict,
            $"The decision {decision} was not taken for data refresh record {recordId}: {reason} Nothing changed.",
            recordId,
            decision);
    }

    private static string SupportComment(DataRefreshManualDecision decision, string? decidedBy, DateTime nowUtc)
    {
        var by = decidedBy == null ? "" : $" (by {decidedBy})";
        return $"{nowUtc.ToString("u", CultureInfo.InvariantCulture)} Manual decision: {decision}{by}.";
    }

    /// <returns>The text on one line, and not longer than <see cref="MaxDecidedByLength"/>; or null when it is empty.</returns>
    private static string? CleanDecidedBy(string? decidedBy)
    {
        if (decidedBy == null)
        {
            return null;
        }

        var oneLine = new string(decidedBy.Select(c => char.IsControl(c) ? ' ' : c).ToArray()).Trim();
        if (oneLine.Length == 0)
        {
            return null;
        }

        return oneLine.Length > MaxDecidedByLength ? oneLine[..MaxDecidedByLength] : oneLine;
    }
}
