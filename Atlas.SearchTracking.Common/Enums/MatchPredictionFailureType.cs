using System.Text.Json.Serialization;

namespace Atlas.SearchTracking.Common.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MatchPredictionFailureType
{
    OrchestrationError,
    UnexpectedError,

    /// <summary>One or more ACA Worker batches failed during match-prediction processing.</summary>
    BatchWorkerFailure,
}