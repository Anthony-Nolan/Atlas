using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using AwesomeAssertions;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using NUnit.Framework;

namespace Atlas.Common.Test.Caching
{
    /// <summary>
    /// Locks in the caching behaviour that HLA Metadata Dictionary invalidation depends on: evicting a key while a
    /// read-through fill for it is still running is effective, and the fill does not put the old data back.
    /// </summary>
    /// <remarks>
    /// The concern this answers is a real one for read-through caching in general - fetch first, write second, so an
    /// eviction landing between the two steps looks like it should be undone by the write. It does not happen here
    /// because LazyCache commits the cache entry SYNCHRONOUSLY, holding an unresolved lazy, and only then awaits the
    /// factory. The entry is therefore present and evictable for the whole duration of the fetch, and resolving the
    /// lazy afterwards writes nothing further to the cache.
    ///
    /// <para>
    /// That is a property of LazyCache, not of our code, and nothing else would notice if it changed - the symptom
    /// would be silently stale HLA metadata for a full cache lifetime. Swapping the
    /// caching library, or reaching past <see cref="LazyCache.IAppCache"/> to <c>MemoryCache.GetOrCreateAsync</c>
    /// (which commits only after its factory completes), would break it. Hence these tests.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class InvalidationDuringCacheFillTests
    {
        private const string Key = "hlaMatchingLookup:3650";

        private IPersistentCacheProvider cacheProvider;

        [SetUp]
        public void SetUp() => cacheProvider = AppCacheBuilder.NewPersistentCacheProvider();

        private void InvalidateRecreatedVersion() => cacheProvider.RemoveWhere(k => HlaVersionedCacheKey.Matches(k, "3650"));

        /// <summary>
        /// The premise of the whole mechanism: an in-flight fill is visible to eviction. If this fails, invalidation
        /// silently misses any key currently being fetched.
        /// </summary>
        [Test]
        public async Task AnInFlightFill_IsVisibleToInvalidation()
        {
            var fetchReachedStorage = new TaskCompletionSource();
            var releaseFetch = new TaskCompletionSource();

            var fill = cacheProvider.Cache.GetOrAddWholeCollectionAsync_Tracked(
                Key,
                async () =>
                {
                    fetchReachedStorage.SetResult();
                    await releaseFetch.Task;
                    return new List<string> { "data-from-before-the-recreation" };
                });

            await fetchReachedStorage.Task;

            var keysVisibleMidFetch = KeysCurrentlyCached();

            releaseFetch.SetResult();
            await fill;

            keysVisibleMidFetch.Should().Contain(Key,
                "eviction enumerates cache keys, so a fill that is not yet visible could not be invalidated");
        }

        [Test]
        public async Task InvalidatingMidFetch_DoesNotLeaveStaleDataCached()
        {
            var fetchReachedStorage = new TaskCompletionSource();
            var releaseFetch = new TaskCompletionSource();

            var fill = cacheProvider.Cache.GetOrAddWholeCollectionAsync_Tracked(
                Key,
                async () =>
                {
                    fetchReachedStorage.SetResult();
                    await releaseFetch.Task;
                    return new List<string> { "data-from-before-the-recreation" };
                });

            await fetchReachedStorage.Task;
            InvalidateRecreatedVersion();
            releaseFetch.SetResult();
            await fill;

            cacheProvider.Cache.Get<List<string>>(Key).Should().BeNull(
                "a fetch that began before the invalidation must not repopulate the cache after it");
        }

        /// <summary>The same guarantee for the per-lookup entries, which is where the bug actually bit.</summary>
        [Test]
        public async Task InvalidatingMidLookup_DoesNotLeaveAStaleOutcomeCached()
        {
            const string lookupKey = "hlaScoringLookup-3650-A-0265";
            var lookupReachedStorage = new TaskCompletionSource();
            var releaseLookup = new TaskCompletionSource();

            // The same overload MetadataServiceBase uses, expiry options included - a different overload would not
            // be evidence about the path that actually caches HLA lookup outcomes.
            var lookup = cacheProvider.Cache.GetOrAddAsync(
                lookupKey,
                async () =>
                {
                    lookupReachedStorage.SetResult();
                    await releaseLookup.Task;
                    return "not-found-before-the-recreation";
                },
                new MemoryCacheEntryOptions { AbsoluteExpiration = DateTimeOffset.Now.AddDays(1) });

            await lookupReachedStorage.Task;
            InvalidateRecreatedVersion();
            releaseLookup.SetResult();
            await lookup;

            cacheProvider.Cache.Get<string>(lookupKey).Should().BeNull();
        }

        [Test]
        public async Task AnUninterruptedFill_StillCaches()
        {
            var collection = await cacheProvider.Cache.GetOrAddWholeCollectionAsync_Tracked(
                Key,
                () => Task.FromResult(new List<string> { "current-data" }));

            collection.Should().ContainSingle();
            cacheProvider.Cache.Get<List<string>>(Key).Should().NotBeNull();
        }

        /// <summary>An invalidation for a different version must not disturb a fill that is still valid.</summary>
        [Test]
        public async Task InvalidatingAnotherVersionMidFetch_LeavesThisFillCached()
        {
            var fetchReachedStorage = new TaskCompletionSource();
            var releaseFetch = new TaskCompletionSource();

            var fill = cacheProvider.Cache.GetOrAddWholeCollectionAsync_Tracked(
                Key,
                async () =>
                {
                    fetchReachedStorage.SetResult();
                    await releaseFetch.Task;
                    return new List<string> { "current-data" };
                });

            await fetchReachedStorage.Task;
            cacheProvider.RemoveWhere(k => HlaVersionedCacheKey.Matches(k, "3660"));
            releaseFetch.SetResult();
            await fill;

            cacheProvider.Cache.Get<List<string>>(Key).Should().NotBeNull(
                "during a data refresh the previous version is still being served, and must keep its warm cache");
        }

        private List<string> KeysCurrentlyCached()
        {
            var keys = new List<string>();
            cacheProvider.RemoveWhere(key =>
            {
                keys.Add(key);
                return false; // Observe only.
            });
            return keys.ToList();
        }
    }
}
