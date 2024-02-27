using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.AzureStorage.Blob;

namespace Atlas.Common.Debugging
{
    /// <summary>
    /// Downloads results for debugging purposes.
    /// </summary>
    public interface IDebugResultsDownloader
    {
        /// <summary>
        /// If <paramref name="batchFolder"/> is not null, results will be loaded from all files within this folder.
        /// Otherwise, results will be loaded along with search summary from <paramref name="searchResultFileName"/> file.
        /// </summary>
        public Task<TResultSet> DownloadResultSet<TResultSet, TResult>(string searchResultFileName, string batchFolder = null)
            where TResultSet : ResultSet<TResult>
            where TResult : Result;
    }

    public class DebugResultsDownloader : IDebugResultsDownloader
    {
        private readonly string searchResultBlobContainer;
        private readonly IBlobDownloader blobDownloader;

        public DebugResultsDownloader(string searchResultBlobContainer, IBlobDownloader blobDownloader)
        {
            this.searchResultBlobContainer = searchResultBlobContainer;
            this.blobDownloader = blobDownloader;
        }

        public async Task<TResultSet> DownloadResultSet<TResultSet, TResult>(string searchResultFileName, string batchFolder = null)
            where TResultSet : ResultSet<TResult> 
            where TResult : Result
        {
            var resultSet = await DownloadResultSetFile<TResultSet>(searchResultFileName);

            resultSet.Results ??= !string.IsNullOrEmpty(batchFolder)
                ? await blobDownloader.DownloadFolderContents<TResult>(searchResultBlobContainer, batchFolder)
                : new List<TResult>();

            return resultSet;
        }

        private async Task<TResultSet> DownloadResultSetFile<TResultSet>(string searchResultFileName)
        {
            return await blobDownloader.Download<TResultSet>(searchResultBlobContainer, searchResultFileName);
        }
    }
}