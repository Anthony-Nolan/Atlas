using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.Data.Helpers
{
    /// <summary>
    /// Metric-based replacement for the Trace-based timers keyed by <see cref="DataRefreshTimingKeys"/>.
    ///
    /// Durations are emitted as pre-aggregated Application Insights metrics (the <c>customMetrics</c> table) which
    /// are never sampled, rather than as <c>TrackTrace</c> summaries which the isolated worker's adaptive sampling
    /// dropped when they burst out at Data Refresh stage-50 completion.
    ///
    /// One metric name, with the operation and locus carried as (low-cardinality) dimensions, so a single Kusto
    /// query can slice by either:
    /// <code>
    /// customMetrics
    /// | where name == "DataRefresh.DurationMs"
    /// | summarize totalMs = sum(valueSum), calls = sum(valueCount), avgMs = sum(valueSum)/sum(valueCount)
    ///     by Operation = tostring(customDimensions.Operation), Locus = tostring(customDimensions.Locus)
    /// </code>
    /// </summary>
    public static class DataRefreshMetrics
    {
        public const string DurationMsMetric = "DataRefresh.DurationMs";

        public const string OperationDimension = "Operation";
        public const string LocusDimension = "Locus";

        // Operation dimension values. Kept low-cardinality by design (a fixed, small set).
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

        // Stage 40 (DonorImport) operations. Added after the A1 stage-ranking showed DonorImport is a co-largest
        // stage (on par with DonorHlaProcessing), yet was previously only timed by a single sampled Verbose Trace.
        // Splitting it into conversion (CPU) vs the two SQL writes (DB) answers the same "our loop or the DB?"
        // question for stage 40 that the stage-50 operations answer for stage 50. The cross-DB donor stream read is
        // not timed directly; it is recoverable as DonorImportStageTotal minus the sum of the DonorImportBatch spans.
        public const string Operation_DonorImportStageTotal = "DonorImportStageTotal";
        public const string Operation_DonorImportBatch = "DonorImportBatch";
        public const string Operation_DonorInfoConversion = "DonorInfoConversion";
        public const string Operation_DonorBulkInsert = "DonorBulkInsert";
        public const string Operation_DonorManagementLogWrite = "DonorManagementLogWrite";

        /// <summary>Locus dimension value used when a measurement is not scoped to a single locus.</summary>
        public const string Locus_All = "all";

        /// <summary>
        /// Builds the dimension set for the duration metric. Always supplies BOTH dimension keys (with Locus
        /// defaulting to <see cref="Locus_All"/>) so every call for <see cref="DurationMsMetric"/> uses an
        /// identical dimension-key set — a requirement of the underlying <c>GetMetric</c> aggregator.
        /// </summary>
        public static Dictionary<string, string> Dims(string operation, string locus = Locus_All) =>
            new Dictionary<string, string>
            {
                { OperationDimension, operation },
                { LocusDimension, locus }
            };
    }
}