namespace Atlas.RepeatSearch.Data.Settings
{
    public class StoreOriginalSearchResultsBulkCopySettings
    {
        /// <summary>
        /// Number of rows that SqlBulkCopy sends and, with its internal transaction, commits at a time.
        /// </summary>
        public int BatchSize { get; set; }

        /// <summary>
        /// Timeout in seconds of the SqlBulkCopy of each chunk of an original search result set.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Number of donor codes in each chunk of an original search result set: how many donor codes are held in memory at once.
        /// Each chunk is inserted with its own SqlBulkCopy, which commits every <see cref="BatchSize"/> rows. Keep this equal to
        /// <see cref="BatchSize"/> (both 10,000 by default), so that each chunk is one batch and one commit.
        /// </summary>
        public int ChunkSize { get; set; }
    }
}
