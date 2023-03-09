using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.DonorImport.ExternalInterface.Settings;
using Atlas.DonorImport.FileSchema.Models.DonorComparer;
using Atlas.DonorImport.FileSchema.Models.DonorIdChecker;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Services
{
    public interface IDonorCheckerBlobStorageClient
    {
        Task UploadResults(DonorIdCheckerResults idCheckerResults, string filename);
        Task UploadResults(DonorComparerResults donorComparerResults, string filename);
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

        public async Task UploadResults(DonorIdCheckerResults idCheckerResults, string filename)
        {
            var serialisedResults = JsonConvert.SerializeObject(idCheckerResults);
            await Upload(donorBlobContainer, $"{idCheckerResultsContainerName}/{filename}", serialisedResults);
        }

        public async Task UploadResults(DonorComparerResults donorComparerResults, string filename)
        {
            var serialisedResults = JsonConvert.SerializeObject(donorComparerResults);
            await Upload(donorBlobContainer, $"{donorComparerResultsContainerName}/{filename}", serialisedResults);
        }
    }
}
