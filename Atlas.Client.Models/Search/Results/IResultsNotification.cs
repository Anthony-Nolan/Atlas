namespace Atlas.Client.Models.Search.Results
{
    public interface IResultsNotification
    {
        string SearchRequestId { get; set; }
        bool WasSuccessful { get; set; }
        string BlobStorageContainerName { get; set; }
        string ResultsFileName { get; set; }
        int? NumberOfResults { get; set; }
    }
}
