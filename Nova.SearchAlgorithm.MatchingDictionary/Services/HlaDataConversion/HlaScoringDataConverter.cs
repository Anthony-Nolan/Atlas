using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.HlaDataConversion
{
    /// <summary>
    /// Converts Matched HLA to model optimised for HLA Scoring lookups.
    /// </summary>
    public interface IHlaScoringDataConverter : IMatchedHlaDataConverterBase
    {
    }

    public class HlaScoringDataConverter :
        MatchedHlaDataConverterBase,
        IHlaScoringDataConverter
    {
        protected override IHlaLookupResult GetSerologyLookupResult(
            IHlaLookupResultSource<SerologyTyping> lookupResultSource)
        {
            var scoringInfo = SerologyScoringInfo.GetScoringInfo(lookupResultSource);

            return new HlaScoringLookupResult(
                lookupResultSource.TypingForHlaLookupResult.MatchLocus,
                lookupResultSource.TypingForHlaLookupResult.Name,
                LookupNameCategory.Serology,
                scoringInfo
            );
        }

        protected override IHlaLookupResult GetSingleAlleleLookupResult(
            IHlaLookupResultSource<AlleleTyping> lookupResultSource)
        {
            return GetMolecularLookupResult(
                new[] { lookupResultSource },
                allele => allele.Name,
                LookupNameCategory.OriginalAllele,
                sources => SingleAlleleScoringInfo.GetScoringInfoWithMatchingSerologies(sources.First()));
        }

        protected override IHlaLookupResult GetNmdpCodeAlleleLookupResult(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> lookupResultSources,
            string nmdpLookupName)
        {
            return GetMolecularLookupResult(
                lookupResultSources,
                allele => nmdpLookupName,
                LookupNameCategory.NmdpCodeAllele,
                MultipleAlleleScoringInfo.GetScoringInfo);
        }

        protected override IHlaLookupResult GetXxCodeLookupResult(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> lookupResultSources)
        {
            return GetMolecularLookupResult(
                lookupResultSources,
                allele => allele.ToXxCodeLookupName(),
                LookupNameCategory.XxCode,
                ConsolidatedMolecularScoringInfo.GetScoringInfo);
        }

        private static HlaScoringLookupResult GetMolecularLookupResult(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> lookupResultSources,
            Func<AlleleTyping, string> getLookupName,
            LookupNameCategory lookupNameCategory,
            Func<IEnumerable<IHlaLookupResultSource<AlleleTyping>>, IHlaScoringInfo> getScoringInfo)
        {
            var sources = lookupResultSources.ToList();

            var firstAllele = sources
                .First()
                .TypingForHlaLookupResult;

            return new HlaScoringLookupResult(
                firstAllele.MatchLocus,
                getLookupName(firstAllele),
                lookupNameCategory,
                getScoringInfo(sources)
            );
        }
    }
}
