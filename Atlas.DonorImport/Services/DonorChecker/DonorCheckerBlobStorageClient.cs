﻿using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.DonorImport.FileSchema.Models.DonorChecker;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Services.DonorChecker
{
    public interface IDonorCheckerBlobStorageClient
    {
        Task UploadResults(DonorCheckerResults checkerResults, string filename);
    }

    public interface IDonorIdCheckerBlobStorageClient : IDonorCheckerBlobStorageClient { }
    public interface IDonorInfoCheckerBlobStorageClient : IDonorCheckerBlobStorageClient { }

    internal class DonorCheckerBlobStorageClient : BlobUploader, IDonorIdCheckerBlobStorageClient, IDonorInfoCheckerBlobStorageClient
    {
        private readonly string donorBlobContainer;
        private readonly string checkerFolderName;

        public DonorCheckerBlobStorageClient(ILogger logger, string connectionString, string donorBlobContainer, string checkerFolderName) : base(connectionString, logger)
        {
            this.donorBlobContainer = donorBlobContainer;
            this.checkerFolderName = checkerFolderName;
        }

        public async Task UploadResults(DonorCheckerResults checkerResults, string filename)
        {
            var serialisedResults = JsonConvert.SerializeObject(checkerResults);
            await Upload(donorBlobContainer, $"{checkerFolderName}/{filename}", serialisedResults);
        }
    }
}