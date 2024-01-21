using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.DonorImport.FileSchema.Models;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Services.Debug
{
    public interface IDonorImportBlobStorageClient
    {
        Task UploadFile(DonorImportFileSchema fileContents, string fileName);
    }

    internal class DonorImportBlobStorageClient : BlobUploader, IDonorImportBlobStorageClient
    {
        private readonly string donorBlobContainer;

        public DonorImportBlobStorageClient(
            ILogger logger,
            string connectionString,
            string donorBlobContainer) : base(connectionString, logger)
        {
            this.donorBlobContainer = donorBlobContainer;
        }

        public async Task UploadFile(DonorImportFileSchema fileContents, string fileName)
        {
            var serialisedResults = JsonConvert.SerializeObject(fileContents);
            await Upload(donorBlobContainer, fileName, serialisedResults);
        }
    }
}
