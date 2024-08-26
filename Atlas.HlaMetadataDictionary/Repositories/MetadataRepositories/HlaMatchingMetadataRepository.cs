using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;

namespace Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories
{
    internal interface IHlaMatchingMetadataRepository : IHlaMetadataRepository
    {
        Task<IEnumerable<string>> GetAllPGroups(string hlaNomenclatureVersion);
    }

    internal class HlaMatchingMetadataRepository : HlaMetadataRepositoryBase, IHlaMatchingMetadataRepository
    {
        private const string DataTableReferencePrefix = "HlaMatchingLookupData";
        private const string CacheKey = nameof(HlaMatchingMetadataRepository);

        public HlaMatchingMetadataRepository(
            ITableClientFactory factory, 
            ITableReferenceRepository tableReferenceRepository,
            IPersistentCacheProvider cacheProvider,
            ILogger logger)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, cacheProvider, CacheKey, logger)
        {
        }

        public async Task<IEnumerable<string>> GetAllPGroups(string hlaNomenclatureVersion)
        {
            return await Cache.GetOrAddAsync($"All-P-Groups:{hlaNomenclatureVersion}", async _ => await CalculateAllPGroups(hlaNomenclatureVersion));
        }

        private async Task<List<string>> CalculateAllPGroups(string hlaNomenclatureVersion)
        {
            var metadataDictionary = await TableData(hlaNomenclatureVersion);
            using (Logger.RunTimed("Calculate all P-Groups from matching metadata entries"))
            {
                return new HashSet<string>(metadataDictionary.Values.SelectMany(v => v.ToHlaMatchingMetadata()?.MatchingPGroups)).ToList();
            }
        }          
    }
}
