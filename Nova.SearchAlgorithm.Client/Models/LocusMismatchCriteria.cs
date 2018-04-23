namespace Nova.SearchAlgorithm.Client.Models
{
    public class LocusMismatchCriteria
    {
        /// <summary>
        /// String representation of the 1st search HLA type position at this locus.
        /// </summary>
        public string SearchHla1 { get; set; }

        /// <summary>
        /// String representation of the 2nd search HLA type position at this locus.
        /// </summary>
        public string SearchHla2 { get; set; }

        /// <summary>
        /// Total number of mismatches permitted, either 0, 1 or 2.
        /// </summary>
        public int MismatchCount { get; set; }
    }
}