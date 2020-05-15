using System;
using Atlas.Common.Caching;
using LazyCache;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;

namespace Atlas.MatchingAlgorithm.Services.ConfigurationProviders
{
    public interface IActiveHlaVersionAccessor
    {
        /// <returns>The version of the wmda hla data used to populate the current Transient donor database</returns>
        string GetActiveHlaDatabaseVersion();
    }

    public class ActiveHlaVersionAccessor : IActiveHlaVersionAccessor
    {
        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private readonly IAppCache cache;

        public ActiveHlaVersionAccessor(
            IDataRefreshHistoryRepository dataRefreshHistoryRepository,
            ITransientCacheProvider cacheProvider)
        {
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            cache = cacheProvider.Cache;
        }

        public string GetActiveHlaDatabaseVersion()
        {
            const string key = "activeWmdaVersion";
            var version = cache.GetOrAdd(key, () => dataRefreshHistoryRepository.GetActiveWmdaDataVersion());
            ThrowIfNull(version, key);
            return version;
        }

        private void ThrowIfNull(string wmdaDatabaseVersion, string key)
        {
            if (string.IsNullOrWhiteSpace(wmdaDatabaseVersion))
            {
                throw new ArgumentNullException(nameof(wmdaDatabaseVersion),
                    $"Attempted to retrieve the {key}, but found <{wmdaDatabaseVersion}>. This is never an appropriate value, under any circumstances, and would definitely cause myriad problems elsewhere.");
            }
        }
    }
}