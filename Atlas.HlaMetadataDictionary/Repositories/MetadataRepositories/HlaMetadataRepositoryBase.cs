using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using Atlas.HlaMetadataDictionary.Services.AzureStorage;

namespace Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories
{
    internal interface IHlaMetadataRepository : IWarmableRepository
    {
        Task RecreateHlaMetadataTable(IEnumerable<ISerialisableHlaMetadata> metadata, string hlaNomenclatureVersion);

        Task<HlaMetadataTableRow> GetHlaMetadataRowIfExists(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            string hlaNomenclatureVersion);
    }

    internal abstract class HlaMetadataRepositoryBase :
        CloudTableRepositoryBase<ISerialisableHlaMetadata, HlaMetadataTableRow>,
        IHlaMetadataRepository
    {
        protected HlaMetadataRepositoryBase(
            ICloudTableFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            string dataFunctionalTableReferencePrefix,
            IPersistentCacheProvider cacheProvider,
            string cacheKey,
            ILogger logger)
            : base(factory, tableReferenceRepository, dataFunctionalTableReferencePrefix, cacheProvider, cacheKey, logger)
        {
        }

        public async Task RecreateHlaMetadataTable(IEnumerable<ISerialisableHlaMetadata> metadata, string hlaNomenclatureVersion)
        {
            await RecreateDataTable(metadata, hlaNomenclatureVersion);
        }

        public async Task<HlaMetadataTableRow> GetHlaMetadataRowIfExists(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            string hlaNomenclatureVersion)
        {
            var partition = HlaMetadataTableKeyManager.GetPartitionKey(locus);
            var rowKey = HlaMetadataTableKeyManager.GetRowKey(lookupName, typingMethod);

            return await GetDataRowIfExists(partition, rowKey, hlaNomenclatureVersion);
        }
    }
}