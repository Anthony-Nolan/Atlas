using Atlas.Common.AzureStorage.Blob;

namespace Atlas.MatchingAlgorithm.Settings.Azure
{
    public class AzureStorageSettings : SearchResultsUploadSettings
    {
        public string ConnectionString { get; set; }
        public string SearchResultsBlobContainer { get; set; }
    }
}