using System.Collections.Generic;
using System.Threading.Tasks;
using LazyCache;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using Atlas.HlaMetadataDictionary.Repositories.LookupRepositories;
using Atlas.Utils.Models;

namespace Atlas.HlaMetadataDictionary.Repositories
{
    public interface IHlaLookupRepository : ILookupRepository<IHlaLookupResult, HlaLookupTableEntity>
    {
        Task RecreateHlaLookupTable(IEnumerable<IHlaLookupResult> lookupResults, string hlaDatabaseVersion);

        Task<HlaLookupTableEntity> GetHlaLookupTableEntityIfExists(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            string hlaDatabaseVersion);
    }

    public abstract class HlaLookupRepositoryBase :
        LookupRepositoryBase<IHlaLookupResult, HlaLookupTableEntity>,
        IHlaLookupRepository
    {
        protected HlaLookupRepositoryBase(
            ICloudTableFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            string dataFunctionalTableReferencePrefix,
            IAppCache cache,
            string cacheKey)
            : base(factory, tableReferenceRepository, dataFunctionalTableReferencePrefix, cache, cacheKey)
        {
        }

        public async Task RecreateHlaLookupTable(IEnumerable<IHlaLookupResult> lookupResults, string hlaDatabaseVersion)
        {
            await RecreateDataTable(lookupResults, HlaLookupTableKeyManager.GetTablePartitionKeys(), hlaDatabaseVersion);
        }

        public async Task<HlaLookupTableEntity> GetHlaLookupTableEntityIfExists(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            string hlaDatabaseVersion)
        {
            var partition = HlaLookupTableKeyManager.GetEntityPartitionKey(locus);
            var rowKey = HlaLookupTableKeyManager.GetEntityRowKey(lookupName, typingMethod);

            return await GetDataIfExists(partition, rowKey, hlaDatabaseVersion);
        }
    }
}