namespace Atlas.Functions.Settings
{
    internal class AzureStorageSettings
    {
        public string ConnectionString { get; set; }
        public string MatchingResultsBlobContainer { get; set; }
        public string SearchResultsBlobContainer { get; set; }
    }
}