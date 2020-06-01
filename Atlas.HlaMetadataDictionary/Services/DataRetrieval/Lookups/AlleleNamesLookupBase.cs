using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;
using Atlas.HlaMetadataDictionary.Repositories.LookupRepositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
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

        public override async Task<IEnumerable<HlaLookupTableEntity>> PerformLookupAsync(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            var alleleNamesToLookup = await GetAlleleLookupNames(locus, lookupName);
            return await GetHlaLookupTableEntities(locus, alleleNamesToLookup, hlaNomenclatureVersion);
        }

        protected abstract Task<IEnumerable<string>> GetAlleleLookupNames(Locus locus, string lookupName);

        private async Task<IEnumerable<HlaLookupTableEntity>> GetHlaLookupTableEntities(
            Locus locus,
            IEnumerable<string> alleleNamesToLookup,
            string hlaNomenclatureVersion
        )
        {
            var lookupTasks = alleleNamesToLookup.Select(name => GetHlaLookupTableEntitiesForAlleleNameIfExists(locus, name, hlaNomenclatureVersion));
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
            string hlaNomenclatureVersion)
        {
            var lookupResult = await TryGetHlaLookupTableEntityByAlleleLookupName(locus, lookupName, hlaNomenclatureVersion);
            if (lookupResult != null)
            {
                return new List<HlaLookupTableEntity> {lookupResult};
            }

            return await GetHlaLookupTableEntitiesByCurrentAlleleNamesIfExists(locus, lookupName, hlaNomenclatureVersion);
        }

        private async Task<HlaLookupTableEntity> TryGetHlaLookupTableEntityByAlleleLookupName(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            return await TryGetHlaLookupTableEntity(locus, lookupName, TypingMethod.Molecular, hlaNomenclatureVersion);
        }

        private async Task<IEnumerable<HlaLookupTableEntity>> GetHlaLookupTableEntitiesByCurrentAlleleNamesIfExists(
            Locus locus,
            string lookupName,
            string hlaNomenclatureVersion
        )
        {
            var currentNames = await alleleNamesLookupService.GetCurrentAlleleNames(locus, lookupName, hlaNomenclatureVersion);
            var lookupTasks = currentNames.Select(name => GetHlaLookupTableEntityIfExists(locus, name, TypingMethod.Molecular, hlaNomenclatureVersion));
            return await Task.WhenAll(lookupTasks);
        }
    }
}