namespace Atlas.MatchingAlgorithm.Settings
{
    public class MatchingConfigurationSettings
    {
        /// <summary>
        /// The matching process streams donors from the first matching locus, then reifies results in batches to create queries for other loci.
        /// The batch size is a balance between performance and memory usage - a larger batch size will result in quicker searches, at the expense of using more memory. 
        /// </summary>
        public int MatchingBatchSize { get; set; }
    }
}