namespace Atlas.DonorImport.ExternalInterface.Settings
{
    public class PublishDonorUpdatesSettings
    {
        /// <summary>
        /// Number of days after which a published donor update will expire and be eligible for deletion from the repository.
        /// </summary>
        public int? PublishedUpdateExpiryInDays { get; set; }
    }
}