using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Functions.Settings;
using Microsoft.Extensions.Options;

namespace Atlas.Functions.Services.BlobStorageClients
{
    internal interface IMatchPredictionResultsDownloader
    {
        public Task<MatchProbabilityResponse> Download(string blobName);
    }

    internal class MatchPredictionResultsDownloader : IMatchPredictionResultsDownloader
    {
        private readonly AzureStorageSettings messagingServiceBusSettings;
        private readonly IBlobDownloader blobDownloader;

        public MatchPredictionResultsDownloader(
            IOptions<AzureStorageSettings> azureStorageSettings,
            IBlobDownloader blobDownloader,
            ILogger logger)
        {
            messagingServiceBusSettings = azureStorageSettings.Value;
            this.blobDownloader = blobDownloader;
        }

        /// <inheritdoc />
        public async Task<MatchProbabilityResponse> Download(string blobName)
        {
            return await blobDownloader.Download<MatchProbabilityResponse>(
                messagingServiceBusSettings.MatchPredictionResultsBlobContainer,
                blobName
            );
        }
    }
}