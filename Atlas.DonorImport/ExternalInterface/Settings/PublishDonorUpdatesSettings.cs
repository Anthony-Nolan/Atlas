namespace Atlas.DonorImport.ExternalInterface.Settings
{
    public class PublishDonorUpdatesSettings
    {
        /// <summary>
        /// Number of days after which a published donor update will expire and be eligible for deletion from the repository.
        /// </summary>
        public int? PublishedUpdateExpiryInDays { get; set; }

        /// <summary>
        /// Maximum number of published donor updates to delete at one time.
        /// </summary>  
        public int PublishedUpdatesToDeleteCap { get; set; }

        /// <summary>
        /// Number of published donor updates to delete in each batch.
        /// </summary>
        public int PublishedUpdatesToDeleteBatchSize { get; set; }
    }
}