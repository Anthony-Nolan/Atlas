using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;

namespace Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories
{
    internal interface ISmallGGroupToPGroupMetadataRepository : IHlaMetadataRepository
    {
        Task<IMolecularTypingToPGroupMetadata> GetPGroupBySmallGGroupIfExists(Locus locus, string smallGGroup, string hlaNomenclatureVersion);
    }

    internal class SmallGGroupToPGroupMetadataRepository : HlaMetadataRepositoryBase, ISmallGGroupToPGroupMetadataRepository
    {
        private const string DataTableReferencePrefix = "SmallGGroupToPGroupLookupData";
        private const string CacheKey = nameof(SmallGGroupToPGroupMetadataRepository);

        public SmallGGroupToPGroupMetadataRepository(
            ICloudTableFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            IPersistentCacheProvider cacheProvider,
            ILogger logger)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, cacheProvider, CacheKey, logger)
        {
        }

        public async Task<IMolecularTypingToPGroupMetadata> GetPGroupBySmallGGroupIfExists(Locus locus, string smallGGroup, string hlaNomenclatureVersion)
        {
            var row = await GetHlaMetadataRowIfExists(locus, smallGGroup, TypingMethod.Molecular, hlaNomenclatureVersion);

            return row == null
                ? null
                : new MolecularTypingToPGroupMetadata(row.Locus, row.LookupName, row.GetHlaInfo<string>());
        }
    }
}