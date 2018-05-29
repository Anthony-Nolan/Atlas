using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;

namespace Nova.SearchAlgorithm.Services
{
    public static class MatchingLookupResultExtensions
    {
        public static ExpandedHla ToExpandedHla(this IMatchingHlaLookupResult lookupResult)
        {
            return new ExpandedHla
            {
                Name = lookupResult.LookupName,
                Locus = lookupResult.MatchLocus.ToLocus(),
                PGroups = lookupResult.MatchingPGroups
            };
        }
    }
}