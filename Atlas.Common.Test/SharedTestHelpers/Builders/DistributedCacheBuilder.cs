using System;
using Atlas.Common.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Atlas.Common.Test.SharedTestHelpers.Builders;

/// <summary>
/// Distributed caches for tests, kept in the memory of the test process - no Redis.
/// </summary>
public static class DistributedCacheBuilder
{
    /// <summary>A distributed cache of its own: what a test stores in it, the test can read back.</summary>
    public static IDistributedCacheProvider NewProvider() => NewProviderOver(NewSharedStore());

    /// <summary>
    /// The store of a distributed cache. Give one store to several <see cref="NewProviderOver"/> calls, to get several
    /// processes that share one Redis.
    /// </summary>
    public static IDistributedCache NewSharedStore() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    public static IDistributedCacheProvider NewProviderOver(IDistributedCache store) =>
        new DistributedCacheProvider(store, new NullDistributedCacheMetrics(), NullLogger<DistributedCacheProvider>.Instance, TimeProvider.System);

    private sealed class NullDistributedCacheMetrics : IDistributedCacheMetrics
    {
        public void RecordOperation(string cacheName, DistributedCacheOperation operation, DistributedCacheResult result, TimeSpan duration)
        {
        }

        public void RecordValueSize(string cacheName, DistributedCacheOperation operation, int bytes)
        {
        }
    }
}
