namespace Atlas.HlaMetadataDictionary.ExternalInterface.Settings
{
    public class HlaMetadataDictionarySettings
    {
        public string AzureStorageConnectionString { get; set; }
        public string HlaNomenclatureSourceUrl { get; set; }

        public SearchRelatedMetadataServiceSettings SearchRelatedMetadata { get; set; }
    }
}