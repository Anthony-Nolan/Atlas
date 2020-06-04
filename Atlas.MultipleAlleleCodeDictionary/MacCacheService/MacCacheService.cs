using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.MultipleAlleleCodeDictionary.MacImportService;
using Atlas.MultipleAlleleCodeDictionary.Models;
using LazyCache;

namespace Atlas.MultipleAlleleCodeDictionary.MacCacheService
{

    internal interface IMacCachingService
    {
        Task GenerateMacCache();
    }
    
    internal interface IMacCache
    {
        Task<MultipleAlleleCodeEntity> GetMacCode(string macCode);
    }
    
    internal class MacCache: IMacCache, IMacCachingService
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

        public async Task<MultipleAlleleCodeEntity> GetMacCode(string macCode)
        {
            return await cache.GetOrAddAsync(macCode, () => macRepository.GetMac(macCode));
        }
        
        public async Task GenerateMacCache()
        {
         
            
        }
    }
}