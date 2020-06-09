using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using LazyCache;

namespace Atlas.MultipleAlleleCodeDictionary.MacCacheService
{
    public interface IMacCache
    {
        Task<MultipleAlleleCode> GetMacCode(string macCode);
        Task GenerateMacCache();
    }
    
    internal class MacCache: IMacCache
    {
        private readonly IAppCache cache;
        private readonly ILogger logger;
        private readonly IMacRepository macRepository;

        public MacCache(ILogger logger, IPersistentCacheProvider cacheProvider, IMacRepository macRepository)
        {
            this.logger = logger;
            this.cache = cacheProvider.Cache;
            this.macRepository = macRepository;
        }

        public async Task<MultipleAlleleCode> GetMacCode(string macCode)
        {
            return await cache.GetOrAddAsync(macCode, () => macRepository.GetMac(macCode));
        }
        
        public async Task GenerateMacCache()
        {
            var macs = await macRepository.GetAllMacs();
            foreach (var mac in macs)
            {
                cache.GetOrAdd(mac.Mac, () => mac);
            }
        }
    }
}