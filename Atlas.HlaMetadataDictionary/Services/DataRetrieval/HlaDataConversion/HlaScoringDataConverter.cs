using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Extensions;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.HlaDataConversion
{
    /// <summary>
    /// Converts Matched HLA to model optimised for HLA Scoring lookups.
    /// </summary>
    internal interface IHlaScoringDataConverter : IMatchedHlaDataConverterBase
    {
    }

    internal class HlaScoringDataConverter :
        MatchedHlaDataConverterBase,
        IHlaScoringDataConverter
    {
        protected override ISerialisableHlaMetadata GetSerologyLookupResult(
            IHlaLookupResultSource<SerologyTyping> lookupResultSource)
        {
            var scoringInfo = SerologyScoringInfo.GetScoringInfo(lookupResultSource);

            return new HlaScoringLookupResult(
                lookupResultSource.TypingForHlaLookupResult.Locus,
                lookupResultSource.TypingForHlaLookupResult.Name,
                scoringInfo,
                TypingMethod.Serology
            );
        }

        protected override ISerialisableHlaMetadata GetSingleAlleleLookupResult(
            IHlaLookupResultSource<AlleleTyping> lookupResultSource)
        {
            return GetMolecularLookupResult(
                new[] { lookupResultSource },
                allele => allele.Name,
                sources => SingleAlleleScoringInfo.GetScoringInfoWithMatchingSerologies(sources.First()));
        }

        protected override ISerialisableHlaMetadata GetNmdpCodeAlleleLookupResult(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> lookupResultSources,
            string nmdpLookupName)
        {
            return GetMolecularLookupResult(
                lookupResultSources,
                allele => nmdpLookupName,
                MultipleAlleleScoringInfo.GetScoringInfo);
        }

        protected override ISerialisableHlaMetadata GetXxCodeLookupResult(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> lookupResultSources)
        {
            return GetMolecularLookupResult(
                lookupResultSources,
                allele => allele.ToXxCodeLookupName(),
                ConsolidatedMolecularScoringInfo.GetScoringInfo);
        }

        private static HlaScoringLookupResult GetMolecularLookupResult(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> lookupResultSources,
            Func<AlleleTyping, string> getLookupName,
            Func<IEnumerable<IHlaLookupResultSource<AlleleTyping>>, IHlaScoringInfo> getScoringInfo)
        {
            var sources = lookupResultSources.ToList();

            var firstAllele = sources
                .First()
                .TypingForHlaLookupResult;

            return new HlaScoringLookupResult(
                firstAllele.Locus,
                getLookupName(firstAllele),
                getScoringInfo(sources),
                TypingMethod.Molecular
            );
        }
    }
}
