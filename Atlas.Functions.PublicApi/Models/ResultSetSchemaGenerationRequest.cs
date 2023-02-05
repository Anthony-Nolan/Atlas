using System.Text.Json.Serialization;

namespace Atlas.Functions.PublicApi.Models
{
    public class ResultSetSchemaGenerationRequest
    {
        public ResultSetOptions ResultSet { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ResultSetOptions
    {
        OriginalSearch,
        RepeatSearch,
        OriginalMatchingAlgorithm,
        RepeatMatchingAlgorithm
    }
}
