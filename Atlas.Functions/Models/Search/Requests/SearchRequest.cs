using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;

namespace Atlas.Functions.Models.Search.Requests
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
        /// Information related to scoring of the matched donors.
        /// </summary>
        public ScoringCriteria ScoringCriteria { get; set; }

        /// <summary>
        /// Search HLA to search on at various loci.
        /// Even if locus should not be used for matching (no match criteria provided),
        /// the hla data should still be provided if possible for use in scoring results
        /// </summary>
        public SearchHlaData SearchHlaData { get; set; }

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

    public static class SearchRequestMappings
    {
        public static MatchingRequest ToMatchingRequest(this SearchRequest searchRequest)
        {
            return new MatchingRequest
            {
                SearchType = searchRequest.SearchType.ToMatchingAlgorithmDonorType(),
                MatchCriteria = searchRequest.MatchCriteria?.ToMatchingAlgorithmMatchCriteria(),
                ScoringCriteria = searchRequest.ScoringCriteria?.ToMatchingAlgorithmScoringCriteria(),
                SearchHlaData = searchRequest.SearchHlaData?.ToPhenotypeInfo().ToPhenotypeInfoTransfer()
            };
        }

        /// <summary>
        /// This method generates a partial match probability search request, to be used to validate the input for the match prediction before the matching algorithm has been run
        /// This shouldn't be used to run real requests!
        /// </summary>
        public static SingleDonorMatchProbabilityInput ToPartialMatchProbabilitySearchRequest(this SearchRequest searchRequest)
        {
            return new SingleDonorMatchProbabilityInput()
            {
                PatientHla = searchRequest.SearchHlaData?.ToPhenotypeInfo().ToPhenotypeInfoTransfer(),
                ExcludedLoci = searchRequest.ScoringCriteria?.LociToExcludeFromAggregateScore
            };
        }
    }
}