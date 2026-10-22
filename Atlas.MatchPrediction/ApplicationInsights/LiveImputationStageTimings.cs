using System;
using System.Diagnostics;
using System.Threading;

namespace Atlas.MatchPrediction.ApplicationInsights;

/// <summary>The stages of one donor's live imputation that <see cref="LiveImputationStageTimings"/> times (ATL-474).</summary>
/// <remarks>The names are logged as metric names (<c>Stage{Name}Ms</c>), so renaming one changes what App Insights queries see.</remarks>
public enum LiveImputationStage
{
    /// <summary>Converting the donor's typing to small g groups (and G or P groups, if the set holds them): HLA Metadata Dictionary and MAC lookups.</summary>
    PhenotypeConversion,

    /// <summary>Getting the frequency set's haplotype pool. Near zero when the set is cached; a SQL load of the whole set when it is not.</summary>
    FrequencySetFetch,

    /// <summary>Filtering the pool to the haplotypes the typing allows (the survivors).</summary>
    SurvivorFiltering,

    /// <summary>One frequency per survivor. Slow when the set's consolidated frequencies are still being built (see <see cref="LiveImputationStageTimings.ConsolidationFallbacks"/>).</summary>
    FrequencyResolution,

    /// <summary>Pairing the survivors into genotypes, with their likelihoods.</summary>
    Pairing,

    /// <summary>Keeping the most likely genotypes.</summary>
    Truncation,

    /// <summary>Converting the kept genotypes to P groups for the match calculation: HLA Metadata Dictionary lookups.</summary>
    PGroupConversion,
}

/// <summary>
/// Where one donor's live imputation spent its time, by <see cref="LiveImputationStage"/> (ATL-474: to find why live
/// imputation on Azure takes about 1 s per donor while the worker's CPU is mostly idle).
/// </summary>
/// <remarks>
/// <para>
/// Collected through an <see cref="AsyncLocal{T}"/> rather than passed down, because the stages sit several calls deep
/// in services shared with the patient, the precompute worker and the standalone path. <see cref="Begin"/> starts a
/// collection for the current async flow; <see cref="Time"/> and <see cref="CountConsolidationFallback"/> add to it, and
/// do nothing (at the cost of a timestamp) when no collection is running - for example for the patient.
/// </para>
/// <para>
/// A stage's time is wall-clock time, so it includes any wait for a thread, a lock or the garbage collector.
/// </para>
/// </remarks>
public sealed class LiveImputationStageTimings
{
    private static readonly int StageCount = Enum.GetValues<LiveImputationStage>().Length;
    private static readonly AsyncLocal<LiveImputationStageTimings> Current = new();

    private readonly long[] ticks = new long[StageCount];
    private int consolidationFallbacks;
    private long survivors;

    /// <summary>
    /// How many survivor frequencies were calculated by a direct scan of the whole set, because the set's consolidated
    /// frequencies were still being built.
    /// </summary>
    public int ConsolidationFallbacks => Volatile.Read(ref consolidationFallbacks);

    /// <summary>How many haplotypes survived filtering, summed over the donor's expansions.</summary>
    public long Survivors => Interlocked.Read(ref survivors);

    public TimeSpan Get(LiveImputationStage stage) => TimeSpan.FromTicks(Interlocked.Read(ref ticks[(int)stage]));

    /// <summary>
    /// Starts collecting for the current async flow; the returned value stops collecting when disposed. Call from the
    /// method that awaits the imputation, so the collection flows into every call it makes.
    /// </summary>
    public static Collection Begin()
    {
        var previous = Current.Value;
        var timings = new LiveImputationStageTimings();
        Current.Value = timings;
        return new Collection(timings, previous);
    }

    /// <summary>Times <paramref name="stage"/> until the returned value is disposed, if a collection is running.</summary>
    public static StageTimer Time(LiveImputationStage stage) => new(Current.Value, stage);

    /// <inheritdoc cref="ConsolidationFallbacks"/>
    public static void CountConsolidationFallback()
    {
        var timings = Current.Value;
        if (timings != null)
        {
            Interlocked.Increment(ref timings.consolidationFallbacks);
        }
    }

    /// <inheritdoc cref="Survivors"/>
    public static void CountSurvivors(int count)
    {
        var timings = Current.Value;
        if (timings != null)
        {
            Interlocked.Add(ref timings.survivors, count);
        }
    }

    private void Add(LiveImputationStage stage, long elapsedTicks) => Interlocked.Add(ref ticks[(int)stage], elapsedTicks);

    /// <summary>A running collection. Dispose to stop collecting; <see cref="Timings"/> stays readable.</summary>
    public readonly struct Collection : IDisposable
    {
        private readonly LiveImputationStageTimings previous;

        internal Collection(LiveImputationStageTimings timings, LiveImputationStageTimings previous)
        {
            Timings = timings;
            this.previous = previous;
        }

        public LiveImputationStageTimings Timings { get; }

        public void Dispose() => Current.Value = previous;
    }

    /// <summary>Times one stage. A no-op when no collection was running at its start.</summary>
    public readonly struct StageTimer : IDisposable
    {
        private readonly LiveImputationStageTimings timings;
        private readonly LiveImputationStage stage;
        private readonly long startTimestamp;

        internal StageTimer(LiveImputationStageTimings timings, LiveImputationStage stage)
        {
            this.timings = timings;
            this.stage = stage;
            startTimestamp = timings == null ? 0 : Stopwatch.GetTimestamp();
        }

        public void Dispose() => timings?.Add(stage, Stopwatch.GetElapsedTime(startTimestamp).Ticks);
    }
}
