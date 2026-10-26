using System.Collections.Generic;
using Atlas.MatchPrediction.ExternalInterface.Models;

namespace Atlas.MatchPrediction.ExternalInterface
{
    /// <summary>
    /// Selects the ids to stamp as Service Bus application properties on match prediction messages,
    /// so they are visible without reading the message body (e.g., in Service Bus Explorer or when inspecting dead letters).
    /// </summary>
    public static class MatchPredictionMessageProperties
    {
        public static IReadOnlyDictionary<string, object> ForParallelBatchRequest(ParallelMatchPredictionBatchRequest request) =>
            new Dictionary<string, object>
            {
                { nameof(ParallelMatchPredictionBatchRequest.SearchRequestId), request.SearchRequestId }
            };

        public static IReadOnlyDictionary<string, object> ForParallelBatchResult(ParallelMatchPredictionBatchResult result) =>
            new Dictionary<string, object>
            {
                { nameof(ParallelMatchPredictionBatchResult.SearchIdentifier), result.SearchIdentifier }
            };

        public static IReadOnlyDictionary<string, object> ForRequest(IdentifiedMatchPredictionRequest request) =>
            new Dictionary<string, object>
            {
                // Same key as on the result message, so a request and its result can be found by the same filter.
                { nameof(MatchPredictionResultLocation.MatchPredictionRequestId), request.Id },
                { nameof(MatchPredictionResultLocation.SearchRequestId), request.SingleDonorMatchProbabilityInput?.SearchRequestId }
            };

        public static IReadOnlyDictionary<string, object> ForResultLocation(MatchPredictionResultLocation location) =>
            new Dictionary<string, object>
            {
                { nameof(MatchPredictionResultLocation.MatchPredictionRequestId), location.MatchPredictionRequestId },
                { nameof(MatchPredictionResultLocation.SearchRequestId), location.SearchRequestId }
            };
    }
}
