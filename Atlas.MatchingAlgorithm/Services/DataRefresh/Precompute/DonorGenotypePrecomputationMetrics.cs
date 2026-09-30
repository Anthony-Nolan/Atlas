using System;
using Microsoft.ApplicationInsights;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;

/// <summary>
/// The measurements of the donor genotype precomputation workers, one set per batch message.
/// </summary>
/// <remarks>
/// Pre-aggregated Application Insights metrics, not events or traces: the telemetry of a worker is sampled, and a metric
/// is not. Query: <c>customMetrics | where name startswith "DonorGenotypePrecomputation."</c>, split by
/// <c>customDimensions.Result</c>.
/// </remarks>
public interface IDonorGenotypePrecomputationMetrics
{
    /// <param name="result">What happened to the batch.</param>
    /// <param name="duration">From the claim to the last write.</param>
    /// <param name="computedGroupCount">The groups that the batch had to compute: the groups with no value yet.</param>
    /// <param name="frequencySetCount">
    /// The distinct haplotype frequency sets of those groups. Near 1 when the workers are affine to frequency sets, which
    /// is what the order of the groups is for.
    /// </param>
    void RecordBatch(DonorGenotypePrecomputationBatchResult result, TimeSpan duration, int computedGroupCount, int frequencySetCount);
}

public class DonorGenotypePrecomputationMetrics : IDonorGenotypePrecomputationMetrics
{
    internal const string BatchDurationMetricName = "DonorGenotypePrecomputation.BatchDurationMs";
    internal const string ComputedGroupCountMetricName = "DonorGenotypePrecomputation.BatchComputedGroupCount";
    internal const string FrequencySetCountMetricName = "DonorGenotypePrecomputation.BatchFrequencySetCount";

    /// <summary>The one dimension of every metric here. A metric id must always be used with the same dimensions.</summary>
    private const string ResultDimension = "Result";

    private readonly TelemetryClient telemetryClient;

    public DonorGenotypePrecomputationMetrics(TelemetryClient telemetryClient)
    {
        this.telemetryClient = telemetryClient;
    }

    /// <inheritdoc />
    public void RecordBatch(DonorGenotypePrecomputationBatchResult result, TimeSpan duration, int computedGroupCount, int frequencySetCount)
    {
        var resultName = result.ToString();

        telemetryClient.GetMetric(BatchDurationMetricName, ResultDimension).TrackValue(duration.TotalMilliseconds, resultName);
        telemetryClient.GetMetric(ComputedGroupCountMetricName, ResultDimension).TrackValue(computedGroupCount, resultName);
        telemetryClient.GetMetric(FrequencySetCountMetricName, ResultDimension).TrackValue(frequencySetCount, resultName);
    }
}
