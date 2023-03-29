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
        Task<List<string>> UploadBatchRequests(string searchRequestId, IEnumerable<MultipleDonorMatchProbabilityInput> batchRequests);
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

        public async Task<List<string>> UploadBatchRequests(string searchRequestId, IEnumerable<MultipleDonorMatchProbabilityInput> batchRequests)
        {
            var batchRequestsWithNames = batchRequests.Select(r => new KeyValuePair<string, MultipleDonorMatchProbabilityInput>($"{searchRequestId}/{r.MatchProbabilityRequestId}.json", r)).ToDictionary();
            await blobUploader.UploadMultiple(container, batchRequestsWithNames);
            return batchRequestsWithNames.Select(f => f.Key).ToList();
        }

        /// <inheritdoc />
        public async Task<MultipleDonorMatchProbabilityInput> DownloadBatchRequest(string blobLocation)
        {
            return await blobDownloader.Download<MultipleDonorMatchProbabilityInput>(container, blobLocation);
        }
    }
}