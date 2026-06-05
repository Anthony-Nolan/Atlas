using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;

namespace Atlas.Common.Caching
{
    /// <inheritdoc cref="ICacheProvider"/>
    public interface IPersistentCacheProvider : ICacheProvider { }
    /// <inheritdoc cref="ICacheProvider"/>
    public interface ITransientCacheProvider : ICacheProvider { }

    /// <summary>
    /// Used to explicitly state the scope of the cache your rely upon; Transient or Persistent as appropriate.
    /// </summary>
    public interface ICacheProvider
    {
        IAppCache Cache { get; }

        void ClearCache();
    }

    public class PersistentCacheProvider : IPersistentCacheProvider
    {
        public IAppCache Cache { get; set; }
        public PersistentCacheProvider(IAppCache cache) { Cache = cache; }

        public void ClearCache()
        {
            Cache.CacheProvider.Dispose();

            Cache = new CachingService(new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions())))
            {
                DefaultCachePolicy = new CacheDefaults
                {
                    DefaultCacheDurationSeconds = int.MaxValue
                }
            };
        }
    }

    public class TransientCacheProvider : ITransientCacheProvider
    {
        public IAppCache Cache { get; }
        public TransientCacheProvider(IAppCache cache) { Cache = cache; }

        public void ClearCache()
        {
            throw new System.NotImplementedException();
        }
    }
}