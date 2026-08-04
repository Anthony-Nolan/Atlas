using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using LazyCache;

namespace Atlas.MultipleAlleleCodeDictionary.Services
{
    public interface IMacCacheService
    {
        Task<IEnumerable<string>> GetHlaFromMac(string firstField, string macCode);
        Task<Mac> GetMacCode(string macCode);
        Task GenerateMacCache();
    }
    
    internal class MacCacheService: IMacCacheService
    {
        private readonly IAppCache cache;
        private readonly IAtlasLogger logger;
        private readonly IMacRepository macRepository;
        private readonly IMacExpander macExpander;

        public MacCacheService(IAtlasLogger logger, IPersistentCacheProvider cacheProvider, IMacRepository macRepository, IMacExpander macExpander)
        {
            this.logger = logger;
            this.cache = cacheProvider.Cache;
            this.macRepository = macRepository;
            this.macExpander = macExpander;
        }

        public async Task<IEnumerable<string>> GetHlaFromMac(string macCode, string firstField)
        {
            var mac = await GetMacCode(macCode);
            if (mac == null)
            {
                var message = $"Unrecognised Mac: {macCode}.";
                logger.SendTrace(message, LogLevel.Error);
                throw new ArgumentNullException(message);
            }
            return macExpander.ExpandMac(mac, firstField);
        }

        public async Task<Mac> GetMacCode(string macCode)
        {
            // Instrumented on the MISS path only, because that is the only path that costs anything: a cache hit is a
            // dictionary lookup, a miss is a Table Storage point lookup. GenerateMacCache (which would bulk-load the
            // cache upfront) has no call sites, so during a data refresh every distinct MAC's first touch pays for a
            // round trip - and Table Storage dependency auto-collection is inactive in the isolated worker, so this is
            // the only way that flood is visible at all.
            //
            // The counter is therefore a count of DISTINCT MACs touched, which is the size of the flood; the duration
            // is what one of them costs, in-region.
            return await cache.GetOrAddAsync(macCode, async () =>
            {
                logger.SendMetric(
                    DataRefreshMetrics.CountMetric,
                    1,
                    DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_MacCacheMisses));

                using (logger.TimeOperationAsMetric(
                           DataRefreshMetrics.DurationMsMetric,
                           DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_MacLookup)))
                {
                    return await macRepository.GetMac(macCode);
                }
            });
        }
        
        public async Task GenerateMacCache()
        {
            var macs = await macRepository.GetAllMacs();
            foreach (var mac in macs)
            {
                cache.GetOrAdd(mac.Code, () => mac);
            }
        }
    }
}