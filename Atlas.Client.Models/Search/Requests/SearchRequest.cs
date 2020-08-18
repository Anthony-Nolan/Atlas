using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable MemberCanBeInternal
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Atlas.Client.Models.Search.Requests
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