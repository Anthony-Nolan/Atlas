namespace Atlas.MatchingAlgorithm.Data.Persistent.Models
{
    /// <summary>
    /// The values of this enum are used to determine the order of a data refresh run.
    /// The values can be safely changed, but the order must remain the same. 
    /// </summary>
    public enum DataRefreshStage
    {
        /// <summary>
        /// Recreation of HLA Metadata Dictionary
        /// </summary>
        MetadataDictionaryRefresh = 0,
        /// <summary>
        /// Deletion of all existing donor data 
        /// </summary>
        DataDeletion = 1,
        /// <summary>
        /// Scaling of database to appropriate size for data refresh
        /// </summary>
        DatabaseScalingSetup = 2,
        /// <summary>
        /// Import all donors from the master donor store. No pre-processing to p-groups
        /// </summary>
        DonorImport = 3,
        /// <summary>
        /// Processing imported donors to p-groups. Encompasses index deletion and re-addition on relevant tables.
        /// </summary>
        DonorHlaProcessing = 4,
        /// <summary>
        /// Scaling of database to appropriate size for live usage.
        /// </summary>
        DatabaseScalingTearDown = 5,
        /// <summary>
        /// Consumption of all donor updates that have accrued during the data refresh.
        /// </summary>
        QueuedDonorUpdateProcessing = 6,
    }
}