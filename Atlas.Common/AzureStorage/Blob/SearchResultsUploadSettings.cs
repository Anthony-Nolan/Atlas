namespace Atlas.Common.AzureStorage.Blob
{
    public class SearchResultsUploadSettings
    {
        public int SearchResultsBatchSize { get; set; }

        public bool ShouldBatchResults => SearchResultsBatchSize > 0;
    }
}
