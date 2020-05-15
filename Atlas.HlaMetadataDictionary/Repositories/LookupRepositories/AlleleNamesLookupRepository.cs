using System.Threading.Tasks;
using LazyCache;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.Lookups.AlleleNameLookup;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using Atlas.Utils.Models;

namespace Atlas.HlaMetadataDictionary.Repositories
{
    public interface IAlleleNamesLookupRepository : IHlaLookupRepository
    {
        Task<IAlleleNameLookupResult> GetAlleleNameIfExists(Locus locus, string lookupName, string hlaDatabaseVersion);
    }

    public class AlleleNamesLookupRepository : 
        HlaLookupRepositoryBase,
        IAlleleNamesLookupRepository
    {
        private const string DataTableReferencePrefix = "AlleleNamesData";
        private const string CacheKeyAlleleNames = "AlleleNames";

        public AlleleNamesLookupRepository(
            ICloudTableFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            IAppCache appCache)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, appCache, CacheKeyAlleleNames)
        {
        }

        public async Task<IAlleleNameLookupResult> GetAlleleNameIfExists(Locus locus, string lookupName, string hlaDatabaseVersion)
        {
            var entity = await GetHlaLookupTableEntityIfExists(locus, lookupName, TypingMethod.Molecular, hlaDatabaseVersion);

            return entity?.ToAlleleNameLookupResult();
        }
    }
}
