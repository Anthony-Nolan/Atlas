using Atlas.MatchPrediction.ExternalInterface.Models;

namespace Atlas.MatchPrediction.ExternalInterface.ResultsUpload
{
    public static class ResultLocationBuilder
    {
        public static MatchPredictionResultLocation BuildMatchPredictionRequestResultLocation(string matchPredictionRequestId, string resultsContainer)
        {
            var fileName = $"match-prediction-requests/{matchPredictionRequestId}.json";

            return new MatchPredictionResultLocation
            {
                MatchPredictionRequestId = matchPredictionRequestId,
                BlobStorageContainerName = resultsContainer,
                FileName = fileName
            };
        }
    }
}