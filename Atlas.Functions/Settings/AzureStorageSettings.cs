namespace Atlas.Functions.Settings
{
    internal class AzureStorageSettings
    {
        public string MatchingConnectionString { get; set; }
        public string MatchingResultsBlobContainer { get; set; }
        public string SearchResultsBlobContainer { get; set; }
        
        public string MatchPredictionConnectionString { get; set; }
        public string MatchPredictionResultsBlobContainer { get; set; }
    }
}