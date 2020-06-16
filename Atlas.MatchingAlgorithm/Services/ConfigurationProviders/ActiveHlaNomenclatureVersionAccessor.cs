using System;
using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using LazyCache;

namespace Atlas.MatchingAlgorithm.Services.ConfigurationProviders
{
    public interface IActiveHlaNomenclatureVersionAccessor
    {
        /// <returns>
        /// The version of the HLA Nomenclature used to populate the current Transient donor database.
        /// If the transient database has not yet been populated, calling this will result in an Exception.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if the active value is empty.</exception>
        string GetActiveHlaNomenclatureVersion();
        
        /// <returns>
        /// If the active value doesn't exist, or is empty, returns the default value, which is a dummy "un-initialised" value.
        /// The only time this is expected is when the data refresh has never been run.
        /// Otherwise, returns the active version.
        /// </returns>
        string GetActiveHlaNomenclatureVersionOrDefault();
    }

    public class ActiveHlaNomenclatureVersionAccessor : IActiveHlaNomenclatureVersionAccessor
    {
        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private readonly IAppCache cache;

        private const string ActiveVersionCacheKey = "activeWmdaVersion";

        public ActiveHlaNomenclatureVersionAccessor(
            IDataRefreshHistoryRepository dataRefreshHistoryRepository,
            ITransientCacheProvider cacheProvider)
        {
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            cache = cacheProvider.Cache;
        }

        public string GetActiveHlaNomenclatureVersion()
        {
            var version = cache.GetOrAdd(ActiveVersionCacheKey, () => dataRefreshHistoryRepository.GetActiveHlaNomenclatureVersion());
            ThrowIfNull(version, ActiveVersionCacheKey);
            return version;
        }

        /// <inheritdoc />
        public string GetActiveHlaNomenclatureVersionOrDefault()
        {
            var version = cache.GetOrAdd(ActiveVersionCacheKey, () => dataRefreshHistoryRepository.GetActiveHlaNomenclatureVersion());
            return string.IsNullOrWhiteSpace(version) ? HlaMetadataDictionaryConstants.NoActiveVersionValue : version;
        }

        private static void ThrowIfNull(string wmdaDatabaseVersion, string key)
        {
            if (string.IsNullOrWhiteSpace(wmdaDatabaseVersion))
            {
                throw new ArgumentNullException(nameof(wmdaDatabaseVersion),
                    $"Attempted to retrieve the {key}, but found <{wmdaDatabaseVersion}>. This is never an appropriate value, under any circumstances, and would definitely cause myriad problems elsewhere.");
            }
        }
    }
}