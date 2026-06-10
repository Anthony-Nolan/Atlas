using System.Text.Json.Serialization;

namespace Atlas.Debug.Client.Models.SearchTracking;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SearchTrackingMatchPredictionFailureType
{
    OrchestrationError,
    UnexpectedError,

    /// <summary>One or more ACA Worker batches failed during match-prediction processing.</summary>
    BatchWorkerFailure,
}