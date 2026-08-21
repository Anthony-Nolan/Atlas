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

    /// <summary>
    /// The paged donor read that feeds stage 50's batch loop - i.e. the stage-50 counterpart of
    /// <see cref="Operation_DonorStreamRead"/>. The cost lands on the async enumerator's <c>MoveNextAsync</c>, which is
    /// outside the <see cref="Operation_BatchProcessing"/> span, so before this it was recoverable only as the residual
    /// between <see cref="Operation_HlaProcessingStageTotal"/> and the sum of its measured children.
    /// </summary>
    public const string Operation_HlaDonorBatchRead = "HlaDonorBatchRead";

    // The two halves of HlaDonorBatchRead, measured inside the repository rather than at the consumer.
    // Record 25 could only bound this split by cross-referencing Query Store, which put the paging SELECT at 76.4 min
    // of the read's ~93 - leaving ~17 min of per-donor client work that no in-process measurement could see. These
    // separate it directly, and they nest under HlaDonorBatchRead so the two form their own reconciliation.

    /// <summary>
    /// The paged donor query. Dapper buffers by default, so this covers the round trip AND Dapper's materialisation of
    /// the <c>Donor</c> entities - the two are not separable at this boundary without going to a raw reader. It also
    /// covers the connection open, which Dapper performs on a closed connection.
    /// </summary>
    public const string Operation_HlaDonorBatchQuery = "HlaDonorBatchQuery";

    /// <summary>
    /// Our own per-donor projection of the queried entities to <c>DonorInfo</c>. Pure client-side CPU, ~2,000 donors
    /// per call, and the half that Query Store can never see.
    /// </summary>
    public const string Operation_HlaDonorBatchMapping = "HlaDonorBatchMapping";

    // The interior of BulkInsertSetup. That span measures the whole synchronous prologue of the per-locus upsert (it
    // wraps the *task-creating call*, so it ends at the method's first true await), and BuildDataTable +
    // DeleteExistingRecords accounted for only a small part of it. These three complete the decomposition:
    //   BulkInsertSetup = BuildDataTable + TransactionScopeSetup + DeleteExistingRecords + BuildSqlBulkCopy + BulkCopySyncPrologue
    // Which of them holds the time decides which fix is the right one, so none of them presumes an attribution.
    // BuildSqlBulkCopy / BulkCopySyncPrologue are also emitted (with Locus = all) by the stage-40 Donors insert, which
    // shares the same helper.

    /// <summary>Constructing the ambient <c>AsyncTransactionScope</c> that wraps one per-locus write.</summary>
    public const string Operation_TransactionScopeSetup = "TransactionScopeSetup";

    /// <summary>Constructing the <c>SqlBulkCopy</c> and its column mappings. No connection is opened here.</summary>
    public const string Operation_BuildSqlBulkCopy = "BuildSqlBulkCopy";

    /// <summary>
    /// The portion of <c>SqlBulkCopy.WriteToServerAsync</c> that runs synchronously on the calling thread before its
    /// first true await - connection open, enlistment in the ambient transaction, and the bulk-load metadata exchange.
    /// Deliberately named for the mechanism (a synchronous prologue) rather than for its suspected contents, which are
    /// exactly what this measurement exists to establish.
    /// </summary>
    public const string Operation_BulkCopySyncPrologue = "BulkCopySyncPrologue";

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
    public const string Operation_MacPreWarm = "MacPreWarm";
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
    /// Relations actually bulk-copied, per batch. Expected to collapse to ~0 after the first few hundred batches.
    ///
    /// <para>
    /// This used to be one of three counters here. The other two - <c>HlaRelationCandidatesExamined</c> (tuples the
    /// build walked) and <c>HlaRelationsBuilt</c> (what it produced) - existed to expose the waste factor of the old
    /// relation build, which walked the batch once per locus over all twelve locus/position pairs to insert five loci.
    /// ATL-280 replaced that with a single pass over the matching loci only, so there is no waste ratio left to watch,
    /// and what is built is exactly what is inserted.
    /// </para>
    /// </summary>
    public const string Operation_HlaRelationsInserted = "HlaRelationsInserted";

    /// <summary>
    /// Relation bulk copies NOT issued because the batch produced no new relations for that locus, per batch.
    /// </summary>
    /// <remarks>
    /// The acceptance test for the "skip the empty relation insert" ticket, and it needs its own counter because the
    /// thing being verified is an ABSENCE. Record 29: <see cref="Operation_InsertHlaRelations"/> cost 38.1 min over
    /// 21,945 calls, flat at ~104 ms/batch, while relations discovered per batch collapsed 9,972 -> 455 over the run
    /// and the same inserts cost the SERVER 1.92 min in total. A 20:1 client-to-server ratio is per-call overhead, so
    /// the calls that produce nothing are pure waste - but "we issued fewer bulk copies" is invisible in a duration
    /// and only partly visible in Query Store (which under AUTO capture mode may simply not have recorded the calls
    /// that stopped happening). Emit it every batch, zeros included, for the same reason
    /// <see cref="Operation_NewPGroupsPerBatch"/> does: a counter that stops being emitted cannot be distinguished
    /// from a fix that stopped working.
    /// </remarks>
    public const string Operation_RelationInsertsSkipped = "RelationInsertsSkipped";

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

    /// <summary>
    /// MACs loaded into the in-memory store by one pre-warm. The denominator <see cref="Operation_MacPreWarm"/> needs:
    /// a duration without a count cannot distinguish a slow load from a large one, and those imply different fixes.
    ///
    /// <para>
    /// It also bounds the memory question. The store is a process-wide singleton with no expiry, in a host that also
    /// serves search, so whatever it holds is resident for the life of the process - on top of the 3,002 MB the
    /// stage-50 batch loop already peaked at in a 14 GB plan. MACs held x per-entry cost is that footprint.
    /// </para>
    /// </summary>
    public const string Operation_MacsPreWarmed = "MacsPreWarmed";

    // The two transaction-mechanism probes. Timers say WHICH statement in the bulk-insert prologue is slow; these say
    // WHY, and they are the difference between choosing the connection-reuse fix on evidence and choosing it on a
    // plausible story. Both are emitted on every per-locus write, INCLUDING the zeros - a counter that is only emitted
    // when it fires cannot distinguish "never happened" from "never measured".

    /// <summary>
    /// 1 when a transaction was already ambient as a per-locus write began, 0 otherwise. Each write opens its own
    /// <c>TransactionScope</c> with the default <c>Required</c>, so an ambient transaction means it JOINS rather than
    /// starts one - putting several bulk-copy connections in one transaction, which promotes it. On the refresh path
    /// the outer scope is a no-op, so any 1 here is a leak between sibling loci; on the fully-transactional path it is
    /// 1 by design and says nothing.
    /// </summary>
    public const string Operation_AmbientTransactionOnEntry = "AmbientTransactionOnEntry";

    /// <summary>
    /// 1 when the ambient transaction had promoted to a distributed transaction by the time the write completed, 0
    /// otherwise. Promotion happens on the SECOND enlistment, so this can only be read after the write, not before it.
    /// </summary>
    public const string Operation_DistributedTransactionPromotions = "DistributedTransactionPromotions";

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

    // Microsoft.Data.SqlClient's own EventCounters, forwarded by the runtime sampler.
    //
    // These exist for one question. The per-locus bulk-insert prologue costs ~40 ms per call, and that has been read
    // as connection establishment - but a POOLED open costs microseconds. 40 ms is the shape of a PHYSICAL connect, or
    // of a transaction enlistment. HardConnects vs SoftConnects settles which, and therefore whether reusing the
    // connection is the right fix or a red herring, without spending another fifteen-hour run to find out.

    /// <summary>Physical connections opened to the server during the interval (a delta). Expensive: TLS + auth.</summary>
    public const string Counter_SqlHardConnects = "SqlHardConnects";

    /// <summary>Connections taken from the pool during the interval (a delta). Cheap. The ratio against
    /// <see cref="Counter_SqlHardConnects"/> is the pooling hit rate.</summary>
    public const string Counter_SqlSoftConnects = "SqlSoftConnects";

    /// <summary>Live connections bypassing the pool. Enlistment in a transaction is one way to end up here.</summary>
    public const string Counter_SqlNonPooledConnections = "SqlNonPooledConnections";

    public const string Counter_SqlActiveConnections = "SqlActiveConnections";
    public const string Counter_SqlFreeConnections = "SqlFreeConnections";

    /// <summary>Connections awaiting completion of an action and unavailable for reuse - where a connection whose
    /// transaction has not yet resolved parks.</summary>
    public const string Counter_SqlStasisConnections = "SqlStasisConnections";

    // Pipeline observability. These exist for the two pipelining tickets (stage 40 read/write, stage 50 batch
    // prefetch), and they must land WITH those fixes rather than after them.
    //
    // The reason: occupancy - sum of leaf durations over stage wall clock - proves that a stage overlapped, but it
    // cannot say WHERE a pipeline stalled, and the two failure modes look identical from the outside. Record 29's
    // stage 40 scored 0.999 (perfectly serial) with a 128.2 min read against a 126.1 min write, so a working pipeline
    // should land near max(128.2, 126.1) = 128.2 min. If it instead lands at 180, the question is immediately "was the
    // producer starved, or was the consumer?" - and with only a duration to look at, answering it costs another
    // nine-hour run. A queue-depth distribution answers it for free:
    //
    //   depth pinned at the channel bound      -> the CONSUMER is the bottleneck. Correct and expected here: the read
    //                                             is the slower arm, so a full buffer means we are reading ahead fine.
    //   depth pinned at zero, starvation high  -> the PRODUCER is the bottleneck; the consumer is idle waiting for
    //                                             rows. If the wall clock did not improve, this is why.
    //   depth oscillating with low starvation  -> healthy; the arms are balanced and the pipeline is doing its job.
    //
    // Sampled by the runtime sampler on its existing interval rather than emitted per batch: at ~4,400 stage-40 and
    // ~22,000 stage-50 batches, per-batch emission would be tens of thousands of SendMetric calls to describe a queue
    // that only moves slowly. Registered as gauges the pipeline updates and the sampler reads.

    /// <summary>
    /// Batches sitting in the stage-40 donor-import pipeline's buffer at sample time. Bounded by the channel's
    /// capacity, so it is read against that bound, not in absolute terms.
    /// </summary>
    public const string Counter_DonorImportQueueDepth = "DonorImportQueueDepth";

    /// <summary>
    /// Times the stage-40 consumer found the buffer empty and had to wait for the reader, since the last sample.
    /// A delta. Non-trivially above zero means the pipeline is producer-bound and T1's prize is capped by the read.
    /// </summary>
    public const string Counter_DonorImportConsumerStarved = "DonorImportConsumerStarved";

    /// <summary>Batches prefetched and not yet processed by the stage-50 HLA batch loop, at sample time.</summary>
    public const string Counter_HlaBatchPrefetchDepth = "HlaBatchPrefetchDepth";

    /// <summary>
    /// Times the stage-50 batch loop found no prefetched batch ready, since the last sample. A delta. The stage-50
    /// read is 79.0 min against 170.1 min of processing, so on a working prefetch this should be near zero - if it is
    /// not, the prefetch depth is too shallow or the read got slower.
    /// </summary>
    public const string Counter_HlaBatchPrefetchStarved = "HlaBatchPrefetchStarved";

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
