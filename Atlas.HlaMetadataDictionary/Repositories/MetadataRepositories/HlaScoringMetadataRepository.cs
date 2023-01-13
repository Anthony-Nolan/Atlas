using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;

namespace Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories
{
    internal interface IHlaScoringMetadataRepository : IHlaMetadataRepository
    {
        Task<IDictionary<Locus, List<string>>> GetAllGGroups(string hlaNomenclatureVersion);
    }

    internal class HlaScoringMetadataRepository : HlaMetadataRepositoryBase, IHlaScoringMetadataRepository
    {
        private const string DataTableReferencePrefix = "HlaScoringLookupData";
        private const string CacheKey = nameof(HlaScoringMetadataRepository);

        public HlaScoringMetadataRepository(
            ICloudTableFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            IPersistentCacheProvider cacheProvider,
            ILogger logger)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, cacheProvider, CacheKey, logger)
        {
        }

        /// <inheritdoc />
        public async Task<IDictionary<Locus, List<string>>> GetAllGGroups(string hlaNomenclatureVersion)
        {
            return await Cache.GetOrAddAsync($"All-G-Groups:{hlaNomenclatureVersion}", async _ => await CalculateAllGGroups(hlaNomenclatureVersion));
        }

        private async Task<IDictionary<Locus, List<string>>> CalculateAllGGroups(string hlaNomenclatureVersion)
        {
            var metadataDictionary = await TableData(hlaNomenclatureVersion);
            using (Logger.RunTimed("Calculate all GGroups"))
            {
                var byLocus = metadataDictionary.Values.GroupBy(v => v.Locus);
                return byLocus.ToDictionary(
                    g => g.Key,
                    g => new HashSet<string>(g.SelectMany(v => v.ToHlaScoringMetadata()?.HlaScoringInfo.MatchingGGroups))
                        .Where(x => x != null)
                        .ToList()
                );
            }
        }
    }
}