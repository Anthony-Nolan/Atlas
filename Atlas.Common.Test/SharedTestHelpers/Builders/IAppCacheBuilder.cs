using Atlas.Common.Caching;
using LazyCache;
using LazyCache.Providers;
using LochNessBuilder;
using Microsoft.Extensions.Caching.Memory;

namespace Atlas.Common.Test.SharedTestHelpers.Builders
{
    [Builder]
    public static class AppCacheBuilder
    {
        public static IAppCache NewDefaultCache() => new CachingService(new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions())));
        
        public static IPersistentCacheProvider NewPersistentCacheProvider() => new PersistentCacheProvider(NewDefaultCache());
        public static ITransientCacheProvider NewTransientCacheProvider() => new TransientCacheProvider(NewDefaultCache());
    }
}