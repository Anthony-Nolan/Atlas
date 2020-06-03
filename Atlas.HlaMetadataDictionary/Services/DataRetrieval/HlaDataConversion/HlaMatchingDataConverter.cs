using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.HlaDataConversion
{
    /// <summary>
    /// Converts Matched HLA to model optimised for HLA Scoring lookups.
    /// </summary>
    internal interface IHlaMatchingDataConverter : IMatchedHlaDataConverterBase
    {
    }

    internal class HlaMatchingDataConverter :
        MatchedHlaDataConverterBase,
        IHlaMatchingDataConverter
    {
        protected override ISerialisableHlaMetadata GetSerologyMetadata(IHlaMetadataSource<SerologyTyping> metadataSource)
        {
            return new HlaMatchingMetadata(
                    metadataSource.TypingForHlaMetadata.Locus,
                    metadataSource.TypingForHlaMetadata.Name,
                    TypingMethod.Serology,
                    metadataSource.MatchingPGroups);
        }

        protected override ISerialisableHlaMetadata GetSingleAlleleMetadata(
            IHlaMetadataSource<AlleleTyping> metadataSource)
        {
            return GetMolecularMetadata(
                new[] { metadataSource },
                allele => allele.Name
            );
        }

        protected override ISerialisableHlaMetadata GetNmdpCodeAlleleMetadata(
            IEnumerable<IHlaMetadataSource<AlleleTyping>> metadataSources,
            string nmdpLookupName)
        {
            return GetMolecularMetadata(
                metadataSources,
                allele => nmdpLookupName
            );
        }

        protected override ISerialisableHlaMetadata GetXxCodeMetadata(
            IEnumerable<IHlaMetadataSource<AlleleTyping>> metadataSources)
        {
            return GetMolecularMetadata(
                metadataSources,
                allele => allele.ToXxCodeLookupName()
            );
        }
        
        private static ISerialisableHlaMetadata GetMolecularMetadata(
            IEnumerable<IHlaMetadataSource<AlleleTyping>> metadataSources,
            Func<AlleleTyping, string> getLookupName)
        {
            var sources = metadataSources.ToList();

            var firstAllele = sources
                .First()
                .TypingForHlaMetadata;

            var pGroups = sources
                .SelectMany(resultSource => resultSource.MatchingPGroups)
                .Distinct();

            return new HlaMatchingMetadata(
                firstAllele.Locus,
                getLookupName(firstAllele),
                TypingMethod.Molecular,
                pGroups
                );
        }
    }
}
