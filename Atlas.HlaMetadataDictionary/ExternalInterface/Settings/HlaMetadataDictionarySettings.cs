namespace Atlas.HlaMetadataDictionary.ExternalInterface.Settings
{
    public class HlaMetadataDictionarySettings
    {
        public string AzureStorageConnectionString { get; set; }
        public string HlaNomenclatureSourceUrl { get; set; }
        public int MaxRetries { get; set; } = 10;  
        public int MaxDelayMilliseconds { get; set; } = 120000;
        public int DelayMilliseconds { get; set; } = 200;
        
        public SearchRelatedMetadataServiceSettings SearchRelatedMetadata { get; set; }
    }
}