using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

namespace Atlas.MatchingAlgorithm.Client.Models.SearchRequests
{
    public class MatchingRequest
    {
        /// <summary>
        /// The type of donors to search, e.g. Adult or Cord.
        /// </summary>
        public DonorType SearchType { get; set; }

        /// <summary>
        /// Mismatch information including number of mismatches permitted per donor and per loci.
        /// </summary>
        public MismatchCriteria MatchCriteria { get; set; }

        /// <summary>
        /// Information related to scoring of the matched donors.
        /// </summary>
        public ScoringCriteria ScoringCriteria { get; set; }

        /// <summary>
        /// Search HLA to search on at supported loci.
        /// Even if locus should not be used for matching (no match criteria provided),
        /// the HLA data should still be provided if possible for use in scoring results.
        ///
        /// A, B, DRB1 required. Others optional - and should be provided as a null <see cref="LocusInfo{T}"/> if not present.
        /// </summary>
        public PhenotypeInfoTransfer<string> SearchHlaData { get; set; }
    }
}
