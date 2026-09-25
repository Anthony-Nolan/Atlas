using System;
using System.Collections.Generic;
using System.Linq;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;

namespace Atlas.Common.Caching
{
    /// <inheritdoc cref="ICacheProvider"/>
    public interface IPersistentCacheProvider : ICacheProvider
    {
        /// <summary>
        /// Evicts every entry whose key satisfies <paramref name="keyPredicate"/>, in this process.
        /// </summary>
        /// <remarks>
        /// Exists so that data cached from the HLA Metadata Dictionary can be dropped when the dictionary's stored
        /// data is recreated - see <see cref="HlaVersionedCacheKey"/>. Only the persistent cache offers
        /// this: the transient cache lives for a single request, so nothing it holds can outlive a recreation.
        ///
        /// <para>
        /// <see cref="IAppCache"/> can evict a key it is given but cannot enumerate its keys, and LazyCache's
        /// <c>MemoryCacheProvider</c> keeps the underlying cache in an <c>internal</c> field, so the
        /// <see cref="MemoryCache"/> is held here alongside the <see cref="IAppCache"/> built over it.
        /// </para>
        /// </remarks>
        void RemoveWhere(Func<string, bool> keyPredicate);
    }

    /// <inheritdoc cref="ICacheProvider"/>
    public interface ITransientCacheProvider : ICacheProvider { }

    /// <summary>
    /// Used to explicitly state the scope of the cache your rely upon; Transient or Persistent as appropriate.
    /// </summary>
    public interface ICacheProvider
    {
        IAppCache Cache { get; }
    }

    public class PersistentCacheProvider : IPersistentCacheProvider
    {
        public IAppCache Cache { get; }

        /// <summary>
        /// The cache <see cref="Cache"/> is built over, or <c>null</c> when it was built elsewhere. Eviction by
        /// predicate needs to enumerate keys, which only this type can do.
        /// </summary>
        private readonly MemoryCache underlyingCache;

        public PersistentCacheProvider(IAppCache cache, MemoryCache underlyingCache = null)
        {
            Cache = cache;
            this.underlyingCache = underlyingCache;
        }

        /// <inheritdoc />
        public void RemoveWhere(Func<string, bool> keyPredicate)
        {
            if (underlyingCache == null)
            {
                throw new InvalidOperationException(
                    $"This {nameof(PersistentCacheProvider)} was built without a reference to its underlying " +
                    $"{nameof(MemoryCache)}, so its keys cannot be enumerated. Construct it with one to evict by predicate.");
            }

            // Materialised before removing: the keys are a live view of the cache, and removing while enumerating it
            // is not something MemoryCache promises to support.
            var keysToRemove = underlyingCache.Keys
                .OfType<string>()
                .Where(keyPredicate)
                .ToList();

            foreach (var key in keysToRemove)
            {
                Cache.Remove(key);
            }
        }
    }

    public class TransientCacheProvider : ITransientCacheProvider
    {
        public IAppCache Cache { get; }
        public TransientCacheProvider(IAppCache cache) { Cache = cache; }
    }
}
