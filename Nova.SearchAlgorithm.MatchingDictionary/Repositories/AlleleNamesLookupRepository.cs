using Microsoft.Extensions.Caching.Memory;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.AlleleNameLookup;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IAlleleNamesLookupRepository : IHlaLookupRepository
    {
        Task<AlleleNameLookupResult> GetAlleleNameIfExists(MatchLocus matchLocus, string lookupName);
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

        public async Task<AlleleNameLookupResult> GetAlleleNameIfExists(MatchLocus matchLocus, string lookupName)
        {
            var entity = await GetHlaLookupTableEntityIfExists(matchLocus, lookupName, TypingMethod.Molecular);

            return entity?.ToAlleleNameLookupResult();
        }
    }
}
