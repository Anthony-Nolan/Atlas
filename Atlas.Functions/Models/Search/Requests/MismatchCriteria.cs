﻿using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.Functions.Models.Search.Requests
{
    public class MismatchCriteria
    {
        /// <summary>
        /// Number of mismatches permitted per donor.
        /// Required.
        /// </summary>
        public int DonorMismatchCount { get; set; }

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
    
    public class LocusMismatchCriteria
    {
        /// <summary>
        /// Total number of mismatches permitted, either 0, 1 or 2.
        /// </summary>
        public int MismatchCount { get; set; }
    }

    public static class MismatchCriteriaMappings
    {
        public static MatchingAlgorithm.Client.Models.SearchRequests.MismatchCriteria ToMatchingAlgorithmMatchCriteria(
            this MismatchCriteria mismatchCriteria)
        {
            return new MatchingAlgorithm.Client.Models.SearchRequests.MismatchCriteria
            {
                DonorMismatchCount = mismatchCriteria.DonorMismatchCount,
                LocusMismatchCounts = new LociInfo<int?>
                {
                    A = mismatchCriteria.LocusMismatchA?.MismatchCount,
                    B = mismatchCriteria.LocusMismatchB?.MismatchCount,
                    C = mismatchCriteria.LocusMismatchC?.MismatchCount,
                    Dqb1 = mismatchCriteria.LocusMismatchDqb1?.MismatchCount,
                    Drb1 = mismatchCriteria.LocusMismatchDrb1?.MismatchCount,
                }
            };
        }
    }
}
