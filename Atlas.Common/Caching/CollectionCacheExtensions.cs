using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.Utils.Concurrency;
using LazyCache;

namespace Atlas.Common.Caching
{
    /// <summary>
    /// In many cases we cache a large collection, are it is more efficient to fetch the *entire* dataset in one go.
    /// e.g. reading an AzureStorage Table.
    ///
    /// However, when taking this approach the initial collection fetch often takes quite a long time - a lot longer
    /// than pulling out a single value would take.
    /// In these cases, if what we want right now is a single record from the collection and the collection has not
    /// yet been cached, then we'd prefer to fetch that single element directly, rather than wait for the full
    /// collection caching to complete.
    ///
    /// But we still want the kick off the caching of the full collection in the background.
    ///
    /// ** These extension methods allow to have that workflow. **
    /// </summary>
    public static class CollectionCacheExtensions
    {
        /// <summary>
        /// ** SEE CLASS DOC-COMMENT **
        /// We have to manually keep track of which collections are in the process of being populated, because
        /// LazyCache has no way of checking that.
        ///
        /// LazyCache will do one of three things:
        /// * return the value (if the key exists in the cache)
        /// * return null (if the key is not in the cache AND there is no process currently populating that key)
        /// * wait (if the key is currently being populated)
        ///
        /// We want to be able to return a bool indicating whether or not a collection is being populated, without
        /// waiting if it is.
        /// </summary>
        private static readonly ConcurrentDictionary<string, bool> CollectionCacheKeysBeingPopulatedInBackground =
            new ConcurrentDictionary<string, bool>();

        // Azure applications have a limitation on the number of concurrently open connections.
        // see: https://docs.microsoft.com/en-us/azure/app-service/troubleshoot-intermittent-outbound-connection-errors#:~:text=TCP%20Connections%3A%20There%20is%20a,connections%20that%20can%20be%20made.&text=Each%20instance%20on%20Azure%20App,same%20host%20and%20port%20combination.
        // Each instance is allocated 128 SNAT ports. We should stay well below this limit here to allow for other connection types. 
        private const int MaxConcurrentSingleItemFetches = 16;

        // Fetching a single item will, in most use cases, open a connection (e.g. fetching data from azure storage)
        // Azure services have a limitation on the number of concurrent open connections, and saturating this allowance causes socket exceptions
        //
        // As calls to this helper can be nested, to avoid deadlocks, we limit threads per-collection cache key
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> SingleItemFetchingSemaphores =
            new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <returns>
        /// Returns <c>true</c> if the key is in the process of being cached.
        /// Returns <c>false</c> if the key is already present, or if caching has not yet been initiated.
        /// </returns>
        private static bool CollectionIsCurrentlyBeingCached(string collectionCacheKey) =>
            CollectionCacheKeysBeingPopulatedInBackground.TryGetValue(collectionCacheKey, out _);

        /// <summary>
        /// ** SEE CLASS DOC-COMMENT **
        ///
        /// Intended Usage:
        ///=================
        /// To be used if you want to fetch the *entirety* of a collection (and use large portions of it), but
        /// individual elements from that collection are ALSO accessed elsewhere.
        /// This method MUST be used (instead of on <see cref="IAppCache.GetOrAddAsync"/>) for any cached collection
        /// which is also being accessed via <see cref="GetSingleItemAndScheduleWholeCollectionCacheWarm{TItem,TCollection}"/>, in order 
        /// to preserve the behaviour of the latter.
        ///
        /// Behaviour:
        ///============
        /// From the point of view of the consumer of this method alone, this method acts exactly as <see cref="IAppCache.GetOrAddAsync"/>.
        /// If the provided key is in the cache, it returns the collection associated with that key
        /// If the provided key is NOT in the cache, it invokes the <c>async</c> Factory function, <c>await</c>s that
        /// function and returns the Result (having added it to the cache).
        ///
        /// Purpose:
        ///==========
        /// Without this method, if code A asks for the whole collection, and the shortly later code B asks for a single
        /// item from the same collection, then code B has no way to know that the (slow) fetch has already been initiated.
        /// As documented on <see cref="CollectionCacheKeysBeingPopulatedInBackground"/> LazyCache offers no way to detect
        /// that without waiting.
        /// So we require code A to use this method, so that the fetch gets recorded, and thus code B knows about it.
        /// </summary>
        /// <typeparam name="TCollection">
        /// Type of the collection to be fetched/returned.
        /// Constrained to implement <see cref="ICollection"/>, which isn't technically *required* by the logic of the method,
        /// but if it's not true you're probably mis-using these methods.
        /// (If you have a real use-case and have though about these methods in detail feel free to remove this constraint.)
        /// </typeparam>
        /// <param name="cache">The underlying cache</param>
        /// <param name="collectionCacheKey">The cache key by which the collection is stored</param>
        /// <param name="fetchFullCollection">An async callback that will generate the full collection for caching. Do not return null, if avoidable.</param>
        /// <returns>The collection produces by the fetch method. From a cache, if possible.</returns>
        public static async Task<TCollection> GetOrAddWholeCollectionAsync_Tracked<TCollection>(
            this IAppCache cache,
            string collectionCacheKey,
            Func<Task<TCollection>> fetchFullCollection
        ) where TCollection : ICollection // See typeparam comment.
        {
            var value = await cache.GetAsync<TCollection>(collectionCacheKey);

            // ReSharper disable once ConvertIfStatementToNullCoalescingExpression
            if (value == null)
            {
                value = await cache.GenerateAndCacheCollectionWithTracking(collectionCacheKey, fetchFullCollection, true);
            }

            return value;
        }

        /// <summary>
        /// ** SEE CLASS DOC-COMMENT **
        ///
        /// Behaviour:
        ///============
        /// If the provided collection Key is present in the cache, reads the single value from that collection.
        /// If not, fetches the single value directly from the source, and kicks off a caching process to fetch the
        /// collection in the background.
        ///
        /// Purpose:
        ///==========
        /// See class-level docComment on <see cref="CollectionCacheExtensions"/>
        /// Get very quick access once the cache is setup, but still have quick-ish access during the warm-up period.
        /// </summary>
        /// <typeparam name="TCollection">
        /// Type of the cached collection.
        /// Constrained to implement <see cref="ICollection"/>, which isn't technically *required* by the logic of the method,
        /// but if it's not true you're probably mis-using these methods.
        /// (If you have a real use-case and have though about these methods in detail feel free to remove this constraint.)
        /// </typeparam>
        /// <typeparam name="TItem">Type of the single item we want to retrieve from the cached collection</typeparam>
        /// <param name="cache">The underlying cache</param>
        /// <param name="collectionCacheKey">The cache key by which the collection is stored</param>
        /// <param name="fetchFullCollection">
        /// An async callback that will fetch the full collection for caching.
        /// WARNING! This will NOT be <c>await</c>ed, so the calling code is responsible for catching and logging any exceptions.
        /// </param>
        /// <param name="readSingleItemFromCollection">A synchronous callback that can read the desired item from the collection, if it has already been cached.</param>
        /// <param name="fetchSingleItemDirectly">An async callback that can fetch the desired item directly without using the collection</param>
        public static async Task<TItem> GetSingleItemAndScheduleWholeCollectionCacheWarm<TItem, TCollection>(
            this IAppCache cache,
            string collectionCacheKey,
            Func<Task<TCollection>> fetchFullCollection,
            Func<TCollection, TItem> readSingleItemFromCollection,
            Func<Task<TItem>> fetchSingleItemDirectly
        ) where TCollection : ICollection // See typeparam comment.
        {
            var semaphore = GetCollectionSemaphore(collectionCacheKey);

            // The cache of this collection is currently being populated for key, do not either wait for cache to complete, or kick off new cache population.
            if (CollectionIsCurrentlyBeingCached(collectionCacheKey))
            {
                using (await semaphore.SemaphoreSlot())
                {
                    return await fetchSingleItemDirectly();
                }
            }

            //See docs on CollectionCacheKeysBeingPopulatedInBackground, for the possible return values here.
            var cachedCollection = await cache.GetAsync<TCollection>(collectionCacheKey);
            if (cachedCollection == null)
            {
                // Item is neither in the cache, nor being populated.
                using (await semaphore.SemaphoreSlot())
                {
                    // So the first thing we want to do is initiate a read of the single item.
                    var desiredSingleItemTask = fetchSingleItemDirectly();
                    // Then, kick off the larger cache warming process.
                    // We explicitly do NOT await this! We want it to run **in the background**, so as not to delay our single item returning.
                    // Errors are the calling code's responsibility.
#pragma warning disable 4014
                    _ = Task.Factory.StartNew((Action) (() => cache.GenerateAndCacheCollectionWithTracking(collectionCacheKey, fetchFullCollection)));
#pragma warning restore 4014

                    return await desiredSingleItemTask;
                }
            }

            return readSingleItemFromCollection(cachedCollection);
        }

        private static SemaphoreSlim GetCollectionSemaphore(string collectionCacheKey)
        {
            if (!SingleItemFetchingSemaphores.TryGetValue(collectionCacheKey, out var semaphore))
            {
                semaphore = new SemaphoreSlim(MaxConcurrentSingleItemFetches, MaxConcurrentSingleItemFetches);
                SingleItemFetchingSemaphores[collectionCacheKey] = semaphore;
            }

            return semaphore;
        }

        private static async Task<TCollection> GenerateAndCacheCollectionWithTracking<TCollection>(
            this IAppCache cache,
            string collectionCacheKey,
            Func<Task<TCollection>> fetchFullCollection,
            bool rethrowErrors = false
        ) where TCollection : ICollection // See typeparam comment.
        {
            CollectionCacheKeysBeingPopulatedInBackground.TryAdd(collectionCacheKey, true);
            try
            {
                return await cache.GetOrAddAsync(collectionCacheKey, fetchFullCollection);
            }
            catch (Exception e)
            {
                var message =
                    $"Exception thrown in {nameof(GenerateAndCacheCollectionWithTracking)}, with {nameof(collectionCacheKey)}: '{collectionCacheKey}'. Exception details: {e.ToString()}";
                Console.WriteLine(message);
                System.Diagnostics.Debug.WriteLine(message);
                //TODO: ATLAS-542 Create static Logger?
                if (rethrowErrors)
                {
                    throw;
                }

                return default;
            }
            finally
            {
                CollectionCacheKeysBeingPopulatedInBackground.Remove(collectionCacheKey, out _);
            }
        }
    }
}