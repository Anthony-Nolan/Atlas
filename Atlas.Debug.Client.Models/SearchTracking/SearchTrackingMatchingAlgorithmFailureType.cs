using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.Debug.Client.Models.SearchTracking
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SearchTrackingMatchingAlgorithmFailureType
    {
        ValidationError,
        HlaMetadataDictionaryError,
        UnexpectedError
    }
}