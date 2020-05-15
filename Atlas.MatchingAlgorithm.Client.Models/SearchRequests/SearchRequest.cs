using System.Collections.Generic;
using Atlas.Common.GeneticData;

namespace Atlas.MatchingAlgorithm.Client.Models.SearchRequests
{
    public class SearchRequest
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
        /// Search HLA to search on at various loci.
        /// Even if locus should not be used for matching (no match criteria provided),
        /// the hla data should still be provided if possible for use in scoring results
        /// </summary>
        public SearchHlaData SearchHlaData { get; set; }
        
        /// <summary>
        /// By default the algorithm will use scoring information available at all loci to aggregate into some overall values to use for ranking.
        /// e.g. MatchCategory, GradeScore, ConfidenceScore
        /// Any loci specified here can be excluded from these aggregates.
        /// </summary>
        public IEnumerable<LocusType> LociToExcludeFromAggregateScore { get; set; }
    }
}
