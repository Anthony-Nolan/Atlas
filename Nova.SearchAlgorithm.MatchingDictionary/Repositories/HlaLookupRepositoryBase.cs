using Microsoft.Extensions.Caching.Memory;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IHlaLookupRepository : ILookupRepository<IHlaLookupResult, HlaLookupTableEntity>
    {
        Task<HlaLookupTableEntity> GetHlaLookupTableEntityIfExists(
            MatchLocus matchLocus, string lookupName, TypingMethod typingMethod);
    }

    public abstract class HlaLookupRepositoryBase :
        LookupRepositoryBase<IHlaLookupResult, HlaLookupTableEntity>,
        IHlaLookupRepository
    {
        protected HlaLookupRepositoryBase(
            ICloudTableFactory factory, 
            ITableReferenceRepository tableReferenceRepository,
            string dataTableReferencePrefix,
            IMemoryCache memoryCache,
            string cacheKey)
            : base(factory, tableReferenceRepository, dataTableReferencePrefix, memoryCache, cacheKey)
        {
        }

        public async Task<HlaLookupTableEntity> GetHlaLookupTableEntityIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            var partition = HlaLookupTableEntity.GetPartition(matchLocus);
            var rowKey = HlaLookupTableEntity.GetRowKey(lookupName, typingMethod);

            return await GetDataIfExists(partition, rowKey);
        }

        protected override IEnumerable<string> GetTablePartitions()
        {
            return PermittedLocusNames
                .GetPermittedMatchLoci()
                .Select(matchLocus => matchLocus.ToString());
        }
    }
}
