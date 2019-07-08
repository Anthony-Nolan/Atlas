using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Client.Models.SearchRequests
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
        /// List of donor registries to search.
        /// </summary>
        public IEnumerable<RegistryCode> RegistriesToSearch { get; set; }
    }
}
