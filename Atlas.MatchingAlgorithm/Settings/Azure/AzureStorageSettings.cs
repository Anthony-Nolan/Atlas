namespace Atlas.MatchingAlgorithm.Settings.Azure
{
    public class AzureStorageSettings
    {
        public string ConnectionString { get; set; }
        public string SearchResultsBlobContainer { get; set; }
        public int BatchSize { get; set; }

        public bool ResultBatched => BatchSize > 0;
    }
}