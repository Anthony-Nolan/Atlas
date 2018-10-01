using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.HlaDataConversion
{
    /// <summary>
    /// Converts Matched HLA to model optimised for HLA Scoring lookups.
    /// </summary>
    public interface IHlaMatchingDataConverter : IMatchedHlaDataConverterBase
    {
    }

    public class HlaMatchingDataConverter :
        MatchedHlaDataConverterBase,
        IHlaMatchingDataConverter
    {
        protected override IHlaLookupResult GetSerologyLookupResult(IHlaLookupResultSource<SerologyTyping> lookupResultSource)
        {
            return new HlaMatchingLookupResult(
                    lookupResultSource.TypingForHlaLookupResult.MatchLocus,
                    lookupResultSource.TypingForHlaLookupResult.Name,
                    TypingMethod.Serology,
                    lookupResultSource.MatchingPGroups);
        }

        protected override IHlaLookupResult GetSingleAlleleLookupResult(
            IHlaLookupResultSource<AlleleTyping> lookupResultSource)
        {
            return GetMolecularLookupResult(
                new[] { lookupResultSource },
                allele => allele.Name
            );
        }

        protected override IHlaLookupResult GetNmdpCodeAlleleLookupResult(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> lookupResultSources,
            string nmdpLookupName)
        {
            return GetMolecularLookupResult(
                lookupResultSources,
                allele => nmdpLookupName
            );
        }

        protected override IHlaLookupResult GetXxCodeLookupResult(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> lookupResultSources)
        {
            return GetMolecularLookupResult(
                lookupResultSources,
                allele => allele.ToXxCodeLookupName()
            );
        }
        
        private static IHlaLookupResult GetMolecularLookupResult(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> lookupResultSources,
            Func<AlleleTyping, string> getLookupName)
        {
            var sources = lookupResultSources.ToList();

            var firstAllele = sources
                .First()
                .TypingForHlaLookupResult;

            var pGroups = sources
                .SelectMany(resultSource => resultSource.MatchingPGroups)
                .Distinct();

            return new HlaMatchingLookupResult(
                firstAllele.MatchLocus,
                getLookupName(firstAllele),
                TypingMethod.Molecular,
                pGroups
                );
        }
    }
}
