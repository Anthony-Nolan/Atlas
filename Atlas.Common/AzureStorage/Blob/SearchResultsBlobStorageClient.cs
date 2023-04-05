using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.ApplicationInsights;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Atlas.Common.AzureStorage.Blob
{
    public interface ISearchResultsBlobStorageClient
    {
        Task UploadResults<T>(ResultSet<T> searchResultSet, int searchResultsBatchSize, string batchFolder) where T : Result;
        Task UploadResults<T>(T results, string blobContainerName, string fileName);
    }

    public class SearchResultsBlobStorageClient : BlobUploader, ISearchResultsBlobStorageClient
    {
        public SearchResultsBlobStorageClient(string connectionString, ILogger logger)
            : base(connectionString, logger)
        {
        }

        public async Task UploadResults<T>(ResultSet<T> searchResultSet, int searchResultsBatchSize, string batchFolder) where T : Result
        {
            await UploadResults(searchResultSet, searchResultSet.BlobStorageContainerName, searchResultSet.ResultsFileName);

            if (searchResultSet.BatchedResult)
            {
                await ChunkAndUpload(searchResultSet.Results, searchResultsBatchSize, searchResultSet.BlobStorageContainerName, batchFolder);
            }
        }

        public async Task UploadResults<T>(T results, string blobContainerName, string fileName)
        {
            // Results will not be serialised if results are being batched
            var serialisedResults = JsonConvert.SerializeObject(results);
            await Upload(blobContainerName, fileName, serialisedResults);
        }
    }
}
