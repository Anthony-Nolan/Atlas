using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using Atlas.HlaMetadataDictionary.Services.AzureStorage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.HlaMetadataDictionary.Repositories.LookupRepositories
{
    internal interface IHlaLookupRepository : ILookupRepository
    {
        Task RecreateHlaLookupTable(IEnumerable<ISerialisableHlaMetadata> lookupResults, string hlaNomenclatureVersion);

        Task<HlaLookupTableEntity> GetHlaLookupTableEntityIfExists(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            string hlaNomenclatureVersion);
    }

    internal abstract class HlaLookupRepositoryBase :
        LookupRepositoryBase<ISerialisableHlaMetadata, HlaLookupTableEntity>,
        IHlaLookupRepository
    {
        protected HlaLookupRepositoryBase(
            ICloudTableFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            string dataFunctionalTableReferencePrefix,
            IPersistentCacheProvider cacheProvider,
            string cacheKey)
            : base(factory, tableReferenceRepository, dataFunctionalTableReferencePrefix, cacheProvider, cacheKey)
        {
        }

        public async Task RecreateHlaLookupTable(IEnumerable<ISerialisableHlaMetadata> lookupResults, string hlaNomenclatureVersion)
        {
            await RecreateDataTable(lookupResults, hlaNomenclatureVersion);
        }

        public async Task<HlaLookupTableEntity> GetHlaLookupTableEntityIfExists(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            string hlaNomenclatureVersion)
        {
            var partition = HlaLookupTableKeyManager.GetEntityPartitionKey(locus);
            var rowKey = HlaLookupTableKeyManager.GetEntityRowKey(lookupName, typingMethod);

            return await GetDataIfExists(partition, rowKey, hlaNomenclatureVersion);
        }
    }
}