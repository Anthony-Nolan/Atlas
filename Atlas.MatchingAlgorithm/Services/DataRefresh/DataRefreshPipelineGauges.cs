using System.Threading;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh;

/// <summary>
/// The queue state of the data refresh's two prefetch pipelines: written by the side that consumes each queue, and
/// read by <see cref="DataRefreshRuntimeSampler"/> on its own interval.
/// </summary>
/// <remarks>
/// This exists because occupancy - summed leaf durations over a stage's wall clock - proves that a stage overlapped,
/// but cannot say WHERE a pipeline stalled, and the two failure modes look identical from the outside. See the counter
/// definitions in <c>DataRefreshMetrics</c> for how a depth distribution answers that, and for what each shape means.
///
/// <para>
/// Registered scoped rather than singleton, so that a depth left behind by one refresh cannot be sampled into the
/// next one's series. The two gauges are separate objects because the two stages run at different times and their
/// depths are read against different channel bounds.
/// </para>
/// </remarks>
public interface IDataRefreshPipelineGauges
{
    /// <summary>The stage-40 donor-import pipeline's queue, between the master-store read and the matching-DB write.</summary>
    PipelineQueueGauge DonorImport { get; }

    /// <summary>The stage-50 HLA-processing pipeline's queue, between the paged donor read and the HLA expansion.</summary>
    PipelineQueueGauge HlaBatchPrefetch { get; }
}

/// <inheritdoc cref="IDataRefreshPipelineGauges"/>
public sealed class DataRefreshPipelineGauges : IDataRefreshPipelineGauges
{
    public PipelineQueueGauge DonorImport { get; } = new();
    public PipelineQueueGauge HlaBatchPrefetch { get; } = new();
}

/// <summary>
/// One prefetch queue's depth, plus how often its consumer found it empty.
/// </summary>
/// <remarks>
/// Deliberately free of any telemetry call. At ~4,400 stage-40 and ~22,000 stage-50 batches, emitting a metric per
/// batch would be tens of thousands of sends to describe a queue that moves slowly; the consumer writes a field and
/// the sampler reads it on the interval it already ticks on.
/// </remarks>
public sealed class PipelineQueueGauge
{
    private int depth;
    private long starvations;

    /// <summary>
    /// Records how much work was queued when the consumer took its last item, counting a starvation when there was
    /// none behind it - which is the consumer about to wait on the reader.
    /// </summary>
    public void RecordDepthOnTake(int queuedItems)
    {
        Volatile.Write(ref depth, queuedItems);

        if (queuedItems == 0)
        {
            Interlocked.Increment(ref starvations);
        }
    }

    /// <summary>The depth observed by the most recent take, or 0 before the stage has taken anything.</summary>
    public int Depth => Volatile.Read(ref depth);

    /// <summary>
    /// Starvations since this was last called, and resets the count. A delta rather than a running total, matching the
    /// rest of the runtime sampler: a total renders as a meaningless ramp, and the question here is a rate.
    /// </summary>
    public long ReadStarvationsSinceLastRead() => Interlocked.Exchange(ref starvations, 0);
}
