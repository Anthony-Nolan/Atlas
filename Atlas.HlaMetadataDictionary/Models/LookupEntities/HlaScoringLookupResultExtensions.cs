using System;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using static Atlas.Common.Utils.Extensions.TypeExtensions;

namespace Atlas.HlaMetadataDictionary.Models.LookupEntities
{
    /// These are a bunch of extension methods on HlaLookupTableEntity, trying to work around the fact that Azure Storage
    /// doesn't support sub-typing at all nicely.
    internal static class HlaScoringLookupResultExtensions
    {
        public static IHlaScoringLookupResult ToHlaScoringLookupResult(this HlaLookupTableEntity entity)
        {
            var scoringInfo = entity.DeserialiseTypedScoringInfo();

            return new HlaScoringLookupResult(
                entity.Locus,
                entity.LookupName,
                scoringInfo,
                entity.TypingMethod);
        }

        // Alas "nameof(T)", which would be compile-time constant, and thus compatible with a switch, doesn't give values that always match with typeof(T).Name
        // So we have to calculate these ourselves.
        private static readonly string SerologyInfoType = GetNeatCSharpName<SerologyScoringInfo>();
        private static readonly string SingleAlleleInfoType = GetNeatCSharpName<SingleAlleleScoringInfo>();
        private static readonly string MultipleAlleleInfoType = GetNeatCSharpName<MultipleAlleleScoringInfo>();
        private static readonly string ConsolidatedMolecularInfoType = GetNeatCSharpName<ConsolidatedMolecularScoringInfo>();

        private static IHlaScoringInfo DeserialiseTypedScoringInfo(this HlaLookupTableEntity entity)
        {
            var infoTypeString = entity.SerialisedHlaInfoType;
            if (infoTypeString == SerologyInfoType)
            {
                return entity.GetHlaInfo<SerologyScoringInfo>();
            }
            if (infoTypeString == SingleAlleleInfoType)
            {
                return entity.GetHlaInfo<SingleAlleleScoringInfo>();
            }
            if (infoTypeString == MultipleAlleleInfoType)
            {
                return entity.GetHlaInfo<MultipleAlleleScoringInfo>();
            }
            if (infoTypeString == ConsolidatedMolecularInfoType)
            {
                return entity.GetHlaInfo<ConsolidatedMolecularScoringInfo>();
            }
            throw new ArgumentOutOfRangeException();
        }
    }
}