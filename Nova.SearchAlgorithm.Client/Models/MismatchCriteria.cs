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
        /// Mismatch preferences for locus HLA-A
        /// </summary>
        public LocusMismatchCriteria LocusMismatchA { get; set; }

        /// <summary>
        /// Mismatch preferences for locus HLA-B
        /// </summary>
        public LocusMismatchCriteria LocusMismatchB { get; set; }

        /// <summary>
        /// Mismatch preferences for locus HLA-C
        /// </summary>
        public LocusMismatchCriteria LocusMismatchC { get; set; }

        /// <summary>
        /// Mismatch preferences for locus HLA-DQB1
        /// </summary>
        public LocusMismatchCriteria LocusMismatchDQB1 { get; set; }

        /// <summary>
        /// Mismatch preferences for locus HLA-DRB1
        /// </summary>
        public LocusMismatchCriteria LocusMismatchDRB1 { get; set; }
    }
}
