using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;

namespace Atlas.MultipleAlleleCodeDictionary.Services
{
    public interface IMacCacheService
    {
        Task<IEnumerable<string>> GetHlaFromMac(string firstField, string macCode);
        Task<Mac> GetMacCode(string macCode);

        /// <inheritdoc cref="ExternalInterface.IMacDictionary.PreWarmAllMacs" />
        Task PreWarmAllMacs();
    }

    internal class MacCacheService : IMacCacheService
    {
        private readonly IAtlasLogger logger;
        private readonly IMacStore macStore;
        private readonly IMacRepository macRepository;
        private readonly IMacExpander macExpander;

        public MacCacheService(IAtlasLogger logger, IMacStore macStore, IMacRepository macRepository, IMacExpander macExpander)
        {
            this.logger = logger;
            this.macStore = macStore;
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
            if (macStore.TryGetMac(macCode, out var storedMac))
            {
                return new Mac(macCode, storedMac.Hla, storedMac.IsGeneric);
            }

            // Either the store has not been warmed, or this MAC was published after it was.
            // Either way, one point lookup, and then remember it.
            var mac = await macRepository.GetMac(macCode);
            if (mac != null)
            {
                macStore.AddMac(mac.Code, new MacValue(mac.Hla, mac.IsGeneric));
            }

            return mac;
        }

        /// <inheritdoc />
        public Task PreWarmAllMacs() => macStore.WarmOnce(FillStoreFromRepository);

        private async Task FillStoreFromRepository()
        {
            using (logger.RunTimed("MAC DICTIONARY: Loading all MACs into memory", logAtStart: true))
            {
                // Streamed rather than fetched via GetAllMacs, so that only one page of the table is alive at a time,
                // instead of the whole table plus the store being filled from it.
                await foreach (var mac in macRepository.StreamAllMacs())
                {
                    macStore.AddMac(mac.Code, new MacValue(mac.Hla, mac.IsGeneric));
                }
            }

            logger.SendTrace($"MAC DICTIONARY: {macStore.Count} MACs held in memory.");
        }
    }
}
