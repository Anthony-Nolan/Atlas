using FluentValidation;

namespace Nova.SearchAlgorithm.Client.Models
{
    public class MatchCriteria
    {
        /// <summary>
        /// Number of mismatches permitted per tier1 donor
        /// </summary>
        public DonorMismatchCriteria DonorMismatchTier1 { get; set; }

        /// <summary>
        /// Number of mismatches permitted per tier2 donor
        /// </summary>
        public DonorMismatchCriteria DonorMismatchTier2 { get; set; }

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
