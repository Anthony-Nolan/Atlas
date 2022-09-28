namespace Atlas.MatchPrediction.ExternalInterface.Models
{
    public class MatchPredictionResultLocation
    {
        public string MatchPredictionRequestId { get; set; }

        /// <summary>
        /// Name of the container in blob storage where results can be found.
        /// </summary>
        public string BlobStorageContainerName { get; set; }

        /// <summary>
        /// Name of the file in which results are stored in blob storage. 
        /// </summary>
        public string FileName { get; set; }
    }
}
