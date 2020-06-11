namespace Atlas.MatchingAlgorithm.Data.Persistent.Models
{
    public enum DataRefreshStage
    {
        /// <summary>
        /// Recreation of HLA Metadata Dictionary
        /// </summary>
        MetadataDictionaryRefresh,
        /// <summary>
        /// Deletion of all existing donor data 
        /// </summary>
        DataDeletion,
        /// <summary>
        /// Scaling of database to appropriate size for data refresh
        /// </summary>
        DatabaseScalingSetup,
        /// <summary>
        /// Import all donors from the master donor store. No pre-processing to p-groups
        /// </summary>
        DonorImport,
        /// <summary>
        /// Processing imported donors to p-groups. Encompasses index deletion and re-addition on relevant tables.
        /// </summary>
        DonorHlaProcessing,
        /// <summary>
        /// Scaling of database to appropriate size for live usage.
        /// </summary>
        DatabaseScalingTearDown,
        /// <summary>
        /// Consumption of all donor updates that have accrued during the data refresh.
        /// </summary>
        QueuedDonorUpdateProcessing,
    }
}