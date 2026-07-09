using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.MatchPrediction.ExternalInterface.Settings;

namespace Atlas.MatchPrediction.ExternalInterface.ResultsUpload
{
    internal interface IMatchPredictionBatchResultUploader
    {
        /// <summary>
        /// Uploads a whole parallel batch's match probability results into a single blob, keyed by Atlas donor id and
        /// named after the batch id.
        /// </summary>
        /// <returns>The blob filename where the batch results were stored.</returns>
        Task<string> UploadMatchPredictionBatchResult(string searchRequestId, int batchId, IReadOnlyDictionary<int, MatchProbabilityResponse> resultsByDonorId);
    }

    internal class MatchPredictionBatchResultUploader : MatchProbabilityResultUploader, IMatchPredictionBatchResultUploader
    {
        public MatchPredictionBatchResultUploader(AzureStorageSettings azureStorageSettings, IAtlasLogger logger) : base(azureStorageSettings, logger)
        {
        }

        public async Task<string> UploadMatchPredictionBatchResult(string searchRequestId, int batchId, IReadOnlyDictionary<int, MatchProbabilityResponse> resultsByDonorId)
        {
            var fileName = $"{searchRequestId}/match-prediction-batch-{batchId}.json";
            using (logger.RunTimed("Uploading match prediction batch results", LogLevel.Verbose))
            {
                await Upload(ResultsContainer, fileName, resultsByDonorId);
            }

            return fileName;
        }
    }
}
