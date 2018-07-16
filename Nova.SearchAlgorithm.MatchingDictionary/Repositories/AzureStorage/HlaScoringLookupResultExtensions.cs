using Newtonsoft.Json;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using System;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    internal static class HlaScoringLookupResultExtensions
    {
        internal static HlaScoringLookupTableEntity ToTableEntity(this IHlaScoringLookupResult lookupResult)
        {
            return new HlaScoringLookupTableEntity(lookupResult.MatchLocus, lookupResult.LookupName, lookupResult.TypingMethod)
            {
                HlaTypingCategoryAsString = lookupResult.HlaTypingCategory.ToString(),
                SerialisedHlaInfo = JsonConvert.SerializeObject(lookupResult.HlaScoringInfo)
            };
        }

        internal static IHlaScoringLookupResult ToHlaScoringLookupResult(this HlaScoringLookupTableEntity entity)
        {
            var scoringInfo = GetPreCalculatedScoringInfo(entity);

            return new HlaScoringLookupResult(
                entity.MatchLocus,
                entity.LookupName,
                entity.TypingMethod,
                entity.HlaTypingCategory,
                scoringInfo);
        }

        private static IHlaScoringInfo GetPreCalculatedScoringInfo(HlaScoringLookupTableEntity entity)
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

        private static IHlaScoringInfo GetScoringInfo<TScoringInfo>(HlaScoringLookupTableEntity entity)
            where TScoringInfo : IHlaScoringInfo
        {
            return JsonConvert.DeserializeObject<TScoringInfo>(entity.SerialisedHlaInfo);
        }
    }
}