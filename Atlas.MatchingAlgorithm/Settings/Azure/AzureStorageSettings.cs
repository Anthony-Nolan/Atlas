namespace Atlas.MatchingAlgorithm.Settings.Azure
{
    public class AzureStorageSettings
    {
        public string ConnectionString { get; set; }
        public string SearchResultsBlobContainer { get; set; }
        public int SearchResultsBatchSize { get; set; }

        public bool ShouldBatchResults => SearchResultsBatchSize > 0;
    }
}