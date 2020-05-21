using System;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;

namespace Atlas.HlaMetadataDictionary.Extensions
{
    public static class HlaScoringLookupResultExtensions
    {
        public static IHlaScoringLookupResult ToHlaScoringLookupResult(this HlaLookupTableEntity entity)
        {
            var scoringInfo = GetPreCalculatedScoringInfo(entity);

            return new HlaScoringLookupResult(
                entity.Locus,
                entity.LookupName,
                entity.HlaTypingCategory,
                scoringInfo);
        }

        private static IHlaScoringInfo GetPreCalculatedScoringInfo(HlaLookupTableEntity entity)
        {
            switch (entity.HlaTypingCategory)
            {
                case HlaTypingCategory.Serology:
                    return entity.GetHlaInfo<SerologyScoringInfo>();
                case HlaTypingCategory.Allele:
                    return entity.GetHlaInfo<SingleAlleleScoringInfo>();
                case HlaTypingCategory.NmdpCode:
                    return entity.GetHlaInfo<MultipleAlleleScoringInfo>();
                case HlaTypingCategory.XxCode:
                    return entity.GetHlaInfo<ConsolidatedMolecularScoringInfo>();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}