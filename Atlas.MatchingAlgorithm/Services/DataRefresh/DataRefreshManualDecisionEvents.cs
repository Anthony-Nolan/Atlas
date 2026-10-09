#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Microsoft.ApplicationInsights;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh;

/// <summary>
/// The Application Insights events of a data refresh that pauses for a manual decision: the pause, the decision, and the
/// resume. Query: <c>customEvents | where name startswith "Data refresh"</c>.
/// </summary>
/// <remarks>
/// Events are sampled like other telemetry. The paused alert is the main signal; these events give a trail.
/// </remarks>
public interface IDataRefreshManualDecisionEvents
{
    /// <summary>The record is paused. "Paused again" when the run had a manual retry before.</summary>
    void Paused(int recordId, DonorGenotypePrecomputationFailureReport failureReport);

    /// <summary>A manual decision was taken for the paused record.</summary>
    void DecisionTaken(int recordId, DataRefreshManualDecision decision, string? decidedBy, DateTime? pausedSinceUtc);

    /// <summary>The refresh started to apply a pending manual decision.</summary>
    void Resumed(int recordId, DataRefreshManualDecision decision);
}

/// <inheritdoc />
public class DataRefreshManualDecisionEvents : IDataRefreshManualDecisionEvents
{
    internal const string PausedEventName = "Data refresh paused for a manual decision";
    internal const string PausedAgainEventName = "Data refresh paused again for a manual decision";
    internal const string DecisionTakenEventName = "Data refresh manual decision taken";
    internal const string ResumedEventName = "Data refresh resumed after a manual decision";

    private readonly TelemetryClient telemetryClient;

    public DataRefreshManualDecisionEvents(TelemetryClient telemetryClient)
    {
        this.telemetryClient = telemetryClient;
    }

    /// <inheritdoc />
    public void Paused(int recordId, DonorGenotypePrecomputationFailureReport failureReport)
    {
        var summary = failureReport.FailureSummary;
        var properties = new Dictionary<string, string>
        {
            { "DataRefreshRecordId", recordId.ToString(CultureInfo.InvariantCulture) },
            { "RunId", failureReport.RunId.ToString(CultureInfo.InvariantCulture) },
            { "FailedDonorCount", summary.FailedDonorCount.ToString(CultureInfo.InvariantCulture) },
            { "TotalDonorCount", summary.TotalDonorCount.ToString(CultureInfo.InvariantCulture) },
            { "FailedDonorFraction", summary.FailedDonorFraction.ToString(CultureInfo.InvariantCulture) },
            { "MaxFailedDonorFraction", failureReport.MaxFailedDonorFraction.ToString(CultureInfo.InvariantCulture) },
            { "PermanentlyFailedBatchCount", summary.PermanentlyFailedBatchCount.ToString(CultureInfo.InvariantCulture) },
            { "FailedGroupCount", summary.FailedGroupCount.ToString(CultureInfo.InvariantCulture) }
        };

        var isPausedAgain = failureReport.ManualRetryCount > 0;
        if (isPausedAgain)
        {
            properties.Add("ManualRetryCount", failureReport.ManualRetryCount.ToString(CultureInfo.InvariantCulture));
        }

        telemetryClient.TrackEvent(isPausedAgain ? PausedAgainEventName : PausedEventName, properties);
    }

    /// <inheritdoc />
    public void DecisionTaken(int recordId, DataRefreshManualDecision decision, string? decidedBy, DateTime? pausedSinceUtc)
    {
        telemetryClient.TrackEvent(DecisionTakenEventName, new Dictionary<string, string>
        {
            { "DataRefreshRecordId", recordId.ToString(CultureInfo.InvariantCulture) },
            { "Decision", decision.ToString() },
            { "DecidedBy", decidedBy ?? "" },
            { "PausedSinceUtc", pausedSinceUtc?.ToString("u", CultureInfo.InvariantCulture) ?? "" }
        });
    }

    /// <inheritdoc />
    public void Resumed(int recordId, DataRefreshManualDecision decision)
    {
        telemetryClient.TrackEvent(ResumedEventName, new Dictionary<string, string>
        {
            { "DataRefreshRecordId", recordId.ToString(CultureInfo.InvariantCulture) },
            { "Decision", decision.ToString() }
        });
    }
}
