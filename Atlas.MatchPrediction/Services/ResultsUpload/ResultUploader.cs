using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.AzureStorage.Blob;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Services.ResultsUpload
{
    internal interface IResultUploader
    {
        Task<string> UploadDonorResult(string searchRequestId, int atlasDonorId, MatchProbabilityResponse matchProbabilityResponse);
    }

    internal class ResultUploader : BlobUploader, IResultUploader
    {
        private readonly string resultsContainer;

        public ResultUploader(AzureStorageSettings azureStorageSettings, ILogger logger) : base(azureStorageSettings.ConnectionString, logger)
        {
            resultsContainer = azureStorageSettings.MatchPredictionResultsBlobContainer;
        }

        public async Task<string> UploadDonorResult(string searchRequestId, int atlasDonorId, MatchProbabilityResponse matchProbabilityResponse)
        {
            using (Logger.RunTimed("Uploading match prediction results", LogLevel.Verbose))
            {
                var serialisedResult = JsonConvert.SerializeObject(matchProbabilityResponse);
                var fileName = $"{searchRequestId}/{atlasDonorId}.json";
                await Upload(resultsContainer, fileName, serialisedResult);
                return fileName;
            }
        }
    }
}