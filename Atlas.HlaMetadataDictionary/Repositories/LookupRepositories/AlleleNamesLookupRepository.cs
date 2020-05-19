using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Extensions;
using Atlas.HlaMetadataDictionary.Models.Lookups.AlleleNameLookup;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using System.Threading.Tasks;

namespace Atlas.HlaMetadataDictionary.Repositories.LookupRepositories
{
    internal interface IAlleleNamesLookupRepository : IHlaLookupRepository
    {
        Task<IAlleleNameLookupResult> GetAlleleNameIfExists(Locus locus, string lookupName, string hlaDatabaseVersion);
    }

    internal class AlleleNamesLookupRepository : 
        HlaLookupRepositoryBase,
        IAlleleNamesLookupRepository
    {
        private const string DataTableReferencePrefix = "AlleleNamesData";
        private const string CacheKeyAlleleNames = "AlleleNames";

        public AlleleNamesLookupRepository(
            ICloudTableFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            IPersistentCacheProvider cacheProvider)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, cacheProvider, CacheKeyAlleleNames)
        {
        }

        public async Task<IAlleleNameLookupResult> GetAlleleNameIfExists(Locus locus, string lookupName, string hlaDatabaseVersion)
        {
            var entity = await GetHlaLookupTableEntityIfExists(locus, lookupName, TypingMethod.Molecular, hlaDatabaseVersion);

            return entity?.ToAlleleNameLookupResult();
        }
    }
}
