using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Services.Lookups
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

        public override async Task<IEnumerable<HlaLookupTableEntity>> PerformLookupAsync(Locus locus, string lookupName, string hlaDatabaseVersion)
        {
            var alleleNamesToLookup = await GetAlleleLookupNames(locus, lookupName);
            return await GetHlaLookupTableEntities(locus, alleleNamesToLookup, hlaDatabaseVersion);
        }

        protected abstract Task<IEnumerable<string>> GetAlleleLookupNames(Locus locus, string lookupName);

        private async Task<IEnumerable<HlaLookupTableEntity>> GetHlaLookupTableEntities(
            Locus locus,
            IEnumerable<string> alleleNamesToLookup,
            string hlaDatabaseVersion
        )
        {
            var lookupTasks = alleleNamesToLookup.Select(name => GetHlaLookupTableEntitiesForAlleleNameIfExists(locus, name, hlaDatabaseVersion));
            var tableEntities = await Task.WhenAll(lookupTasks);

            return tableEntities.SelectMany(entities => entities);
        }

        /// <summary>
        /// Query matching lookup repository using the allele lookup name.
        /// If nothing is found, try again using the current version(s) of the allele name.
        /// Else an invalid HLA exception will be thrown.
        /// </summary>
        private async Task<IEnumerable<HlaLookupTableEntity>> GetHlaLookupTableEntitiesForAlleleNameIfExists(
            Locus locus,
            string lookupName,
            string hlaDatabaseVersion)
        {
            var lookupResult = await TryGetHlaLookupTableEntityByAlleleLookupName(locus, lookupName, hlaDatabaseVersion);
            if (lookupResult != null)
            {
                return new List<HlaLookupTableEntity> {lookupResult};
            }

            return await GetHlaLookupTableEntitiesByCurrentAlleleNamesIfExists(locus, lookupName, hlaDatabaseVersion);
        }

        private async Task<HlaLookupTableEntity> TryGetHlaLookupTableEntityByAlleleLookupName(Locus locus, string lookupName, string hlaDatabaseVersion)
        {
            return await TryGetHlaLookupTableEntity(locus, lookupName, TypingMethod.Molecular, hlaDatabaseVersion);
        }

        private async Task<IEnumerable<HlaLookupTableEntity>> GetHlaLookupTableEntitiesByCurrentAlleleNamesIfExists(
            Locus locus,
            string lookupName,
            string hlaDatabaseVersion
        )
        {
            var currentNames = await alleleNamesLookupService.GetCurrentAlleleNames(locus, lookupName, hlaDatabaseVersion);
            var lookupTasks = currentNames.Select(name => GetHlaLookupTableEntityIfExists(locus, name, TypingMethod.Molecular, hlaDatabaseVersion));
            return await Task.WhenAll(lookupTasks);
        }
    }
}