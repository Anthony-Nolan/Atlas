namespace Atlas.RepeatSearch.Data.Settings
{
    public class StoreOriginalSearchResultsBulkCopySettings
    {
        public int BatchSize { get; set; }

        /// <summary>
        /// Timeout in seconds of each SQL command that stores original search results: one per chunk.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Number of donor codes in each chunk of an original search result set. Each chunk is read, inserted and committed on its own,
        /// so this is how many donor codes are held in memory at once.
        /// </summary>
        public int ChunkSize { get; set; }
    }
}
