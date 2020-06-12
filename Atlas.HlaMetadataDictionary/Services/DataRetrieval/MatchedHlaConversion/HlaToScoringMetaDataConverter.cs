using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.MatchedHlaConversion
{
    /// <summary>
    /// Converts Matched HLA to model optimised for HLA Scoring lookups.
    /// </summary>
    internal interface IHlaToScoringMetaDataConverter : IMatchedHlaToMetaDataConverterBase
    {
    }

    internal class HlaToScoringMetaDataConverter :
        MatchedHlaToMetaDataConverterBase,
        IHlaToScoringMetaDataConverter
    {
        protected override ISerialisableHlaMetadata GetSerologyMetadata(
            IHlaMetadataSource<SerologyTyping> metadataSource)
        {
            var scoringInfo = SerologyScoringInfo.GetScoringInfo(metadataSource);

            return new HlaScoringMetadata(
                metadataSource.TypingForHlaMetadata.Locus,
                metadataSource.TypingForHlaMetadata.Name,
                scoringInfo,
                TypingMethod.Serology
            );
        }

        protected override ISerialisableHlaMetadata GetSingleAlleleMetadata(
            IHlaMetadataSource<AlleleTyping> metadataSource)
        {
            return GetMolecularMetadata(
                new[] { metadataSource },
                allele => allele.Name,
                sources => SingleAlleleScoringInfo.GetScoringInfoWithMatchingSerologies(sources.First()));
        }

        protected override ISerialisableHlaMetadata GetNmdpCodeAlleleMetadata(
            IEnumerable<IHlaMetadataSource<AlleleTyping>> metadataSources,
            string nmdpLookupName)
        {
            return GetMolecularMetadata(
                metadataSources,
                allele => nmdpLookupName,
                MultipleAlleleScoringInfo.GetScoringInfo);
        }

        protected override ISerialisableHlaMetadata GetXxCodeMetadata(
            IEnumerable<IHlaMetadataSource<AlleleTyping>> metadataSources)
        {
            return GetMolecularMetadata(
                metadataSources,
                allele => allele.ToXxCodeLookupName(),
                ConsolidatedMolecularScoringInfo.GetScoringInfo);
        }

        private static HlaScoringMetadata GetMolecularMetadata(
            IEnumerable<IHlaMetadataSource<AlleleTyping>> metadataSources,
            Func<AlleleTyping, string> getLookupName,
            Func<IEnumerable<IHlaMetadataSource<AlleleTyping>>, IHlaScoringInfo> getScoringInfo)
        {
            var sources = metadataSources.ToList();

            var firstAllele = sources
                .First()
                .TypingForHlaMetadata;

            return new HlaScoringMetadata(
                firstAllele.Locus,
                getLookupName(firstAllele),
                getScoringInfo(sources),
                TypingMethod.Molecular
            );
        }
    }
}
