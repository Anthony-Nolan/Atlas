using System.Text.Json.Serialization;

namespace Atlas.Debug.Client.Models.SearchTracking
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SearchTrackingMatchingAlgorithmFailureType
    {
        ValidationError,
        HlaMetadataDictionaryError,
        UnexpectedError
    }
}