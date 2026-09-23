using Atlas.Common.Caching;
using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;

namespace Atlas.Common.Test.SharedTestHelpers.Builders;

public static class AppCacheBuilder
{
    public static IAppCache NewDefaultCache() => NewCacheOver(new MemoryCache(new MemoryCacheOptions()));

    public static IAppCache NewCacheOver(MemoryCache underlyingCache) => new CachingService(new MemoryCacheProvider(underlyingCache));

    /// <remarks>
    /// Built over a cache it keeps a reference to, so that <see cref="IPersistentCacheProvider.RemoveWhere"/> works -
    /// eviction by predicate has to enumerate keys, which the <see cref="IAppCache"/> alone cannot do.
    /// </remarks>
    public static IPersistentCacheProvider NewPersistentCacheProvider()
    {
        var underlyingCache = new MemoryCache(new MemoryCacheOptions());
        return new PersistentCacheProvider(NewCacheOver(underlyingCache), underlyingCache);
    }

    public static ITransientCacheProvider NewTransientCacheProvider() => new TransientCacheProvider(NewDefaultCache());
}
