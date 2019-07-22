namespace Nova.SearchAlgorithm.Client.Models.SearchRequests
{
    public class LocusMismatchCriteria
    {
        /// <summary>
        /// Total number of mismatches permitted, either 0, 1 or 2.
        /// </summary>
        public int MismatchCount { get; set; }
    }
}