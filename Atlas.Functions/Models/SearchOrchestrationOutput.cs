namespace Atlas.Functions.Models
{
    /// <summary>
    /// This is the data that will be returned on a poll to the status Query of the search orchestration function on success.
    /// Only summary data is provided here, as the full output will be very large and should be fetched from azure storage. 
    /// </summary>
    public class SearchOrchestrationOutput
    {
        public int MatchingDonorCount { get; set; }
        public string MatchingResultBlobContainer { get; set; }
        public string MatchingResultFileName { get; set; }
        public string HlaNomenclatureVersion { get; set; }
    }
}