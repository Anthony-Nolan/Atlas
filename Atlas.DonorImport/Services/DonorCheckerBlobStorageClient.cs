using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.DonorImport.ExternalInterface.Settings;
using Atlas.DonorImport.FileSchema.Models.DonorChecker;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Services
{
    public interface IDonorCheckerBlobStorageClient
    {
        Task UploadDonorIdCheckerResults(DonorCheckerResults checkerResults, string filename);
        Task UploadDonorInfoCheckerResults(DonorCheckerResults checkerResults, string filename);
    }

    internal class DonorCheckerBlobStorageClient : BlobUploader, IDonorCheckerBlobStorageClient
    {
        private readonly string donorBlobContainer;
        private readonly string idCheckerResultsContainerName;
        private readonly string donorComparerResultsContainerName;

        public DonorCheckerBlobStorageClient(AzureStorageSettings azureStorageSettings, ILogger logger) : base(azureStorageSettings.ConnectionString, logger)
        {
            donorBlobContainer = azureStorageSettings.DonorFileBlobContainer;
            idCheckerResultsContainerName = azureStorageSettings.DonorIdCheckerResultsBlobContainer;
            donorComparerResultsContainerName = azureStorageSettings.CompareDonorsResultsBlobContainer;
        }

        public async Task UploadDonorIdCheckerResults(DonorCheckerResults checkerResults, string filename) =>
            await UploadResults(checkerResults, $"{idCheckerResultsContainerName}/{filename}");

        public async Task UploadDonorInfoCheckerResults(DonorCheckerResults checkerResults, string filename) =>
            await UploadResults(checkerResults, $"{donorComparerResultsContainerName}/{filename}");

        private async Task UploadResults(DonorCheckerResults checkerResults, string fileLocation)
        {
            var serialisedResults = JsonConvert.SerializeObject(checkerResults);
            await Upload(donorBlobContainer, fileLocation, serialisedResults);
        }
    }
}
