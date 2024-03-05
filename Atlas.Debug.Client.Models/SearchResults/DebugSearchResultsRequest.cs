namespace Atlas.Debug.Client.Models.SearchResults
{
    /// <summary>
    /// Request to retrieve search results for debugging purposes.
    /// </summary>
    public class DebugSearchResultsRequest
    {
        /// <summary>
        /// Blob container containing <see cref="BatchFolderName"/> and <see cref="SearchResultFileName"/>.
        /// </summary>
        public string SearchResultBlobContainer { get; set; }

        /// <summary>
        /// Search result file name.
        /// For un-batched results, this file contains both the search summary and matched donor list.
        /// For batched results, this file will only contain the search summary - matched donors will be retrieved from <see cref="BatchFolderName"/>.
        /// </summary>
        public string SearchResultFileName { get; set; }

        /// <summary>
        /// Name of folder containing search results, if the results were batched.
        /// </summary>
        public string BatchFolderName { get; set; }
    }
}