using Newtonsoft.Json;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    internal static class HlaScoringLookupResultExtensions
    {
        internal static HlaLookupTableEntity ToTableEntity(this IHlaScoringLookupResult lookupResult)
        {
            return new HlaLookupTableEntity(lookupResult.MatchLocus, lookupResult.LookupName, lookupResult.TypingMethod)
            {
                SerialisedHlaInfo = JsonConvert.SerializeObject(lookupResult.HlaScoringInfo)
            };
        }

        internal static IHlaScoringLookupResult ToHlaScoringLookupResult(this HlaLookupTableEntity entity)
        {
            var scoringInfo = entity.TypingMethod == TypingMethod.Molecular
                ? GetScoringInfo<AlleleScoringInfo>(entity)
                : GetScoringInfo<SerologyScoringInfo>(entity);

            return new HlaScoringLookupResult(entity.MatchLocus, entity.LookupName, entity.TypingMethod, scoringInfo);
        }

        private static IHlaScoringInfo GetScoringInfo<TScoringInfo>(HlaLookupTableEntity entity)
            where TScoringInfo : IHlaScoringInfo
        {
            return JsonConvert.DeserializeObject<TScoringInfo>(entity.SerialisedHlaInfo);
        }
    }
}