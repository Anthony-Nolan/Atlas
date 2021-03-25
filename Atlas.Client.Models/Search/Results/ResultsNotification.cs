namespace Atlas.Client.Models.Search.Results
{
    public abstract class ResultsNotification
    {
        public string SearchRequestId { get; set; }
        public bool WasSuccessful { get; set; }
        
        /// <summary>
        /// Name of the container in blob storage where results can be found.
        /// </summary>
        public string BlobStorageContainerName { get; set; }

        /// <summary>
        /// Name of the file in which results are stored in blob storage. 
        /// </summary>
        public string ResultsFileName { get; set; }

        public int? NumberOfResults { get; set; }
    }
}