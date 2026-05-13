namespace Atlas.Functions.Settings
{
    internal class OrchestrationSettings
    {
        public int MatchPredictionBatchSize { get; set; }

        /// <summary>Donor batch size used when preparing blobs for the parallel ACA Worker MPA path.</summary>
        public int ParallelMpaBatchSize { get; set; }
    }
}