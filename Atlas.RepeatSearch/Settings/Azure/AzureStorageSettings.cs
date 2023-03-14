namespace Atlas.RepeatSearch.Settings.Azure
{
    public class AzureStorageSettings
    {
        public string ConnectionString { get; set; }
        public string MatchingResultsBlobContainer { get; set; }
        public int BatchSize { get; set; }

        public bool ResultBatched => BatchSize > 0;
    }
}
