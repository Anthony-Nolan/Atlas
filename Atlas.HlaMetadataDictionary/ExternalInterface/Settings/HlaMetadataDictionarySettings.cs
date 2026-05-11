using System.ComponentModel.DataAnnotations;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Settings
{
    public class HlaMetadataDictionarySettings
    {
        [Required(AllowEmptyStrings = false)]
        public string AzureStorageConnectionString { get; set; }

        public string HlaNomenclatureSourceUrl { get; set; }

        [Range(0, int.MaxValue)]
        public int MaxRetries { get; set; } = 10;  

        [Range(1, int.MaxValue)]
        public int MaxDelayMilliseconds { get; set; } = 120000;

        [Range(1, int.MaxValue)]
        public int DelayMilliseconds { get; set; } = 200;
        
        public SearchRelatedMetadataServiceSettings SearchRelatedMetadata { get; set; }
    }
}