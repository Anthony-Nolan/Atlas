using FluentValidation;

namespace Nova.SearchAlgorithm.Client.Models
{
    public class MatchCriteria
    {
        /// <summary>
        /// Number of mismatches permitted per tier1 donor
        /// </summary>
        DonorMismatchCriteria DonorMismatchTier1 { get; set; }

        /// <summary>
        /// Number of mismatches permitted per tier2 donor
        /// </summary>
        DonorMismatchCriteria DonorMismatchTier2 { get; set; }

        /// <summary>
        /// Mismatch preferences for locus HLA-A
        /// </summary>
        LocusMismatchCriteria LocusMismatchA { get; set; }

        /// <summary>
        /// Mismatch preferences for locus HLA-B
        /// </summary>
        LocusMismatchCriteria LocusMismatchB { get; set; }

        /// <summary>
        /// Mismatch preferences for locus HLA-C
        /// </summary>
        LocusMismatchCriteria LocusMismatchC { get; set; }

        /// <summary>
        /// Mismatch preferences for locus HLA-DQB1
        /// </summary>
        LocusMismatchCriteria LocusMismatchDQB1 { get; set; }

        /// <summary>
        /// Mismatch preferences for locus HLA-DRB1
        /// </summary>
        LocusMismatchCriteria LocusMismatchDRB1 { get; set; }
    }
}
