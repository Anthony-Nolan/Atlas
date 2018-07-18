using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    internal static class HlaScoringLookupResultExtensions
    {
        internal static HlaLookupTableEntity ToTableEntity(this IHlaScoringLookupResult lookupResult)
        {
            return new HlaLookupTableEntity(lookupResult, lookupResult.HlaScoringInfo)
            {
                HlaTypingCategoryAsString = lookupResult.HlaTypingCategory.ToString()
            };
        }

        internal static IHlaScoringLookupResult ToHlaScoringLookupResult(this HlaLookupTableEntity entity)
        {
            var scoringInfo = GetPreCalculatedScoringInfo(entity);

            return new HlaScoringLookupResult(
                entity.MatchLocus,
                entity.LookupName,
                entity.TypingMethod,
                entity.HlaTypingCategory,
                scoringInfo);
        }

        private static IHlaScoringInfo GetPreCalculatedScoringInfo(HlaLookupTableEntity entity)
        {
            switch (entity.HlaTypingCategory)
            {
                case HlaTypingCategory.Allele:
                    return GetHlaInfoForAlleleTyping(entity);
                case HlaTypingCategory.Serology:
                    return entity.GetHlaInfo<SerologyScoringInfo>();
                case HlaTypingCategory.XxCode:
                    return entity.GetHlaInfo<XxCodeScoringInfo>();
                case HlaTypingCategory.AlleleStringOfNames:
                case HlaTypingCategory.AlleleStringOfSubtypes:
                case HlaTypingCategory.NmdpCode:
                    return entity.GetHlaInfo<MultipleAlleleScoringInfo>();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static IHlaScoringInfo GetHlaInfoForAlleleTyping(HlaLookupTableEntity entity)
        {
            var alleleScoringInfos = entity.GetHlaInfo<IEnumerable<SingleAlleleScoringInfo>>().ToList();

            return alleleScoringInfos.Count == 1
                ? alleleScoringInfos.First() as IHlaScoringInfo
                : new MultipleAlleleScoringInfo(alleleScoringInfos);
        }
    }
}