using System;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using static Atlas.Common.Utils.Extensions.TypeExtensions;

namespace Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows
{
    /// These are a bunch of extension methods on HlaMetadataTableRow, trying to work around the fact that Azure Storage
    /// doesn't support sub-typing at all nicely.
    internal static class HlaScoringMetadataExtensions
    {
        public static IHlaScoringMetadata ToHlaScoringMetadata(this HlaMetadataTableRow row)
        {
            var scoringInfo = row.DeserialiseTypedScoringInfo();

            return new HlaScoringMetadata(
                row.Locus,
                row.LookupName,
                scoringInfo,
                row.TypingMethod);
        }

        // Alas "nameof(T)", which would be compile-time constant, and thus compatible with a switch, doesn't give values that always match with typeof(T).Name
        // So we have to calculate these ourselves.
        private static readonly string SerologyInfoType = GetNeatCSharpName<SerologyScoringInfo>();
        private static readonly string SingleAlleleInfoType = GetNeatCSharpName<SingleAlleleScoringInfo>();
        private static readonly string MultipleAlleleInfoType = GetNeatCSharpName<MultipleAlleleScoringInfo>();
        private static readonly string ConsolidatedMolecularInfoType = GetNeatCSharpName<ConsolidatedMolecularScoringInfo>();

        private static IHlaScoringInfo DeserialiseTypedScoringInfo(this HlaMetadataTableRow row)
        {
            var infoTypeString = row.SerialisedHlaInfoType;
            if (infoTypeString == SerologyInfoType)
            {
                return row.GetHlaInfo<SerologyScoringInfo>();
            }
            if (infoTypeString == SingleAlleleInfoType)
            {
                return row.GetHlaInfo<SingleAlleleScoringInfo>();
            }
            if (infoTypeString == MultipleAlleleInfoType)
            {
                return row.GetHlaInfo<MultipleAlleleScoringInfo>();
            }
            if (infoTypeString == ConsolidatedMolecularInfoType)
            {
                return row.GetHlaInfo<ConsolidatedMolecularScoringInfo>();
            }
            throw new ArgumentOutOfRangeException();
        }
    }
}