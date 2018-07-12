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
    internal abstract class AlleleNamesLookupBase : HlaTypingLookupBase
    {
        private readonly IAlleleNamesLookupService alleleNamesLookupService;

        protected AlleleNamesLookupBase(
            IPreCalculatedHlaMatchRepository preCalculatedHlaMatchRepository, IAlleleNamesLookupService alleleNamesLookupService)
                : base(preCalculatedHlaMatchRepository)
        {
            this.alleleNamesLookupService = alleleNamesLookupService;
        }

        public override async Task<PreCalculatedHlaMatchInfo> PerformLookupAsync(MatchLocus matchLocus, string lookupName)
        {
            var alleleNamesToLookup = await GetAlleleLookupNames(matchLocus, lookupName);
            var preCalculatedHlaMatchInfo = await GetPreCalculatedHlaMatchInfo(matchLocus, alleleNamesToLookup);
            var molecularSubtype = GetMolecularSubtype(preCalculatedHlaMatchInfo);

            return new PreCalculatedHlaMatchInfo(matchLocus, lookupName, molecularSubtype, preCalculatedHlaMatchInfo);
        }

        protected abstract Task<IEnumerable<string>> GetAlleleLookupNames(MatchLocus matchLocus, string lookupName);

        private async Task<IEnumerable<PreCalculatedHlaMatchInfo>> GetPreCalculatedHlaMatchInfo(MatchLocus matchLocus, IEnumerable<string> alleleNamesToLookup)
        {
            var lookupTasks = alleleNamesToLookup.Select(name => GetPreCalculatedHlaMatchInfoForAlleleNameIfExists(matchLocus, name));
            var lookupResults = await Task.WhenAll(lookupTasks);
            var preCalculatedHlaMatchInfo = lookupResults.SelectMany(result => result);

            return preCalculatedHlaMatchInfo;
        }

        /// <summary>
        /// Query matching dictionary using the allele lookup name.
        /// If nothing is found, try again using the current version(s) of the allele name.
        /// Else an invalid HLA exception will be thrown.
        /// </summary>
        private async Task<IEnumerable<PreCalculatedHlaMatchInfo>> GetPreCalculatedHlaMatchInfoForAlleleNameIfExists(
            MatchLocus matchLocus, string lookupName)
        {
            if (TryGetPreCalculatedHlaMatchInfoByAlleleLookupName(matchLocus, lookupName, out var entry))
            {
                return new List<PreCalculatedHlaMatchInfo> { entry };
            }

            return await GetPreCalculatedHlaMatchInfoByCurrentAlleleNamesIfExists(matchLocus, lookupName);
        }

        private bool TryGetPreCalculatedHlaMatchInfoByAlleleLookupName(
            MatchLocus matchLocus, string lookupName, out PreCalculatedHlaMatchInfo entry)
        {
            return TryGetPreCalculatedHlaMatchInfo(matchLocus, lookupName, TypingMethod.Molecular, out entry);
        }

        private async Task<IEnumerable<PreCalculatedHlaMatchInfo>> GetPreCalculatedHlaMatchInfoByCurrentAlleleNamesIfExists(
            MatchLocus matchLocus, string lookupName)
        {
            var currentNames = await alleleNamesLookupService.GetCurrentAlleleNames(matchLocus, lookupName);
            var lookupTasks = currentNames.Select(name => GetPreCalculatedHlaMatchInfoIfExists(matchLocus, name, TypingMethod.Molecular));
            return await Task.WhenAll(lookupTasks);
        }

        private static MolecularSubtype GetMolecularSubtype(IEnumerable<PreCalculatedHlaMatchInfo> preCalculatedHlaMatchInfo)
        {
            return preCalculatedHlaMatchInfo.Count() == 1
                ? MolecularSubtype.CompleteAllele
                : MolecularSubtype.MultipleAlleles;
        }
    }
}
