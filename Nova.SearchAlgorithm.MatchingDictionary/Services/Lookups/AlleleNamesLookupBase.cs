using System;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Lookups
{
    /// <summary>
    /// Base class for all lookups that involve searching the lookup repository
    /// with one or more allele lookup names.
    /// </summary>
    internal abstract class AlleleNamesLookupBase : HlaLookupBase
    {
        private readonly IAlleleNamesLookupService alleleNamesLookupService;

        protected AlleleNamesLookupBase(
            IHlaLookupRepository hlaLookupRepository,
            IAlleleNamesLookupService alleleNamesLookupService)
            : base(hlaLookupRepository)
        {
            this.alleleNamesLookupService = alleleNamesLookupService;
        }

        public override async Task<IEnumerable<HlaLookupTableEntity>> PerformLookupAsync(MatchLocus matchLocus, string lookupName)
        {
            var alleleNamesToLookup = await GetAlleleLookupNames(matchLocus, lookupName);
            return await GetHlaLookupTableEntities(matchLocus, alleleNamesToLookup);
        }

        protected abstract Task<IEnumerable<string>> GetAlleleLookupNames(MatchLocus matchLocus, string lookupName);

        private async Task<IEnumerable<HlaLookupTableEntity>> GetHlaLookupTableEntities(
            MatchLocus matchLocus,
            IEnumerable<string> alleleNamesToLookup
        )
        {
            var lookupTasks = alleleNamesToLookup.Select(name => GetHlaLookupTableEntitiesForAlleleNameIfExists(matchLocus, name));
            var tableEntities = await Task.WhenAll(lookupTasks);

            return tableEntities.SelectMany(entities => entities);
        }

        /// <summary>
        /// Query matching lookup repository using the allele lookup name.
        /// If nothing is found, try again using the current version(s) of the allele name.
        /// Else an invalid HLA exception will be thrown.
        /// </summary>
        private async Task<IEnumerable<HlaLookupTableEntity>> GetHlaLookupTableEntitiesForAlleleNameIfExists(MatchLocus matchLocus, string lookupName)
        {
            var lookupResult = await TryGetHlaLookupTableEntityByAlleleLookupName(matchLocus, lookupName);
            if (lookupResult != null)
            {
                return new List<HlaLookupTableEntity> {lookupResult};
            }

            return await GetHlaLookupTableEntitiesByCurrentAlleleNamesIfExists(matchLocus, lookupName);
        }

        private async Task<HlaLookupTableEntity> TryGetHlaLookupTableEntityByAlleleLookupName(MatchLocus matchLocus, string lookupName)
        {
            return await TryGetHlaLookupTableEntity(matchLocus, lookupName, TypingMethod.Molecular);
        }

        private async Task<IEnumerable<HlaLookupTableEntity>> GetHlaLookupTableEntitiesByCurrentAlleleNamesIfExists(
            MatchLocus matchLocus,
            string lookupName
        )
        {
            var currentNames = await alleleNamesLookupService.GetCurrentAlleleNames(matchLocus, lookupName);
            var lookupTasks = currentNames.Select(name => GetHlaLookupTableEntityIfExists(matchLocus, name, TypingMethod.Molecular));
            return await Task.WhenAll(lookupTasks);
        }
    }
}