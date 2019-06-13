using Microsoft.Extensions.Caching.Memory;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.AlleleNameLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
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
            IMemoryCache memoryCache)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, memoryCache, CacheKeyAlleleNames)
        {
        }

        public async Task<IAlleleNameLookupResult> GetAlleleNameIfExists(Locus locus, string lookupName, string hlaDatabaseVersion)
        {
            var entity = await GetHlaLookupTableEntityIfExists(locus, lookupName, TypingMethod.Molecular, hlaDatabaseVersion);

            return entity?.ToAlleleNameLookupResult();
        }
    }
}
