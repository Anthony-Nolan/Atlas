using Newtonsoft.Json;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary.ScoringLookup;
using System;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    internal static class HlaScoringLookupResultExtensions
    {
        internal static HlaLookupTableEntity ToTableEntity(this IHlaScoringLookupResult lookupResult)
        {
            return new HlaLookupTableEntity(lookupResult.MatchLocus, lookupResult.LookupName, lookupResult.TypingMethod)
            {
                HlaTypingCategory = lookupResult.HlaTypingCategory,
                SerialisedHlaInfo = JsonConvert.SerializeObject(lookupResult.PreCalculatedHlaInfo)
            };
        }

        internal static IHlaScoringLookupResult ToHlaScoringLookupResult(this HlaLookupTableEntity entity)
        {
            var scoringInfo = GetPreCalculatedScoringInfo(entity);

            return new HlaScoringLookupResult(
                entity.MatchLocus,
                entity.LookupName,
                entity.HlaTypingCategory,
                scoringInfo);
        }

        private static IPreCalculatedScoringInfo GetPreCalculatedScoringInfo(HlaLookupTableEntity entity)
        {
            switch (entity.HlaTypingCategory)
            {
                case HlaTypingCategory.Allele:
                    return GetScoringInfo<SingleAlleleScoringInfo>(entity);
                case HlaTypingCategory.Serology:
                    return GetScoringInfo<SerologyScoringInfo>(entity);
                case HlaTypingCategory.XxCode:
                    return GetScoringInfo<XxCodeScoringInfo>(entity);
                case HlaTypingCategory.AlleleStringOfNames:
                case HlaTypingCategory.AlleleStringOfSubtypes:
                case HlaTypingCategory.NmdpCode:
                    return GetScoringInfo<AlleleStringScoringInfo>(entity);
                default:
                    throw new NotImplementedException();
            }
        }

        private static IPreCalculatedScoringInfo GetScoringInfo<TScoringInfo>(HlaLookupTableEntity entity)
            where TScoringInfo : IPreCalculatedScoringInfo
        {
            return JsonConvert.DeserializeObject<TScoringInfo>(entity.SerialisedHlaInfo);
        }
    }
}