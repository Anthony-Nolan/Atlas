using Atlas.Common.AzureStorage;

namespace Atlas.MatchingAlgorithm.Settings.Azure
{
    public class MatchingAzureStorageSettings : AzureStorageSettings
    {
        public string SearchResultsBlobContainer { get; set; }
    }
}