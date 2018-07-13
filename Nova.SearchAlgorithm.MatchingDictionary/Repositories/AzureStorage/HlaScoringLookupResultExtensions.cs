using Newtonsoft.Json;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary.ScoringLookup;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    internal static class HlaScoringLookupResultExtensions
    {
        internal static HlaLookupTableEntity ToTableEntity<TScoringInfo>(this IHlaScoringLookupResult<TScoringInfo> lookupResult)
            where TScoringInfo : IPreCalculatedScoringInfo
        {
            return new HlaLookupTableEntity(lookupResult.MatchLocus, lookupResult.LookupName, lookupResult.TypingMethod)
            {
                SerialisedHlaInfo = JsonConvert.SerializeObject(lookupResult.PreCalculatedHlaInfo)
            };
        }

        internal static IHlaScoringLookupResult<IPreCalculatedScoringInfo> ToHlaScoringLookupResult(this HlaLookupTableEntity entity)
        {
            var scoringInfo = entity.TypingMethod == TypingMethod.Molecular
                ? GetScoringInfo<AlleleScoringInfo>(entity)
                : GetScoringInfo<SerologyScoringInfo>(entity);

            return new HlaScoringLookupResult<IPreCalculatedScoringInfo>(entity.MatchLocus, entity.LookupName, scoringInfo);
        }

        private static IPreCalculatedScoringInfo GetScoringInfo<TScoringInfo>(HlaLookupTableEntity entity)
            where TScoringInfo : IPreCalculatedScoringInfo
        {
            return JsonConvert.DeserializeObject<TScoringInfo>(entity.SerialisedHlaInfo);
        }
    }
}