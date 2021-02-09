using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;

namespace Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories
{
    internal interface ISmallGGroupToPGroupMetadataRepository : IHlaMetadataRepository
    {
        Task<IDictionary<Locus, ISet<string>>> GetAllSmallGGroups(string hlaNomenclatureVersion);
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

        public async Task<IMolecularTypingToPGroupMetadata> GetPGroupBySmallGGroupIfExists(
            Locus locus,
            string smallGGroup,
            string hlaNomenclatureVersion)
        {
            var row = await GetHlaMetadataRowIfExists(locus, smallGGroup, TypingMethod.Molecular, hlaNomenclatureVersion);

            return row == null
                ? null
                : new MolecularTypingToPGroupMetadata(row.Locus, row.LookupName, row.GetHlaInfo<string>());
        }
        
        public async Task<IDictionary<Locus, ISet<string>>> GetAllSmallGGroups(string hlaNomenclatureVersion)
        {
            return await Cache.GetOrAddAsync(
                $"All-small-g-Groups:{hlaNomenclatureVersion}",
                async _ => await CalculateAllSmallGGroups(hlaNomenclatureVersion)
            );
        }

        private async Task<IDictionary<Locus, ISet<string>>> CalculateAllSmallGGroups(string hlaNomenclatureVersion)
        {
            var tableData = await TableData(hlaNomenclatureVersion);
            using (Logger.RunTimed("Calculate all small g-groups"))
            {
                var byLocus = tableData.Values.GroupBy(v => v.Locus);
                return byLocus.ToDictionary(
                    g => g.Key,
                    g => g.Select(row => row.LookupName)
                        .Where(x => x != null)
                        .ToHashSet() as ISet<string>
                );
            }
        }
    }
}