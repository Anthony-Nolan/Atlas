using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.Debug.Client.Models.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MatchingAlgorithmFailureType
    {
        ValidationError,
        HlaMetadataDictionaryError,
        UnexpectedError
    }
}