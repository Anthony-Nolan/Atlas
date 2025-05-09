using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.Debug.Client.Models.SearchTracking
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SearchTrackingMatchPredictionFailureType
    {
        OrchestrationError,
        UnexpectedError
    }
}
