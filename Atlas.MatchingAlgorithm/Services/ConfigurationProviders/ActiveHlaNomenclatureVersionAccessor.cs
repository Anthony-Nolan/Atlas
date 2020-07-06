using System;
using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using LazyCache;

namespace Atlas.MatchingAlgorithm.Services.ConfigurationProviders
{
    public interface IActiveHlaNomenclatureVersionAccessor
    {

        /// <summary>
        /// Indicates whether or not an active HLA version exists.
        /// </summary>
        /// <returns>
        /// If the active value doesn't exist, or is empty, returns false;
        /// Otherwise true
        /// </returns>
        bool DoesActiveHlaNomenclatureVersionExist();

        /// <returns>
        /// The version of the HLA Nomenclature used to populate the current Transient donor database.
        /// If the transient database has not yet been populated, calling this will result in an Exception.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if the active value is empty.</exception>
        string GetActiveHlaNomenclatureVersion();
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

        public bool DoesActiveHlaNomenclatureVersionExist()
        {
            var version = cache.GetOrAdd(ActiveVersionCacheKey, () => dataRefreshHistoryRepository.GetActiveHlaNomenclatureVersion());
            return IsDefined(version);
        }

        public string GetActiveHlaNomenclatureVersion()
        {
            var version = cache.GetOrAdd(ActiveVersionCacheKey, () => dataRefreshHistoryRepository.GetActiveHlaNomenclatureVersion());
            ThrowIfNull(version, ActiveVersionCacheKey);
            return version;
        }

        private void ThrowIfNull(string wmdaDatabaseVersion, string key)
        {
            if (!IsDefined(wmdaDatabaseVersion))
            {
                throw new ArgumentNullException(nameof(wmdaDatabaseVersion),
                    $"Attempted to retrieve the {key}, but found <{wmdaDatabaseVersion}>. This is never an appropriate value, under any circumstances, and would definitely cause myriad problems elsewhere.");
            }
        }

        /// <remarks>
        /// Note that this isn't attempting to check whether the version is *VALID*, just whether or not we've got something that purports to be a version.
        /// </remarks>
        private bool IsDefined(string version) => !string.IsNullOrWhiteSpace(version);
    }
}