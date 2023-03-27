using Atlas.Common.AzureStorage.Blob;

namespace Atlas.RepeatSearch.Settings.Azure
{
    public class AzureStorageSettings : SearchResultsUploadSettings
    {
        public string ConnectionString { get; set; }
        public string MatchingResultsBlobContainer { get; set; }
    }
}
