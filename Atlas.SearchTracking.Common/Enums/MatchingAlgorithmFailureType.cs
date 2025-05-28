using System.Text.Json.Serialization;

namespace Atlas.SearchTracking.Common.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MatchingAlgorithmFailureType
    {
        ValidationError,
        HlaMetadataDictionaryError,
        UnexpectedError
    }
}
