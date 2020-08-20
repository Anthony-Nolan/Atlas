using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Functions.Settings;
using Microsoft.Extensions.Options;

namespace Atlas.Functions.Services
{
    public interface IMatchingResultsDownloader
    {
        public Task<MatchingAlgorithmResultSet> Download(string blobName);
    }

    internal class MatchingResultsDownloader : IMatchingResultsDownloader
    {
        private readonly AzureStorageSettings messagingServiceBusSettings;
        private readonly IBlobDownloader blobDownloader;

        public MatchingResultsDownloader(IOptions<AzureStorageSettings> messagingServiceBusSettings, IBlobDownloader blobDownloader)
        {
            this.messagingServiceBusSettings = messagingServiceBusSettings.Value;
            this.blobDownloader = blobDownloader;
        }

        /// <inheritdoc />
        public async Task<MatchingAlgorithmResultSet> Download(string blobName)
        {
            return await blobDownloader.Download<MatchingAlgorithmResultSet>(messagingServiceBusSettings.MatchingResultsBlobContainer, blobName);
        }
    }
}