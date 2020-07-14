using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LazyCache;

namespace Atlas.Common.Caching
{
    public static class CacheExtensions
    {
        /// <summary>
        /// Manually keep track of when cache keys are in the process of being populated.
        /// Some cached collections take a long time to generate, and LazyCache has no way of checking whether a cache is currently being populated:
        /// instead it will wait for the population to complete, then return a value (rather than returning null, which indicates both "no value" and "no-in progress cache population")
        /// </summary>
        private static readonly List<string> CacheKeysBeingPopulatedInBackground = new List<string>();

        /// <summary>
        /// Intended for use in conjunction with <see cref="GetAndScheduleFullCacheWarm{TItem,TCollection}"/>.
        /// Use this when calling `GetOrAddAsync` on a cacheKey that is elsewhere accessed via <see cref="GetAndScheduleFullCacheWarm{TItem,TCollection}"/> 
        ///
        /// In some cases, both the full collection cache + the single item fetch method will depend on another cached collection - which
        /// may not be itself appropriate for use of <see cref="GetAndScheduleFullCacheWarm{TItem,TCollection}"/>
        ///
        /// Some caches take a long time to generate, but if the operation to populate it has begun, GetOrAdd will wait for the value to be populated,
        /// rather than returning null. We do not want the "fetch item directly" behaviour to be slowed down by this, so we track currently-caching keys.
        ///
        /// This method allows us to track keys even when we do not use the background cache population method. 
        /// </summary>
        public static async Task<T> GetOrAddAsync_Tracked<T>(this IAppCache cache, string cacheKey, Func<Task<T>> addItemFactory)
        {
            var value = await cache.GetAsync<T>(cacheKey);
            if (value == null)
            {
                CacheKeysBeingPopulatedInBackground.Add(cacheKey);
                value = await cache.GetOrAddAsync(cacheKey, addItemFactory);
                CacheKeysBeingPopulatedInBackground.Remove(cacheKey);
            }

            return value;
        }

        /// <summary>
        /// In some situations, warming a cache can take a long time - e.g. pulling ina large collection from external storage.
        /// In these cases, fetching an element from the cache is quicker once warmed,
        /// but if not warmed fetching an individual item can be faster than waiting for the whole collection to cache.
        ///
        /// This method triggers a background population of a full cache, while allowing a quick retrieval from the un-warmed cache.
        /// </summary>
        /// <typeparam name="TCollection">Type of the cached collection</typeparam>
        /// <typeparam name="TItem">Type of the item we want to retrieve from the cached collection</typeparam>
        /// <param name="cache">The underlying cache</param>
        /// <param name="cacheKey">The cache key by which the collection is stored</param>
        /// <param name="generateFullCollection">A callback that will generate the full collection for caching</param>
        /// <param name="getItemFromCollection">A callback that can fetch the desired item from the cached collection</param>
        /// <param name="getItemDirectly">A callback that can fetch the desired item while bypassing the cache</param>
        public static async Task<TItem> GetAndScheduleFullCacheWarm<TItem, TCollection>(
            this IAppCache cache,
            string cacheKey,
            Func<Task<TCollection>> generateFullCollection,
            Func<TCollection, TItem> getItemFromCollection,
            Func<Task<TItem>> getItemDirectly
        )
        {
            // If cache currently being populated for key, do not either wait for cache to complete, or kick off new cache population.
            if (CacheKeysBeingPopulatedInBackground.Contains(cacheKey))
            {
                return await getItemDirectly();
            }

            // cache will return null if item is not present, and item if fully present.
            // If currently populating, it will wait for the cache to populate, then return the item.
            var cachedCollection = await cache.GetAsync<TCollection>(cacheKey);
            if (cachedCollection != null)
            {
                return getItemFromCollection(cachedCollection);
            }

            // Explicitly do not await this! We want it to complete in the background while we fetch the inner item the fast way.
            _ = Task.Factory.StartNew(() => cache.GenerateAndCacheCollection(cacheKey, generateFullCollection));

            return await getItemDirectly();
        }

        private static async Task GenerateAndCacheCollection<TCollection>(
            this IAppCache cache,
            string cacheKey,
            Func<Task<TCollection>> generateFullCollection)
        {
            CacheKeysBeingPopulatedInBackground.Add(cacheKey);
            await cache.GetOrAddAsync(cacheKey, async () =>
            {
                var collection = await generateFullCollection();
                CacheKeysBeingPopulatedInBackground.Remove(cacheKey);
                return collection;
            });
        }
    }
}