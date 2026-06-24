using System.ComponentModel.DataAnnotations;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Settings;

public class SearchRelatedMetadataServiceSettings
{
    [Range(1, int.MaxValue)]
    public int? CacheSlidingExpirationInSeconds { get; set; }
}