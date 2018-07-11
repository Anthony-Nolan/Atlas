using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;

namespace Nova.SearchAlgorithm.Client.Models
{
    public class SearchResult
    {
        /// <summary>
        /// The ID of the donor for lookup in donor registries.
        /// </summary>
        public int DonorId { get; set; }
        
        /// <summary>
        /// The type of donor, for example Adult or Cord.
        /// </summary>
        public DonorType DonorType { get; set; }

        /// <summary>
        /// The code of the donor registry which this donor originates from.
        /// </summary>
        public RegistryCode Registry { get; set; }

        /// <summary>
        /// The number of loci matched, down to the type.
        /// Out of a maximum of 10.
        /// Should some loci be untyped, then this field reflects the potential match count,
        /// rather than the actual known match count.
        /// </summary>
        public int TotalMatchCount { get; set; }

        /// <summary>
        /// The number of loci which are typed for this donor.
        /// </summary>
        public int TypedLociCount { get; set; }

        /// <summary>
        /// The relative rank of this match within the search results,
        /// based on the scoring algorithm.
        /// </summary>
        public int MatchRank { get; set; }

        /// <summary>
        /// The match grade according to the scoring algorithm,
        /// for validation and debugging.
        /// </summary>
        public int TotalMatchGrade { get; set; }

        /// <summary>
        /// The match confidence according to the scoring algorithm,
        /// for validation and debugging.
        /// </summary>
        public int TotalMatchConfidence { get; set; }

        /// <summary>
        /// The details of the match at locus A.
        /// </summary>
        public LocusSearchResult SearchResultAtLocusA { get; set; }

        /// <summary>
        /// The details of the match at locus B.
        /// </summary>
        public LocusSearchResult SearchResultAtLocusB { get; set; }

        /// <summary>
        /// The details of the match at locus C.
        /// </summary>
        public LocusSearchResult SearchResultAtLocusC { get; set; }

        /// <summary>
        /// The details of the match at locus DRB1.
        /// </summary>
        public LocusSearchResult SearchResultAtLocusDrb1 { get; set; }

        /// <summary>
        /// The details of the match at locus DQB1.
        /// </summary>
        public LocusSearchResult SearchResultAtLocusDqb1 { get; set; }
    }
}