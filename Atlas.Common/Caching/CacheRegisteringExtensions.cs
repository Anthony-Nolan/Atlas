using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Common.Caching
{
    public static class CacheRegisteringExtensions
    {
        public static void RegisterLifeTimeScopedCacheTypes(this IServiceCollection services, int? transientCacheLifetimeOverride = null, int? persistentCacheLifetimeOverride = null)
        {
            // The TransientCache is a TransientScoped (shocker!), to allow for caching of data
            // that is repeatedly accessed, but should be read a-fresh on each request.
            // Most notably this is necessary for anything relating to the hot-swapping of the
            // Donor Matching Database, e.g. ActiveHlaNomenclatureVersion.
            // The cache should never expire. If a request is long running then it still shouldn't change.
            services.AddTransient<ITransientCacheProvider, TransientCacheProvider>(sp =>
            {
                const int tenYears = 60 * 60 * 24 * 365 * 10;
                var lifeTime = transientCacheLifetimeOverride ?? tenYears;

                return new TransientCacheProvider(MakeCache(lifeTime));
            });

            // The PersistentCache is SingletonScoped, so that it is shared across requests, thus
            // avoiding re-caching large collections e.g. Metadata Dictionary and Alleles each request.
            // It is reloaded once a day to ensure it doesn't get problematically stale.
            services.AddSingleton<IPersistentCacheProvider, PersistentCacheProvider>(sp =>
            {
                const int oneDay = 60 * 60 * 24;
                var lifeTime = persistentCacheLifetimeOverride ?? oneDay;

                return new PersistentCacheProvider(MakeCache(lifeTime));
            });
        }

        private static CachingService MakeCache(int lifeTimeSeconds)
        {
            return new CachingService(new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions())))
            {
                DefaultCachePolicy = new CacheDefaults
                {
                    DefaultCacheDurationSeconds = lifeTimeSeconds,
                }
            };
        }
    }
}
