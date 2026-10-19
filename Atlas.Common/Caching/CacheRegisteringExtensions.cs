using System;
using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

                // The underlying cache is kept alongside the IAppCache built over it, so that entries can be evicted
                // by predicate - see IPersistentCacheProvider.RemoveWhere, used to drop HLA Metadata Dictionary data
                // when the dictionary is recreated.
                var underlyingCache = new MemoryCache(new MemoryCacheOptions());

                return new PersistentCacheProvider(MakeCache(lifeTime, underlyingCache), underlyingCache);
            });
        }

        /// <summary>
        /// Registers <see cref="IDistributedCacheProvider"/>: the distributed cache in Redis, or a cache that keeps nothing
        /// when <see cref="DistributedCacheSettings.ConnectionString"/> is empty.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Safe to call more than once, e.g. from two components of the same host: only the first call registers.
        /// </para>
        ///
        /// <para>
        /// Nothing connects to Redis here, nor when the provider is constructed: the connection opens on the first read or
        /// write. A connection string that is not valid is logged as an error, and the app then runs without the cache,
        /// as the cache is never a dependency.
        /// </para>
        ///
        /// <para>
        /// Deliberately does not register <see cref="IDistributedCache"/>: other libraries would pick it up, and callers
        /// could reach Redis around the provider, which keeps the faults of Redis from them.
        /// </para>
        /// </remarks>
        public static void RegisterDistributedCacheTypes(
            this IServiceCollection services,
            Func<IServiceProvider, DistributedCacheSettings> fetchDistributedCacheSettings)
        {
            services.TryAddSingleton(TimeProvider.System);
            services.TryAddSingleton<IDistributedCacheMetrics, DistributedCacheMetrics>();
            services.TryAddSingleton(sp => MakeDistributedCacheProvider(sp, fetchDistributedCacheSettings(sp)));
        }

        private static IDistributedCacheProvider MakeDistributedCacheProvider(IServiceProvider serviceProvider, DistributedCacheSettings settings)
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                return new DisabledDistributedCacheProvider(loggerFactory.CreateLogger<DisabledDistributedCacheProvider>());
            }

            try
            {
                // To find a connection string that is not valid now. Each connection attempt builds its own options.
                RedisConnection.BuildOptions(settings);
            }
            catch (ArgumentException exception)
            {
                // Not the connection string itself: a connection string can hold a password.
                loggerFactory.CreateLogger<DistributedCacheProvider>().LogError(
                    exception,
                    "The distributed cache connection string is not valid. The app runs without the distributed cache.");
                return new DisabledDistributedCacheProvider(loggerFactory.CreateLogger<DisabledDistributedCacheProvider>());
            }

            var store = new RedisCache(Options.Create(new RedisCacheOptions
            {
                ConnectionMultiplexerFactory = RedisConnection.ConnectionFactory(settings)
            }));

            return new DistributedCacheProvider(
                store,
                serviceProvider.GetRequiredService<IDistributedCacheMetrics>(),
                loggerFactory.CreateLogger<DistributedCacheProvider>(),
                serviceProvider.GetRequiredService<TimeProvider>());
        }

        private static CachingService MakeCache(int lifeTimeSeconds, MemoryCache underlyingCache = null)
        {
            return new CachingService(new MemoryCacheProvider(underlyingCache ?? new MemoryCache(new MemoryCacheOptions())))
            {
                DefaultCachePolicy = new CacheDefaults
                {
                    DefaultCacheDurationSeconds = lifeTimeSeconds
                }
            };
        }
    }
}
