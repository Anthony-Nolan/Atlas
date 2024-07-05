using System.Linq;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;

namespace Atlas.MatchingAlgorithm.Helpers
{
    public static class SearchTrackingEventHelper
    {
        public static string GetSearchCriteria(SearchRequest searchRequest)
        {
            var mismatchCount = searchRequest.MatchCriteria.DonorMismatchCount;

            var lociSearched = searchRequest.MatchCriteria.LocusMismatchCriteria.ToLociInfo()
                .ToEnumerable().Count(x => x.HasValue) * 2;

            return (lociSearched - mismatchCount).ToString() + '/' + lociSearched;
        }
    }
}
