using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Debug.Client.Models.SearchResults;

namespace Atlas.Common.Debugging
{
    /// <summary>
    /// Downloads results for debugging purposes.
    /// </summary>
    public interface IDebugResultsDownloader
    {
        /// <summary>
        /// If batch folder is provided, results will be loaded from all files within this folder.
        /// Otherwise, results will be loaded along with search summary from the main search results file.
        /// </summary>
        public Task<TResultSet> DownloadResultSet<TResultSet, TResult>(DebugSearchResultsRequest request)
            where TResultSet : ResultSet<TResult>
            where TResult : Result;
    }

    public class DebugResultsDownloader : IDebugResultsDownloader
    {
        private readonly IBlobDownloader blobDownloader;

        public DebugResultsDownloader(IBlobDownloader blobDownloader)
        {
            this.blobDownloader = blobDownloader;
        }

        public async Task<TResultSet> DownloadResultSet<TResultSet, TResult>(DebugSearchResultsRequest request)
            where TResultSet : ResultSet<TResult> 
            where TResult : Result
        {
            var resultSet = await blobDownloader.Download<TResultSet>(request.SearchResultBlobContainer, request.SearchResultFileName);

            resultSet.Results ??= !string.IsNullOrEmpty(request.BatchFolderName)
                ? await blobDownloader.DownloadFolderContents<TResult>(request.SearchResultBlobContainer, request.BatchFolderName)
                : new List<TResult>();

            return resultSet;
        }
    }
}