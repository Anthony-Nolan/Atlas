using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.ApplicationInsights;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Atlas.Common.AzureStorage.Blob
{
    public interface ISearchResultsBlobStorageClient
    {
        Task UploadResults<T>(ResultSet<T> searchResultSet, string batchFolder) where T : Result;
    }

    public class SearchResultsBlobStorageClient : BlobUploader, ISearchResultsBlobStorageClient
    {
        private readonly int searchResultsBatchSize;

        public SearchResultsBlobStorageClient(string connectionString, int searchResultsBatchSize, ILogger logger)
            : base(connectionString, logger)
        {
            this.searchResultsBatchSize = searchResultsBatchSize;
        }

        public async Task UploadResults<T>(ResultSet<T> searchResultSet, string batchFolder) where T : Result
        {
            // Results will not be serialised if results are being batched
            var serialisedResults = JsonConvert.SerializeObject(searchResultSet);
            await Upload(searchResultSet.BlobStorageContainerName, searchResultSet.ResultsFileName, serialisedResults);

            if (searchResultSet.BatchedResult)
            {
                await BatchUpload(searchResultSet.Results, searchResultsBatchSize, searchResultSet.BlobStorageContainerName, batchFolder);
            }
        }
    }
}
