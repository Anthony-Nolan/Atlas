using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups
{
    /// <summary>
    /// Base class for all lookups that involve searching the Matching Dictionary
    /// with one or more allele lookup names.
    /// </summary>
    internal abstract class AlleleNamesLookupBase : MatchingDictionaryLookup
    {
        private readonly IAlleleNamesLookupService alleleNamesLookupService;

        protected AlleleNamesLookupBase(
            IMatchingDictionaryRepository dictionaryRepository, IAlleleNamesLookupService alleleNamesLookupService)
                : base(dictionaryRepository)
        {
            this.alleleNamesLookupService = alleleNamesLookupService;
        }

        public override async Task<MatchingDictionaryEntry> PerformLookupAsync(MatchLocus matchLocus, string lookupName)
        {
            var alleleNamesToLookup = await GetAlleleLookupNames(matchLocus, lookupName);
            var matchingDictionaryEntries = await GetMatchingDictionaryEntries(matchLocus, alleleNamesToLookup);
            var molecularSubtype = GetMolecularSubtype(matchingDictionaryEntries);

            return new MatchingDictionaryEntry(matchLocus, lookupName, molecularSubtype, matchingDictionaryEntries);
        }

        protected abstract Task<IEnumerable<string>> GetAlleleLookupNames(MatchLocus matchLocus, string lookupName);

        private async Task<IEnumerable<MatchingDictionaryEntry>> GetMatchingDictionaryEntries(MatchLocus matchLocus, IEnumerable<string> alleleNamesToLookup)
        {
            var lookupTasks = alleleNamesToLookup.Select(name => GetMatchingDictionaryEntryForAlleleNameIfExists(matchLocus, name));
            var lookupResults = await Task.WhenAll(lookupTasks);
            var matchingDictionaryEntries = lookupResults.SelectMany(result => result);

            return matchingDictionaryEntries;
        }

        /// <summary>
        /// Query matching dictionary using the allele lookup name.
        /// If nothing is found, try again using the current version(s) of the allele name.
        /// Else an invalid HLA exception will be thrown.
        /// </summary>
        private async Task<IEnumerable<MatchingDictionaryEntry>> GetMatchingDictionaryEntryForAlleleNameIfExists(
            MatchLocus matchLocus, string lookupName)
        {
            if (TryGetMatchingDictionaryEntryByAlleleLookupName(matchLocus, lookupName, out var entry))
            {
                return new List<MatchingDictionaryEntry> { entry };
            }

            return await GetMatchingDictionaryEntriesByCurrentAlleleNamesIfExists(matchLocus, lookupName);
        }

        private bool TryGetMatchingDictionaryEntryByAlleleLookupName(
            MatchLocus matchLocus, string lookupName, out MatchingDictionaryEntry entry)
        {
            return TryGetMatchingDictionaryEntry(matchLocus, lookupName, TypingMethod.Molecular, out entry);
        }

        private async Task<IEnumerable<MatchingDictionaryEntry>> GetMatchingDictionaryEntriesByCurrentAlleleNamesIfExists(
            MatchLocus matchLocus, string lookupName)
        {
            var currentNames = await alleleNamesLookupService.GetCurrentAlleleNames(matchLocus, lookupName);
            var lookupTasks = currentNames.Select(name => GetMatchingDictionaryEntryIfExists(matchLocus, name, TypingMethod.Molecular));
            return await Task.WhenAll(lookupTasks);
        }

        private static MolecularSubtype GetMolecularSubtype(IEnumerable<MatchingDictionaryEntry> matchingDictionaryEntries)
        {
            return matchingDictionaryEntries.Count() == 1
                ? MolecularSubtype.CompleteAllele
                : MolecularSubtype.MultipleAlleles;
        }
    }
}
