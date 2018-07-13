using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary.MatchingLookup;
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
    internal abstract class AlleleNamesLookupBase : HlaMatchingLookupBase
    {
        private readonly IAlleleNamesLookupService alleleNamesLookupService;

        protected AlleleNamesLookupBase(
            IHlaMatchingLookupRepository hlaMatchingLookupRepository, IAlleleNamesLookupService alleleNamesLookupService)
                : base(hlaMatchingLookupRepository)
        {
            this.alleleNamesLookupService = alleleNamesLookupService;
        }

        public override async Task<HlaMatchingLookupResult> PerformLookupAsync(MatchLocus matchLocus, string lookupName)
        {
            var alleleNamesToLookup = await GetAlleleLookupNames(matchLocus, lookupName);
            var lookupResults = await GetHlaMatchingLookupResult(matchLocus, alleleNamesToLookup);

            return new HlaMatchingLookupResult(matchLocus, lookupName, lookupResults);
        }

        protected abstract Task<IEnumerable<string>> GetAlleleLookupNames(MatchLocus matchLocus, string lookupName);

        private async Task<IEnumerable<HlaMatchingLookupResult>> GetHlaMatchingLookupResult(MatchLocus matchLocus, IEnumerable<string> alleleNamesToLookup)
        {
            var lookupTasks = alleleNamesToLookup.Select(name => GetHlaMatchingLookupResultsForAlleleNameIfExists(matchLocus, name));
            var lookupResults = await Task.WhenAll(lookupTasks);
            var preCalculatedHlaMatchInfo = lookupResults.SelectMany(result => result);

            return preCalculatedHlaMatchInfo;
        }

        /// <summary>
        /// Query matching lookup repository using the allele lookup name.
        /// If nothing is found, try again using the current version(s) of the allele name.
        /// Else an invalid HLA exception will be thrown.
        /// </summary>
        private async Task<IEnumerable<HlaMatchingLookupResult>> GetHlaMatchingLookupResultsForAlleleNameIfExists(
            MatchLocus matchLocus, string lookupName)
        {
            if (TryGetHlaMatchingLookupResultByAlleleLookupName(matchLocus, lookupName, out var lookupResult))
            {
                return new List<HlaMatchingLookupResult> { lookupResult };
            }

            return await GetHlaMatchingLookupResultByCurrentAlleleNamesIfExists(matchLocus, lookupName);
        }

        private bool TryGetHlaMatchingLookupResultByAlleleLookupName(
            MatchLocus matchLocus, string lookupName, out HlaMatchingLookupResult lookupResult)
        {
            return TryGetHlaMatchingLookupResult(matchLocus, lookupName, TypingMethod.Molecular, out lookupResult);
        }

        private async Task<IEnumerable<HlaMatchingLookupResult>> GetHlaMatchingLookupResultByCurrentAlleleNamesIfExists(
            MatchLocus matchLocus, string lookupName)
        {
            var currentNames = await alleleNamesLookupService.GetCurrentAlleleNames(matchLocus, lookupName);
            var lookupTasks = currentNames.Select(name => GetHlaMatchingLookupResultIfExists(matchLocus, name, TypingMethod.Molecular));
            return await Task.WhenAll(lookupTasks);
        }
    }
}
