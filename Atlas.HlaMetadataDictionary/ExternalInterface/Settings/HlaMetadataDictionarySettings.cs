namespace Atlas.HlaMetadataDictionary.ExternalInterface.Settings
{
    public class HlaMetadataDictionarySettings
    {
        public string AzureStorageConnectionString { get; set; }
        public string HlaNomenclatureSourceUrl { get; set; }
        public int MaxRetries { get; set; } = 10;
        public int MaxDelay { get; set; } = 30;

        public SearchRelatedMetadataServiceSettings SearchRelatedMetadata { get; set; }
    }
}