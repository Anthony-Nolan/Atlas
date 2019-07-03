namespace Nova.SearchAlgorithm.Client.Models.SearchRequests
{
    public class MismatchCriteria
    {
        /// <summary>
        /// Number of mismatches permitted per donor.
        /// Required.
        /// </summary>
        public int? DonorMismatchCount { get; set; }

        /// <summary>
        /// Mismatch preferences for HLA at locus A.
        /// Required.
        /// </summary>
        public LocusMismatchCriteria LocusMismatchA { get; set; }
        
        /// <summary>
        /// Mismatch preferences for HLA at locus B.
        /// Required.
        /// </summary>
        public LocusMismatchCriteria LocusMismatchB { get; set; }

        /// <summary>
        /// Mismatch preferences for HLA at locus C.
        /// Optional.
        /// </summary>
        public LocusMismatchCriteria LocusMismatchC { get; set; }

        /// <summary>
        /// Mismatch preferences for HLA at locus DQB1.
        /// Optional.
        /// </summary>
        public LocusMismatchCriteria LocusMismatchDqb1 { get; set; }

        /// <summary>
        /// Mismatch preferences for HLA at locus DRB1.
        /// Required.
        /// </summary>
        public LocusMismatchCriteria LocusMismatchDrb1 { get; set; }
    }
}
