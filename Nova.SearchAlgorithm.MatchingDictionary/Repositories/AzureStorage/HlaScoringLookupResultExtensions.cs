using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using System;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    internal static class HlaScoringLookupResultExtensions
    {
        internal static HlaLookupTableEntity ToTableEntity(this IHlaScoringLookupResult lookupResult)
        {
            return new HlaLookupTableEntity(lookupResult, lookupResult.HlaScoringInfo)
            {
                LookupCategoryAsString = lookupResult.LookupCategory.ToString()
            };
        }

        internal static IHlaScoringLookupResult ToHlaScoringLookupResult(this HlaLookupTableEntity entity)
        {
            var scoringInfo = GetPreCalculatedScoringInfo(entity);

            return new HlaScoringLookupResult(
                entity.MatchLocus,
                entity.LookupName,
                entity.TypingMethod,
                entity.LookupCategory,
                scoringInfo);
        }

        private static IHlaScoringInfo GetPreCalculatedScoringInfo(HlaLookupTableEntity entity)
        {
            switch (entity.LookupCategory)
            {
                case LookupCategory.Serology:
                    return entity.GetHlaInfo<SerologyScoringInfo>();
                case LookupCategory.OriginalAllele:
                    return entity.GetHlaInfo<SingleAlleleScoringInfo>();
                case LookupCategory.NmdpCodeAllele:
                    return entity.GetHlaInfo<MultipleAlleleScoringInfo>();
                case LookupCategory.XxCode:
                    return entity.GetHlaInfo<XxCodeScoringInfo>();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}