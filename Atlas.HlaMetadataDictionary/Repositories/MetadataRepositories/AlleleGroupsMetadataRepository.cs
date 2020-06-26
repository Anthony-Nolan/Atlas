using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories
{
    internal interface IAlleleGroupsMetadataRepository : IHlaMetadataRepository
    {
        Task<IAlleleGroupMetadata> GetAlleleGroupIfExists(Locus locus, string lookupName, string hlaNomenclatureVersion);
    }

    internal class AlleleGroupsMetadataRepository : HlaMetadataRepositoryBase, IAlleleGroupsMetadataRepository
    {
        private const string DataTableReferencePrefix = "AlleleGroupsLookupData";
        private const string CacheKey = "AlleleGroupsLookup";

        public AlleleGroupsMetadataRepository(
            ICloudTableFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            IPersistentCacheProvider cacheProvider)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, cacheProvider, CacheKey)
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