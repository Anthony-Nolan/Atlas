using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;

namespace Atlas.Client.Models.Search.Results.Matching
{
    public class MatchingAlgorithmResult : IResult
    {
        public MatchingResult MatchingResult { get; set; }

        public ScoringResult ScoringResult { get; set; }

        /// <summary>
        /// The ATLAS ID of the donor for lookup in donor registries.
        /// </summary>
        public int AtlasDonorId { get; set; }

        /// <summary>
        /// The External Donor Code (possibly referred to as an ID) of the donor.
        /// This will match the id for a donor provided by a consumer at the time of donor import.
        /// </summary>
        public string DonorCode { get; set; }

        /// <summary>
        ///     The type of donor, for example Adult or Cord.
        /// </summary>
        public DonorType DonorType { get; set; }
    }

    public class MatchingResult
    {
        /// <summary>
        ///     The number of loci matched, down to the type.
        ///     Out of a maximum of 10.
        ///     Should some loci be untyped, then this field reflects the potential match count, rather than the actual known match count.
        ///     This will only count loci specified in the search request - so if search criteria were only given for A,B,DRB1 - then any
        ///     matches at C, DQB1, DPB1 will not be recorded here.
        /// </summary>
        public int TotalMatchCount { get; set; }

        /// <summary>
        ///     The donor HLA at the time the search was run.
        ///     Useful in two cases:
        ///     - when further analysis is performed on results (e.g. match prediction), and we must ensure the same HLA is used as for matching
        ///     - when donor details are updated between running a search and viewing the results.
        /// </summary>
        public PhenotypeInfoTransfer<string> DonorHla { get; set; }

        /// <summary>
        ///     The number of loci which are typed for this donor.
        ///     This will be calculated for all loci
        /// </summary>
        public int? TypedLociCount { get; set; }
    }

    public class ScoringResult
    {
        /// <summary>
        ///     The number of loci matched, down to the type.
        ///     Out of a maximum of 10.
        ///     Should some loci be untyped, then this field reflects the potential match count, rather than the actual known match count.
        ///     This will only count loci included in the Scored Loci list of the search request <see cref="Search.Requests.ScoringCriteria"/>
        /// </summary>
        public int TotalMatchCount { get; set; }

        /// <summary>
        ///     The overall quality of the match. An aggregate of the per-locus grades and confidences.
        /// </summary>
        public MatchCategory? MatchCategory { get; set; }

        /// <summary>
        ///     A numeric value representing the aggregate relative match grade across all scored loci, according to the scoring algorithm
        /// </summary>
        public int? GradeScore { get; set; }

        /// <summary>
        ///     A numeric value representing the aggregate relative match confidence across all scored loci, according to the scoring algorithm
        /// </summary>
        public int? ConfidenceScore { get; set; }

        /// <summary>
        ///     The number of loci which are typed for this donor.
        ///     Loci excluded from scoring and aggregation will not be included, regardless of whether they are typed.
        /// </summary>
        public int? TypedLociCountAtScoredLoci { get; set; }

        /// <summary>
        ///     The number of the total potential matches.
        ///     This will only count loci specified in the search request
        /// </summary>
        public int PotentialMatchCount { get; set; }

        /// <summary>
        ///     The number of the total exact matches.
        ///     The <see cref="TotalMatchCount"/> is a sum of potential and exact matches, so an exact match count can be calculated as the difference of these values.
        /// </summary>
        public int ExactMatchCount => TotalMatchCount - PotentialMatchCount;

        /// <summary>
        /// The details of the match by locus
        /// </summary>
        /// <remarks>
        /// The results at C, DPB1 and DQB1 will be populated even if those loci were excluded from aggregate scoring.
        /// </remarks>
        public LociInfoTransfer<LocusSearchResult> ScoringResultsByLocus { get; set; }
    }
}