using System.Collections.Generic;
using Atlas.Client.Models.Search;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;

// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable MemberCanBeInternal
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Atlas.Client.Models.Common.Requests
{
    public class SearchRequest
    {
        /// <summary>
        /// The type of donors to search, e.g. Adult or Cord.
        /// </summary>
        public DonorType SearchDonorType { get; set; }

        /// <summary>
        /// Mismatch information including number of mismatches permitted per donor and per loci.
        /// </summary>
        public MismatchCriteria MatchCriteria { get; set; }

        /// <summary>
        /// Information related to scoring of the matched donors.
        /// </summary>
        public ScoringCriteria ScoringCriteria { get; set; }

        /// <summary>
        /// Search HLA to search on at various loci.
        /// Even if locus should not be used for matching (no match criteria provided),
        /// the hla data should still be provided if possible for use in results analysis
        /// </summary>
        public PhenotypeInfoTransfer<string> SearchHlaData { get; set; }

        /// <summary>
        /// Used to choose frequency data for use in match prediction.
        /// Must match ethnicity code format uploaded with haplotype frequency sets.
        /// </summary>
        public string PatientEthnicityCode { get; set; }

        /// <summary>
        /// Determines which haplotype frequency data to use for match prediction.
        /// Must match registry code format uploaded with haplotype frequency sets.
        /// </summary>
        public string PatientRegistryCode { get; set; }

        /// <summary>
        /// Optional, defaults to true.
        /// Allows consumer to optionally disable match prediction for search results - match prediction is the most computationally
        /// expensive and time consuming portion of a search, so if results are not needed, results can be returned much faster without it.
        /// </summary>
        public bool RunMatchPrediction { get; set; } = true;

        /// <summary>
        /// Optional.
        /// When non-null, only donors from the corresponding registry codes will be returned from the search.
        /// These registry codes must *exactly* match (case-sensitive) those provided in donor files used to import donors to Atlas.
        /// </summary>
        public List<string> DonorRegistryCodes { get; set; }
    }

    public class MismatchCriteria
    {
        /// <summary>
        /// Number of mismatches permitted per donor.
        /// Required.
        /// </summary>
        public int DonorMismatchCount { get; set; }

        /// <summary>
        /// Mismatch preferences for input HLA, in the form of a number of allowed mismatches - can be 0, 1, or 2.
        /// Loci A, B, DRB1 are required.
        /// </summary>
        public LociInfoTransfer<int?> LocusMismatchCriteria { get; set; }

        /// <summary>
        /// When set, the <see cref="DonorMismatchCount"/> will be treated as a minimum requirement for matches.
        /// e.g. When running a 9/10 search, allowing a single mismatch: any donors with no mismatches (10/10) will also be returned.
        ///
        /// Otherwise, <see cref="DonorMismatchCount"/> will be treated as an *exact* requirement.
        /// e.g. When running a 9/10 search, allowing a single mismatch: any donors with no mismatches (10/10) will *not* be returned.
        ///
        /// Defaults to true.
        /// </summary>
        public bool IncludeBetterMatches { get; set; } = true;
    }

    public class ScoringCriteria
    {
        /// <summary>
        /// By default, scoring is not performed on matched donor HLA, except on the loci specified here.
        /// </summary>
        public IReadOnlyCollection<Locus> LociToScore { get; set; }

        /// <summary>
        /// By default, the algorithm will use scoring information available at loci defined in <see cref="LociToScore"/>
        /// to aggregate into some overall values to use for ranking. e.g. MatchCategory, GradeScore, ConfidenceScore
        /// Any loci specified here can be excluded from these aggregates.
        /// </summary>
        public IReadOnlyCollection<Locus> LociToExcludeFromAggregateScore { get; set; }
    }
}