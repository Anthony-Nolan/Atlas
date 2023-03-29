using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Functions.Settings;
using Microsoft.Extensions.Options;

namespace Atlas.Functions.Services.BlobStorageClients
{
    internal interface IMatchPredictionResultsDownloader
    {
        Task<Dictionary<int, MatchProbabilityResponse>> Download(IReadOnlyDictionary<int, string> resultLocations);
    }

    internal class MatchPredictionResultsDownloader : IMatchPredictionResultsDownloader
    {
        private readonly AzureStorageSettings messagingServiceBusSettings;
        private readonly IBlobDownloader blobDownloader;

        public MatchPredictionResultsDownloader(
            IOptions<AzureStorageSettings> azureStorageSettings,
            IBlobDownloader blobDownloader)
        {
            messagingServiceBusSettings = azureStorageSettings.Value;
            this.blobDownloader = blobDownloader;
        }

        public async Task<Dictionary<int, MatchProbabilityResponse>> Download(IReadOnlyDictionary<int, string> resultLocations)
            => await blobDownloader.DownloadMultipleBlobs<MatchProbabilityResponse>(messagingServiceBusSettings.MatchPredictionResultsBlobContainer, resultLocations);
    }
}