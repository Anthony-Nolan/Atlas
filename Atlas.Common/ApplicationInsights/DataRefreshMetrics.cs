using System.Collections.Generic;

namespace Atlas.Common.ApplicationInsights;

/// <summary>
/// The Data Refresh telemetry contract: metric names, dimension keys, and the (deliberately low-cardinality) sets of
/// values each dimension may take.
///
/// Durations and counts are emitted as pre-aggregated Application Insights metrics (the <c>customMetrics</c> table)
/// which are never sampled, rather than as <c>TrackTrace</c> summaries which the isolated worker's adaptive sampling
/// dropped when they burst out at Data Refresh stage-50 completion.
///
/// <para>
/// This lives in Atlas.Common - rather than next to the refresh code - because more than one component emits into the
/// same metric names (Atlas.MatchingAlgorithm[.Data] for the stages themselves, Atlas.MultipleAlleleCodeDictionary for
/// the MAC lookups the refresh floods), and the <c>GetMetric</c> aggregator cache is keyed on
/// (metric name + ordered dimension names). A second definition of the same metric name with a different dimension-key
/// set would throw at runtime, so there is exactly one definition, here.
/// </para>
///
/// <para><b>Adding a metric name?</b> Give it its own strict <c>*Dims</c> helper that always supplies every key for
/// that metric. Do NOT reuse a helper belonging to a different metric name.</para>
///
/// <para><b>Adding a dimension value?</b> Keep it a fixed, small set. App Insights caps the number of data series per
/// metric (order-of-1000); a high-cardinality value (batch index, donor id, SQL text) silently stops new series being
/// tracked.</para>
///
/// <code>
/// customMetrics
/// | where name == "DataRefresh.DurationMs"
/// | summarize totalMs = sum(valueSum), calls = sum(valueCount), avgMs = sum(valueSum)/sum(valueCount)
///     by Operation = tostring(customDimensions.Operation), Locus = tostring(customDimensions.Locus)
/// </code>
/// </summary>
public static class DataRefreshMetrics
{
    /// <summary>Elapsed milliseconds of an <see cref="OperationDimension"/>. Dimensions: Operation + Locus.</summary>
    public const string DurationMsMetric = "DataRefresh.DurationMs";

    /// <summary>
    /// A count of things an <see cref="OperationDimension"/> produced or consumed. Dimensions: Operation + Locus.
    ///
    /// A duration tells you what something <i>cost</i>; a counter tells you what it <i>bought</i>. Both are needed to
    /// tell redundant work (fixable by doing less) from expensive work (fixable only by doing it faster).
    /// </summary>
    public const string CountMetric = "DataRefresh.Count";

    /// <summary>
    /// Elapsed milliseconds of one execution of one refresh stage. Dimension: Stage.
    ///
    /// Supersedes deriving stage durations by DATEDIFF-ing the DataRefreshHistory columns: those are last-write-wins,
    /// so they are only meaningful when the job ran exactly once. A metric per stage execution is correct at any
    /// attempt count, and gives the short stages (0/10/20/30/70/80) real numbers for the first time.
    /// </summary>
    public const string StageDurationMsMetric = "DataRefresh.StageDurationMs";

    /// <summary>
    /// A periodic sample of process-level utilisation while a refresh is running. Dimension: Counter.
    ///
    /// This is the headroom question: "stage 40 is 31% CPU / 69% DB" is un-actionable without knowing whether the
    /// worker is pegged. At 100% CPU, pipelining buys nothing and the answer is less work or more cores; at 25% it is
    /// free money.
    /// </summary>
    public const string RuntimeMetric = "DataRefresh.Runtime";

    public const string OperationDimension = "Operation";
    public const string LocusDimension = "Locus";
    public const string StageDimension = "Stage";
    public const string CounterDimension = "Counter";

    #region Operation values - durations

    public const string Operation_HlaProcessingStageTotal = "HlaProcessingStageTotal";
    public const string Operation_BatchProcessing = "BatchProcessing";
    public const string Operation_HlaExpansion = "HlaExpansion";
    public const string Operation_UpsertOverall = "UpsertOverall";
    public const string Operation_BulkInsertSetup = "BulkInsertSetup";
    public const string Operation_BuildDataTable = "BuildDataTable";
    public const string Operation_DeleteExistingRecords = "DeleteExistingRecords";
    public const string Operation_BlockingWaitOnDbInsert = "BlockingWaitOnDbInsert";
    public const string Operation_DbBulkInsert = "DbBulkInsert";

    // Stage 50 ImportHla operations. This is the HLA-name / p-group import path (HlaProcessor -> IHlaImportRepository.ImportHla)
    // that the spike profile (Phase B, Finding #1) identified as the single largest slice of stage-50 user-code (~55%),
    // yet which previously lived entirely UNMEASURED inside the BatchProcessing span. These break it into its cost centres so
    // the CPU-vs-DB question can be answered for the hotspot itself:
    //  - EnsurePGroupsExist / EnsureHlaNamesExist: the per-batch "insert new names then re-read the WHOLE table to refresh the
    //    in-memory id map" pattern (DB-read bound, ~quadratic in table size) — the actual Finding #1 anti-pattern.
    //  - EnsureProcessedHlaCache: the one-off (first-batch) full per-locus read of existing HlaNamePGroupRelation ids.
    //  - BuildHlaRelations: constructing the relations to insert (CPU + PhenotypeInfo/LociInfo allocations, Finding #3).
    //  - InsertHlaRelations: the SqlBulkCopy of the new relations into HlaNamePGroupRelation* (DB-write).
    //  - ImportHlaOverall: the whole slice, as a single number and a cross-check that the sub-ops sum to it.
    public const string Operation_ImportHlaOverall = "ImportHlaOverall";
    public const string Operation_EnsureProcessedHlaCache = "EnsureProcessedHlaCache";
    public const string Operation_EnsurePGroupsExist = "EnsurePGroupsExist";
    public const string Operation_EnsureHlaNamesExist = "EnsureHlaNamesExist";
    public const string Operation_BuildHlaRelations = "BuildHlaRelations";
    public const string Operation_InsertHlaRelations = "InsertHlaRelations";

    // Stage 50 upfront setup. Previously the only stage-50 setup numbers, and they were sampleable Traces.
    public const string Operation_HmdPreWarm = "HmdPreWarm";
    public const string Operation_UpfrontPGroupInsert = "UpfrontPGroupInsert";

    // Stage 40 (DonorImport) operations. Added after the A1 stage-ranking showed DonorImport is a co-largest
    // stage (on par with DonorHlaProcessing), yet was previously only timed by a single sampled Verbose Trace.
    // Splitting it into conversion (CPU) vs the two SQL writes (DB) answers the same "our loop or the DB?"
    // question for stage 40 that the stage-50 operations answer for stage 50.
    public const string Operation_DonorImportStageTotal = "DonorImportStageTotal";
    public const string Operation_DonorImportBatch = "DonorImportBatch";
    public const string Operation_DonorInfoConversion = "DonorInfoConversion";
    public const string Operation_DonorBulkInsert = "DonorBulkInsert";
    public const string Operation_DonorManagementLogWrite = "DonorManagementLogWrite";

    /// <summary>
    /// The cross-DB donor stream read. Previously the largest unmeasured slice of the whole job: it was only
    /// recoverable as DonorImportStageTotal minus the DonorImportBatch spans, i.e. as an unattributed residual.
    /// </summary>
    public const string Operation_DonorStreamRead = "DonorStreamRead";

    // The two halves of DonorInfoConversion: per-donor FluentValidation, then the field-copy mapping.
    public const string Operation_DonorValidation = "DonorValidation";
    public const string Operation_DonorMapping = "DonorMapping";

    // The two halves of DonorManagementLogWrite: the existing-logs read (a no-op on a refresh, since stage 20
    // TRUNCATEs the table) then the actual insert.
    public const string Operation_DonorManagementLogRead = "DonorManagementLogRead";
    public const string Operation_DonorManagementLogInsert = "DonorManagementLogInsert";

    /// <summary>
    /// A single MAC point-lookup against Table Storage (cache MISS path only - hits are free and are not timed).
    /// SQL/Table dependency auto-collection is inactive in the isolated worker, so this is the only way to see it.
    /// </summary>
    public const string Operation_MacLookup = "MacLookup";

    #endregion

    #region Operation values - counts

    /// <summary>
    /// Candidate (donor, locus, position, p-group) tuples the relation build actually walked, per batch - i.e. what
    /// the build COST, as opposed to <see cref="Operation_HlaRelationsBuilt"/>, which is what it PRODUCED.
    ///
    /// <para>
    /// The distinction is the whole point. <c>HlaRelationsBuilt</c> counts the build's output, which is already
    /// filtered (against the processed-HLA cache) and de-duplicated, so it can only ever be close to
    /// <see cref="Operation_HlaRelationsInserted"/> and therefore cannot show waste even when the waste is total.
    /// This counter is incremented before either of those steps, so <c>Examined / Built</c> is the waste factor
    /// directly, with no correlation argument and - unlike a per-batch correlation - it survives App Insights'
    /// one-minute pre-aggregation.
    /// </para>
    /// </summary>
    public const string Operation_HlaRelationCandidatesExamined = "HlaRelationCandidatesExamined";

    /// <summary>Relations constructed by the per-donor build, per batch. Ratio against Inserted is the waste factor.</summary>
    public const string Operation_HlaRelationsBuilt = "HlaRelationsBuilt";

    /// <summary>Relations actually bulk-copied, per batch. Expected to collapse to ~0 after the first few hundred batches.</summary>
    public const string Operation_HlaRelationsInserted = "HlaRelationsInserted";

    /// <summary>New HLA names actually inserted, per batch. Emitted EVERY batch (including zero) - the zeros are the finding.</summary>
    public const string Operation_NewHlaNamesPerBatch = "NewHlaNamesPerBatch";

    /// <summary>New p-groups actually inserted, per batch. Emitted every batch, including zero.</summary>
    public const string Operation_NewPGroupsPerBatch = "NewPGroupsPerBatch";

    /// <summary>Rows returned by a full-table re-cache of HlaNames. Tests the "cost grows ~quadratically" claim directly.</summary>
    public const string Operation_HlaNamesTableRows = "HlaNamesTableRows";

    /// <summary>Rows returned by a full-table re-cache of PGroupNames.</summary>
    public const string Operation_PGroupTableRows = "PGroupTableRows";

    /// <summary>Rows bulk-copied into MatchingHlaAt&lt;Locus&gt;. Normalises DbBulkInsert ms into ms per million rows.</summary>
    public const string Operation_MatchingHlaRowsWritten = "MatchingHlaRowsWritten";

    /// <summary>Characters of SQL text in the non-parameterised donor-management-log IN clause, per call.</summary>
    public const string Operation_ManagementLogSqlTextLength = "ManagementLogSqlTextLength";

    /// <summary>MAC cache misses - i.e. distinct MACs touched, which is the size of the decode flood.</summary>
    public const string Operation_MacCacheMisses = "MacCacheMisses";

    // Batch-size sanity counters. Named per stage rather than sharing one Operation value, so a short final batch in
    // one loop cannot skew the other loop's per-batch distribution.
    public const string Operation_DonorsPerImportBatch = "DonorsPerImportBatch";
    public const string Operation_FailedDonorsPerImportBatch = "FailedDonorsPerImportBatch";
    public const string Operation_DonorsPerHlaBatch = "DonorsPerHlaBatch";
    public const string Operation_FailedDonorsPerHlaBatch = "FailedDonorsPerHlaBatch";

    #endregion

    #region Counter values - for RuntimeMetric

    /// <summary>Process CPU as a percentage of all available cores, averaged over the sampling interval.</summary>
    public const string Counter_CpuPercent = "CpuPercent";

    public const string Counter_WorkingSetMb = "WorkingSetMb";
    public const string Counter_ThreadPoolQueueLength = "ThreadPoolQueueLength";
    public const string Counter_ThreadPoolThreadCount = "ThreadPoolThreadCount";

    /// <summary>Gen2 collections that happened during the sampling interval (a delta, not the running total).</summary>
    public const string Counter_Gen2Collections = "Gen2Collections";

    /// <summary>Percentage of the sampling interval spent in GC pauses.</summary>
    public const string Counter_GcPauseTimePercent = "GcPauseTimePercent";

    #endregion

    /// <summary>Locus dimension value used when a measurement is not scoped to a single locus.</summary>
    public const string Locus_All = "all";

    /// <summary>
    /// Builds the dimension set for <see cref="DurationMsMetric"/> and <see cref="CountMetric"/> - which share a
    /// dimension-key set by design, so a single query can join a cost to what it bought. Always supplies BOTH keys
    /// (with Locus defaulting to <see cref="Locus_All"/>) so every call for a given metric name uses an identical
    /// dimension-key set - a requirement of the underlying <c>GetMetric</c> aggregator.
    /// </summary>
    public static Dictionary<string, string> Dims(string operation, string locus = Locus_All) =>
        new Dictionary<string, string>
        {
            { OperationDimension, operation },
            { LocusDimension, locus }
        };

    /// <summary>
    /// Builds the dimension set for <see cref="StageDurationMsMetric"/>. Deliberately NOT <see cref="Dims"/>: a metric
    /// name must be called with the same dimension KEYS every time, and this metric's key set is Stage alone.
    /// </summary>
    public static Dictionary<string, string> StageDims(string stage) =>
        new Dictionary<string, string>
        {
            { StageDimension, stage }
        };

    /// <summary>
    /// Builds the dimension set for <see cref="RuntimeMetric"/>. Its key set is Counter alone - see
    /// <see cref="StageDims"/> for why this cannot share a helper with the others.
    /// </summary>
    public static Dictionary<string, string> RuntimeDims(string counter) =>
        new Dictionary<string, string>
        {
            { CounterDimension, counter }
        };
}
