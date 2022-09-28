using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.AzureStorage.Blob;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.ExternalInterface.ResultsUpload
{
    public abstract class MatchProbabilityResultUploader : BlobUploader
    {
        protected readonly string ResultsContainer;

        protected MatchProbabilityResultUploader(AzureStorageSettings azureStorageSettings, ILogger logger) : base(azureStorageSettings.ConnectionString, logger)
        {
            ResultsContainer = azureStorageSettings.MatchPredictionResultsBlobContainer;
        }

        public async Task UploadResult(string fileName, MatchProbabilityResponse matchProbabilityResponse)
        {
            using (Logger.RunTimed("Uploading match prediction results", LogLevel.Verbose))
            {
                var serialisedResult = JsonConvert.SerializeObject(matchProbabilityResponse);
                await Upload(ResultsContainer, fileName, serialisedResult);
            }
        }
    }
}