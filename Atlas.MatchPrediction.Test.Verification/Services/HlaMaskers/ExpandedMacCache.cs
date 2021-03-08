using Atlas.Common.Caching;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using LazyCache;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers
{
    internal interface IExpandedMacCache
    {
        Task<IEnumerable<string>> GetSecondFieldsByCode(string mac);
        Task<IEnumerable<string>> GetCodesBySecondField(string secondField);
    }

    internal class ExpandedMacCache : IExpandedMacCache
    {
        private readonly IAppCache cache;
        private readonly IExpandedMacsRepository macsRepository;

        // ReSharper disable once SuggestBaseTypeForParameter
        public ExpandedMacCache(ITransientCacheProvider cacheProvider, IExpandedMacsRepository macsRepository)
        {
            cache = cacheProvider.Cache;
            this.macsRepository = macsRepository;
        }

        public async Task<IEnumerable<string>> GetSecondFieldsByCode(string mac)
        {
            return await cache.GetOrAddAsync(mac, () => macsRepository.SelectSecondFieldsByCode(mac));
        }

        public async Task<IEnumerable<string>> GetCodesBySecondField(string secondField)
        {
            return await cache.GetOrAddAsync(secondField, () => macsRepository.SelectCodesBySecondField(secondField));
        }
    }
}