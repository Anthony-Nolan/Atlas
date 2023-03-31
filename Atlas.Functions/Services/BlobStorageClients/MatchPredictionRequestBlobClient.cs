using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Utils.Extensions;
using Atlas.Functions.Settings;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Microsoft.Extensions.Options;

namespace Atlas.Functions.Services.BlobStorageClients
{
    public interface IMatchPredictionRequestBlobClient
    {
        Task<IEnumerable<string>> UploadMatchProbabilityRequests(string searchRequestId, IEnumerable<MultipleDonorMatchProbabilityInput> requests);
        Task<MultipleDonorMatchProbabilityInput> DownloadBatchRequest(string blobLocation);
    }

    internal class MatchPredictionRequestBlobClient : IMatchPredictionRequestBlobClient
    {
        private readonly BlobUploader blobUploader;
        private readonly IBlobDownloader blobDownloader;
        private readonly string container;

        public MatchPredictionRequestBlobClient(IOptions<AzureStorageSettings> azureStorageSettings, ILogger logger)
        {
            blobDownloader = new BlobDownloader(azureStorageSettings.Value.MatchPredictionConnectionString, logger);
            blobUploader = new BlobUploader(azureStorageSettings.Value.MatchPredictionConnectionString, logger);
            container = azureStorageSettings.Value.MatchPredictionRequestsBlobContainer;
        }

        public async Task<IEnumerable<string>> UploadMatchProbabilityRequests(string searchRequestId, IEnumerable<MultipleDonorMatchProbabilityInput> requests)
        {
            var requestsWithNames = requests.Select(r => new KeyValuePair<string, MultipleDonorMatchProbabilityInput>($"{searchRequestId}/{r.MatchProbabilityRequestId}.json", r)).ToDictionary();
            await blobUploader.UploadMultiple(container, requestsWithNames);
            return requestsWithNames.Select(f => f.Key);
        }

        /// <inheritdoc />
        public async Task<MultipleDonorMatchProbabilityInput> DownloadBatchRequest(string blobLocation)
        {
            return await blobDownloader.Download<MultipleDonorMatchProbabilityInput>(container, blobLocation);
        }
    }
}