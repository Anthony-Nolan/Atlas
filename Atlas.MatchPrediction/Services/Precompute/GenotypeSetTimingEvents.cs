using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.MatchProbability;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;

namespace Atlas.MatchPrediction.Services.Precompute;

/// <summary>One donor input's part in a batch's timing event.</summary>
public sealed record DonorGenotypeSetTimingSample(DonorGenotypeSetSource Source, int DonorGenotypeCount, DonorGenotypeSetTiming Timing);

/// <summary>Everything a batch's timing event reports (ATL-474).</summary>
/// <param name="MatchPredictionPath">"Durable" (one donor at a time) or "Parallel" (the match prediction worker).</param>
/// <param name="MaxDegreeOfParallelism">How many donors ran at once; per-donor times grow with it.</param>
/// <param name="BatchId">The parallel batch id; null on the durable path.</param>
/// <param name="PatientTime">Getting the patient genotype set: stored or live.</param>
/// <param name="CompleteTime">Storing live sets for reuse and sending the usage event.</param>
/// <param name="BatchTime">The whole batch, from the read of the stored rows to the end of <paramref name="CompleteTime"/>.</param>
/// <param name="WarmUpTime">Warming the MAC and HLA Metadata Dictionary caches (ATL-476); zero when the batch skipped it.</param>
public sealed record GenotypeSetBatchTimings(
    IdentifiedMatchProbabilityRequest Request,
    DonorGenotypeSetBatchContext BatchContext,
    string MatchPredictionPath,
    int MaxDegreeOfParallelism,
    int? BatchId,
    PatientGenotypeSetSource PatientGenotypeSetSource,
    int PatientGenotypeCount,
    TimeSpan PatientTime,
    IReadOnlyCollection<DonorGenotypeSetTimingSample> Donors,
    TimeSpan CompleteTime,
    TimeSpan BatchTime,
    TimeSpan WarmUpTime = default);

public interface IGenotypeSetTimingEvents
{
    /// <summary>
    /// Sends the batch's one <see cref="GenotypeSetTimingEvents.EventName"/> event, so a search's live and precomputed
    /// genotype set times can be compared (ATL-474). Never throws.
    /// </summary>
    void Send(GenotypeSetBatchTimings timings);
}

/// <remarks>
/// <para>
/// Sent with the <see cref="TelemetryClient"/>, not <c>IAtlasLogger</c>, so the event can opt out of sampling: the worker's
/// Application Insights SDK samples events adaptively, whatever host.json says. An item whose sampling percentage is
/// already set is never sampled again, so every batch's event arrives and a search's totals add up.
/// </para>
///
/// <para>
/// One event per batch, not per donor, to keep the volume low. The donor times are summed per source and per
/// genotype count bucket; a mean per donor is the sum divided by the count. Query:
/// <c>customEvents | where name == "Genotype set timings"</c>.
/// </para>
/// </remarks>
internal class GenotypeSetTimingEvents : IGenotypeSetTimingEvents
{
    internal const string EventName = "Genotype set timings";

    /// <summary>Upper bounds (inclusive) of the genotype count buckets; the last bucket has no upper bound.</summary>
    private static readonly (int Max, string Name)[] GenotypeCountBuckets =
    [
        (1, "G0to1"),
        (10, "G2to10"),
        (100, "G11to100"),
        (500, "G101to500"),
        (1000, "G501to1000"),
        (int.MaxValue, "G1001Plus"),
    ];

    private readonly TelemetryClient telemetryClient;

    public GenotypeSetTimingEvents(TelemetryClient telemetryClient)
    {
        this.telemetryClient = telemetryClient;
    }

    /// <inheritdoc />
    public void Send(GenotypeSetBatchTimings timings)
    {
        try
        {
            var telemetry = new EventTelemetry(EventName);
            foreach (var (key, value) in Properties(timings))
            {
                telemetry.Properties[key] = value;
            }

            foreach (var (key, value) in Metrics(timings))
            {
                telemetry.Metrics[key] = value;
            }

            ((ISupportSampling)telemetry).SamplingPercentage = 100;
            telemetryClient.TrackEvent(telemetry);
        }
        catch
        {
            // Logging must not fail the batch.
        }
    }

    internal static Dictionary<string, string> Properties(GenotypeSetBatchTimings timings)
    {
        var context = timings.BatchContext;
        return new Dictionary<string, string>
        {
            { "SearchRequestId", timings.Request.SearchRequestId },
            { "MatchPredictionPath", timings.MatchPredictionPath },
            { "BatchId", timings.BatchId?.ToString(CultureInfo.InvariantCulture) ?? "" },
            // The live path never looks up the allowed-loci key, so the excluded loci say which one the search used.
            { "ExcludedLoci", string.Join(",", (timings.Request.ExcludedLoci ?? []).Order()) },
            { "AllowedLociKey", context.AllowedLociKey ?? "" },
            { "UsePrecomputedGenotypeSets", context.UsePrecomputedGenotypeSets.ToString() },
            { "UsePrecomputedGenotypeSetsSource", context.UsePrecomputedGenotypeSetsSource.ToString() },
            { "PrecomputeMode", context.Mode.ToString() },
            { "BatchFallbackReason", context.BatchFallbackReason?.ToString() ?? "" },
            { "MatchingAlgorithmDataRefreshRecordId", context.MatchingAlgorithmDataRefreshRecordId?.ToString(CultureInfo.InvariantCulture) ?? "" },
            { "PatientGenotypeSetSource", timings.PatientGenotypeSetSource.ToString() },
        };
    }

    internal static Dictionary<string, double> Metrics(GenotypeSetBatchTimings timings)
    {
        var context = timings.BatchContext;
        var timedDonors = timings.Donors.Where(d => d.Timing != null).ToList();
        var live = timedDonors.Where(d => d.Timing.LiveComputeTime != null).ToList();
        var precomputed = timedDonors.Where(d => d.Source == DonorGenotypeSetSource.Precomputed).ToList();
        var encoded = timedDonors.Where(d => d.Timing.EncodeTime > TimeSpan.Zero).ToList();

        var metrics = new Dictionary<string, double>
        {
            { "MaxDegreeOfParallelism", timings.MaxDegreeOfParallelism },
            { "DonorCount", timings.Donors.Count },
            { "BatchMs", timings.BatchTime.TotalMilliseconds },
            { "WarmUpMs", timings.WarmUpTime.TotalMilliseconds },
            { "PatientMs", timings.PatientTime.TotalMilliseconds },
            { "PatientGenotypeCount", timings.PatientGenotypeCount },
            { "ReadMs", context.ReadTime?.TotalMilliseconds ?? 0 },
            { "ReadRowCount", context.ReadRowCount },
            { "ReadBytes", context.ReadBytes },
            { "CompleteMs", timings.CompleteTime.TotalMilliseconds },
            { "DonorTotalMs", timedDonors.Sum(d => d.Timing.TotalTime.TotalMilliseconds) },
            { "LookupMs", timedDonors.Sum(d => d.Timing.LookupTime.TotalMilliseconds) },
            { "LiveCount", live.Count },
            { "LiveMs", live.Sum(d => d.Timing.LiveComputeTime!.Value.TotalMilliseconds) },
            { "LiveGenotypes", live.Sum(d => d.DonorGenotypeCount) },
            { "PrecomputedCount", precomputed.Count },
            { "PrecomputedMs", precomputed.Sum(d => d.Timing.LookupTime.TotalMilliseconds) },
            { "PrecomputedGenotypes", precomputed.Sum(d => d.DonorGenotypeCount) },
            { "PrecomputedBytes", precomputed.Sum(d => d.Timing.StoredBytes ?? 0) },
            { "EncodeCount", encoded.Count },
            { "EncodeMs", encoded.Sum(d => d.Timing.EncodeTime.TotalMilliseconds) },
        };

        foreach (var (_, name) in GenotypeCountBuckets)
        {
            var liveInBucket = live.Where(d => BucketName(d.DonorGenotypeCount) == name).ToList();
            var precomputedInBucket = precomputed.Where(d => BucketName(d.DonorGenotypeCount) == name).ToList();
            metrics[$"Live{name}Count"] = liveInBucket.Count;
            metrics[$"Live{name}Ms"] = liveInBucket.Sum(d => d.Timing.LiveComputeTime!.Value.TotalMilliseconds);
            metrics[$"Precomputed{name}Count"] = precomputedInBucket.Count;
            metrics[$"Precomputed{name}Ms"] = precomputedInBucket.Sum(d => d.Timing.LookupTime.TotalMilliseconds);
            metrics[$"Precomputed{name}Bytes"] = precomputedInBucket.Sum(d => d.Timing.StoredBytes ?? 0);
        }

        // Where the live imputation time went, summed over the batch's live donors (ATL-474). The stage sums add up to a
        // little less than LiveMs: the gap is the validation and set-up between stages.
        var staged = live.Where(d => d.Timing.LiveImputationStages != null).Select(d => d.Timing.LiveImputationStages).ToList();
        metrics["StagedLiveCount"] = staged.Count;
        metrics["Survivors"] = staged.Sum(s => s.Survivors);
        metrics["ConsolidationFallbacks"] = staged.Sum(s => s.ConsolidationFallbacks);
        foreach (var stage in Enum.GetValues<LiveImputationStage>())
        {
            metrics[$"Stage{stage}Ms"] = staged.Sum(s => s.Get(stage).TotalMilliseconds);
        }

        return metrics;
    }

    internal static string BucketName(int genotypeCount) => GenotypeCountBuckets.First(b => genotypeCount <= b.Max).Name;
}
