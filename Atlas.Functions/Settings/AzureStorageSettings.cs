namespace Atlas.Functions.Settings
{
    public class AzureStorageSettings
    {
        public string MatchingConnectionString { get; set; }
        public string MatchingResultsBlobContainer { get; set; }
        public string RepeatSearchMatchingResultsBlobContainer { get; set; }
        public string SearchResultsBlobContainer { get; set; }
        public string RepeatSearchResultsBlobContainer { get; set; }
        
        public string MatchPredictionConnectionString { get; set; }
        public string MatchPredictionRequestsBlobContainer { get; set; }
        public string MatchPredictionResultsBlobContainer { get; set; }

        public bool ResultsShouldBeBatched { get; set; }
    }
}