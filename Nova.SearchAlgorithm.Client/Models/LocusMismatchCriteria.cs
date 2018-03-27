namespace Nova.SearchAlgorithm.Client.Models
{
    public class LocusMismatchCriteria
    {
        /// <summary>
        /// String representation of the 1st search HLA type
        /// </summary>
        public string SearchHla1 { get; set; }

        /// <summary>
        /// String representation of the 2nd search HLA type
        /// </summary>
        public string SearchHla2 { get; set; }

        /// <summary>
        /// Should matches at this locus be counted at antigen level?
        /// </summary>
        public bool IsAntigenLevel { get; set; }

        /// <summary>
        /// Total number of mismatches permitted
        /// </summary>
        public int TotalMismatch { get; set; }

        /// <summary>
        /// Number of antigen mismatches permitted
        /// </summary>
        public int AntigenMismatch { get; set; }
    }
}