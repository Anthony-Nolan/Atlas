using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using System;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    internal static class HlaScoringLookupResultExtensions
    {
        internal static HlaLookupTableEntity ToTableEntity(this IHlaScoringLookupResult lookupResult)
        {
            return new HlaLookupTableEntity(lookupResult)
            {
                LookupNameCategoryAsString = lookupResult.LookupNameCategory.ToString()
            };
        }

        internal static HlaScoringLookupResult ToHlaScoringLookupResult(this HlaLookupTableEntity entity)
        {
            var scoringInfo = GetPreCalculatedScoringInfo(entity);

            return new HlaScoringLookupResult(
                entity.MatchLocus,
                entity.LookupName,
                entity.LookupNameCategory,
                scoringInfo);
        }

        private static IHlaScoringInfo GetPreCalculatedScoringInfo(HlaLookupTableEntity entity)
        {
            switch (entity.LookupNameCategory)
            {
                case LookupNameCategory.Serology:
                    return entity.GetHlaInfo<SerologyScoringInfo>();
                case LookupNameCategory.OriginalAllele:
                    return entity.GetHlaInfo<SingleAlleleScoringInfo>();
                case LookupNameCategory.NmdpCodeAllele:
                    return entity.GetHlaInfo<MultipleAlleleScoringInfo>();
                case LookupNameCategory.XxCode:
                    return entity.GetHlaInfo<ConsolidatedMolecularScoringInfo>();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}