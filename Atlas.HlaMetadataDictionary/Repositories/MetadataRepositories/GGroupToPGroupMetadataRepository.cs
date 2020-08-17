using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using Atlas.Common.ApplicationInsights;

namespace Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories
{
    internal interface IGGroupToPGroupMetadataRepository : IHlaMetadataRepository
    {
        Task<string> GetPGroupByGGroupIfExists(Locus locus, string lookupName, string hlaNomenclatureVersion);
    }

    internal class GGroupToPGroupMetadataRepository : HlaMetadataRepositoryBase, IGGroupToPGroupMetadataRepository
    {
        private const string DataTableReferencePrefix = "GGroupToPGroupLookupData";
        private const string CacheKey = nameof(GGroupToPGroupMetadataRepository);

        public GGroupToPGroupMetadataRepository(
            ICloudTableFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            IPersistentCacheProvider cacheProvider,
            ILogger logger)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, cacheProvider, CacheKey, logger)
        {
        }

        public async Task<string> GetPGroupByGGroupIfExists(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            var row = await GetHlaMetadataRowIfExists(locus, lookupName, TypingMethod.Molecular, hlaNomenclatureVersion);

            return row?.SerialisedHlaInfo;
        }
    }
}
