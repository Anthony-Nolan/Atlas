using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Functions.Settings;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Atlas.Functions.Services.BlobStorageClients
{
    public interface IMatchPredictionRequestBlobClient
    {
        Task<string> UploadBatchRequest(string searchRequestId, MultipleDonorMatchProbabilityInput batchRequest);
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

        /// <inheritdoc />
        public async Task<string> UploadBatchRequest(string searchRequestId, MultipleDonorMatchProbabilityInput batchRequest)
        {
            var serialisedResult = JsonConvert.SerializeObject(batchRequest);
            var fileName = $"{searchRequestId}/{batchRequest.MatchProbabilityRequestId}.json";
            await blobUploader.Upload(container, fileName, serialisedResult);
            return fileName;
        }

        /// <inheritdoc />
        public async Task<MultipleDonorMatchProbabilityInput> DownloadBatchRequest(string blobLocation)
        {
            return await blobDownloader.Download<MultipleDonorMatchProbabilityInput>(container, blobLocation);
        }
    }
}