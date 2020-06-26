﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.MultipleAlleleCodeDictionary.Services;
using LazyCache;

namespace Atlas.MultipleAlleleCodeDictionary.MacCacheService
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
        private readonly ILogger logger;
        private readonly IMacRepository macRepository;
        private readonly IMacExpander macExpander;

        public MacCacheService(ILogger logger, IPersistentCacheProvider cacheProvider, IMacRepository macRepository, IMacExpander macExpander)
        {
            this.logger = logger;
            this.cache = cacheProvider.Cache;
            this.macRepository = macRepository;
            this.macExpander = macExpander;
        }

        public async Task<IEnumerable<string>> GetHlaFromMac(string macCode, string firstField)
        {
            var mac = await GetMacCode(macCode);
            logger.SendTrace($"Attempting to expand Hla for Mac: {mac.Code}", LogLevel.Verbose);
            return macExpander.ExpandMac(mac, firstField);
        }

        public async Task<Mac> GetMacCode(string macCode)
        {
            return await cache.GetOrAddAsync(macCode, () => macRepository.GetMac(macCode));
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