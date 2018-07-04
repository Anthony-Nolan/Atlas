using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups
{
    internal abstract class AlleleNameBasedLookup : MatchingDictionaryLookup
    {
        private readonly IAlleleNamesLookupService alleleNamesLookupService;

        protected AlleleNameBasedLookup(
            IMatchingDictionaryRepository dictionaryRepository, IAlleleNamesLookupService alleleNamesLookupService)
                : base(dictionaryRepository)
        {
            this.alleleNamesLookupService = alleleNamesLookupService;
        }

        public override async Task<MatchingDictionaryEntry> PerformLookupAsync(MatchLocus matchLocus, string lookupName)
        {
            var alleleNamesToLookup = await GetAllelesNames(matchLocus, lookupName);
            var matchingDictionaryEntries = await GetMatchingDictionaryEntries(matchLocus, alleleNamesToLookup);
            var molecularSubtype = GetMolecularSubtype(matchingDictionaryEntries);

            return new MatchingDictionaryEntry(matchLocus, lookupName, molecularSubtype, matchingDictionaryEntries);
        }

        protected abstract Task<IEnumerable<string>> GetAllelesNames(MatchLocus matchLocus, string lookupName);

        private async Task<IEnumerable<MatchingDictionaryEntry>> GetMatchingDictionaryEntries(MatchLocus matchLocus, IEnumerable<string> alleleNamesToLookup)
        {
            var lookupTasks = alleleNamesToLookup.Select(name => GetEntryForAlleleNameIfExists(matchLocus, name));
            var lookupResults = await Task.WhenAll(lookupTasks);
            var matchingDictionaryEntries = lookupResults.SelectMany(result => result);

            return matchingDictionaryEntries;
        }

        /// <summary>
        /// Query matching dictionary using the submitted allele name.
        /// If nothing is found, try again using the current version(s) of the allele name.
        /// Else an invalid HLA exception will be thrown.
        /// </summary>
        private async Task<IEnumerable<MatchingDictionaryEntry>> GetEntryForAlleleNameIfExists(MatchLocus matchLocus, string lookupName)
        {
            if (TryGetEntryByLookupName(matchLocus, lookupName, out var entry))
            {
                return new List<MatchingDictionaryEntry> { entry };
            }

            return await GetEntriesByCurrentNamesIfExists(matchLocus, lookupName);
        }

        private bool TryGetEntryByLookupName(MatchLocus matchLocus, string lookupName, out MatchingDictionaryEntry entry)
        {
            return TryGetMatchingDictionaryEntry(matchLocus, lookupName, TypingMethod.Molecular, out entry);
        }

        private async Task<IEnumerable<MatchingDictionaryEntry>> GetEntriesByCurrentNamesIfExists(MatchLocus matchLocus, string lookupName)
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
