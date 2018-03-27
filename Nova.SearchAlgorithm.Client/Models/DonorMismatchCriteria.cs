namespace Nova.SearchAlgorithm.Client.Models
{
    public class DonorMismatchCriteria
    {
        /// <summary>
        /// Total number of mismatches permitted
        /// </summary>
        public int TotalMismatch { get; set; }

        /// <summary>
        /// Number of antigen mismatches permitted
        /// </summary>
        public int AntigenMismatch { get; set; }

        /// <summary>
        /// Minimum number of matches required per donor when 'mixed' counting
        /// </summary>
        public int? MixedCountCutoff { get; set; }
    }
}