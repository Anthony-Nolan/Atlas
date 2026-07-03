using System.Text.Json.Serialization;

namespace Atlas.SearchTracking.Common.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MatchPredictionFailureType
    {
        OrchestrationError,
        UnexpectedError,

        /// <summary>One or more ACA Worker batches failed during match-prediction processing.</summary>
        BatchWorkerFailure,

        /// <summary>
        /// The run was abandoned because one or more batches did not return a result within the configured timeout.
        /// </summary>
        Abandoned,
    }
}
