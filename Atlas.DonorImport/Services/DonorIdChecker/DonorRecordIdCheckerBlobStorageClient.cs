using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.DonorImport.ExternalInterface.Settings;
using Atlas.DonorImport.FileSchema.Models.DonorIdChecker;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Services.DonorIdChecker
{
    public interface IDonorRecordIdCheckerBlobStorageClient
    {
        Task UploadResults(DonorIdCheckerResults idCheckerResults, string filename);

        Task InitiateUpload(string filename);
        Task UploadResults(IReadOnlyCollection<DonorIdCheckerResult> idCheckerResults);
        Task CommitUpload();
        Task CancelUpload();
    }

    public class DonorRecordIdCheckerBlobStorageClient : BlobUploader, IDonorRecordIdCheckerBlobStorageClient
    {
        private readonly string donorBlobContainer;
        private readonly string checkerResultsContainerName;

        private CloudBlockBlob blob;
        private List<string> blockList;

        public DonorRecordIdCheckerBlobStorageClient(AzureStorageSettings azureStorageSettings,ILogger logger) : base(azureStorageSettings.ConnectionString, logger)
        {
            donorBlobContainer = azureStorageSettings.DonorFileBlobContainer;
            checkerResultsContainerName = azureStorageSettings.DonorIdCheckerResultsBlobContainer;
        }

        public async Task UploadResults(DonorIdCheckerResults idCheckerResults, string filename)
        {
            var serialisedResults = JsonConvert.SerializeObject(idCheckerResults);
            await Upload(donorBlobContainer, $"{checkerResultsContainerName}/{filename}", serialisedResults);
        }

        public async Task InitiateUpload(string filename)
        {
            var containerRef = await GetBlobContainer(donorBlobContainer);
            blob = containerRef.GetBlockBlobReference($"{checkerResultsContainerName}/{filename}");
            blob.Properties.ContentType = "text/plain";

            blockList = new List<string>();
        }

        public async Task UploadResults(IReadOnlyCollection<DonorIdCheckerResult> idCheckerResults)
        {
            var message = JsonConvert.SerializeObject(idCheckerResults);
            var messageStream = new MemoryStream(Encoding.UTF8.GetBytes(message));

            var blockId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            await blob.PutBlockAsync(blockId, messageStream);

            blockList.Add(blockId);
        }

        public async Task CommitUpload() =>
            await blob.PutBlockListAsync(blockList);

        public async Task CancelUpload()
        {
            if (blockList != null)
            {
                await blob.PutBlockListAsync(blockList);
            }

            await blob.DeleteIfExistsAsync();
        }
    }
}
