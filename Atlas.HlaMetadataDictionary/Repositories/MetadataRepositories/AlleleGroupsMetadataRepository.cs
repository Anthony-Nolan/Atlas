using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;

namespace Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories
{
    internal interface IAlleleGroupsMetadataRepository : IHlaMetadataRepository
    {
        Task<IAlleleGroupMetadata> GetAlleleGroupIfExists(Locus locus, string lookupName, string hlaNomenclatureVersion);
    }

    internal class AlleleGroupsMetadataRepository : HlaMetadataRepositoryBase, IAlleleGroupsMetadataRepository
    {
        private const string DataTableReferencePrefix = "AlleleGroupsLookupData";
        private const string CacheKey = nameof(AlleleGroupsMetadataRepository);

        public AlleleGroupsMetadataRepository(
            ITableClientFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            IPersistentCacheProvider cacheProvider,
            ILogger logger)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, cacheProvider, CacheKey, logger)
        {
        }

        public async Task<IAlleleGroupMetadata> GetAlleleGroupIfExists(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            var row = await GetHlaMetadataRowIfExists(locus, lookupName, TypingMethod.Molecular, hlaNomenclatureVersion);

            return row == null
                ? null
                : new AlleleGroupMetadata(row.Locus, row.LookupName, row.GetHlaInfo<List<string>>());
        }
    }
}