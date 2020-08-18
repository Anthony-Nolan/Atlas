using Atlas.Client.Models.Search.Requests;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;

namespace Atlas.MatchPrediction.ExternalInterface.Models
{
    public static class SearchRequestMappings
    {
        public static SingleDonorMatchProbabilityInput ToPartialMatchProbabilitySearchRequest(this SearchRequest searchRequest)
        {
            return new SingleDonorMatchProbabilityInput()
            {
                PatientHla = searchRequest.SearchHlaData,
                ExcludedLoci = searchRequest.ScoringCriteria?.LociToExcludeFromAggregateScore
            };
        }
    }
}