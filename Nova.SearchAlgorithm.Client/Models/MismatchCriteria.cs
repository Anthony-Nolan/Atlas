using FluentValidation;

namespace Nova.SearchAlgorithm.Client.Models
{
    public class MismatchCriteria
    {
        /// <summary>
        /// Number of mismatches permitted per donor.
        /// Required.
        /// </summary>
        public int DonorMismatchCount { get; set; }

        /// <summary>
        /// Search HLA and mismatch preferences for locus HLA-A.
        /// Required.
        /// </summary>
        public LocusMismatchCriteria LocusMismatchA { get; set; }

        /// <summary>
        /// Search HLA and mismatch preferences for locus HLA-B.
        /// Required.
        /// </summary>
        public LocusMismatchCriteria LocusMismatchB { get; set; }

        /// <summary>
        /// Search HLA and mismatch preferences for locus HLA-C.
        /// Optional.
        /// </summary>
        public LocusMismatchCriteria LocusMismatchC { get; set; }

        /// <summary>
        /// Search HLA and mismatch preferences for locus HLA-DQB1.
        /// Optional.
        /// </summary>
        public LocusMismatchCriteria LocusMismatchDQB1 { get; set; }

        /// <summary>
        /// Search HLA and mismatch preferences for locus HLA-DRB1.
        /// Required.
        /// </summary>
        public LocusMismatchCriteria LocusMismatchDRB1 { get; set; }
    }
}
