using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.SearchTracking.Common.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MatchingAlgorithmFailureType
    {
        ValidationError,
        HlaMetadataDictionaryError,
        UnexpectedError
    }
}
