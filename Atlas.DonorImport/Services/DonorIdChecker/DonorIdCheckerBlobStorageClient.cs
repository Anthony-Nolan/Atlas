using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.DonorImport.ExternalInterface.Settings;
using Atlas.DonorImport.FileSchema.Models.DonorIdChecker;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Services.DonorIdChecker
{
    public interface IDonorIdCheckerBlobStorageClient
    {
        Task UploadResults(DonorIdCheckerResults idCheckerResults, string filename);
    }

    internal class DonorIdCheckerBlobStorageClient : BlobUploader, IDonorIdCheckerBlobStorageClient
    {
        private readonly string donorBlobContainer;
        private readonly string checkerResultsContainerName;

        public DonorIdCheckerBlobStorageClient(AzureStorageSettings azureStorageSettings,ILogger logger) : base(azureStorageSettings.ConnectionString, logger)
        {
            donorBlobContainer = azureStorageSettings.DonorFileBlobContainer;
            checkerResultsContainerName = azureStorageSettings.DonorIdCheckerResultsBlobContainer;
        }

        public async Task UploadResults(DonorIdCheckerResults idCheckerResults, string filename)
        {
            var serialisedResults = JsonConvert.SerializeObject(idCheckerResults);
            await Upload(donorBlobContainer, $"{checkerResultsContainerName}/{filename}", serialisedResults);
        }
    }
}
