using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using System;

namespace Atlas.HlaMetadataDictionary.Repositories.AzureStorage
{
    public static class HlaScoringLookupResultExtensions
    {
        public static IHlaScoringLookupResult ToHlaScoringLookupResult(this HlaLookupTableEntity entity)
        {
            var scoringInfo = GetPreCalculatedScoringInfo(entity);

            return new HlaScoringLookupResult(
                entity.Locus,
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