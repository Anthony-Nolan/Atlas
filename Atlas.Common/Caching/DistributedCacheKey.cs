using System;

namespace Atlas.Common.Caching
{
    /// <summary>
    /// The key of a value in the distributed cache - see <see cref="IDistributedCacheProvider"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="CacheName"/> names one kind of cached data, e.g. <c>"hf-active-sets-v1"</c>, and <see cref="Id"/> picks
    /// the value within it. The cache name keeps the keys of one consumer apart from the keys of another, and it is the
    /// dimension of the cache's metrics. So it must be one of a few fixed names, never a name made from data.
    /// </para>
    ///
    /// <para>
    /// Put a version in the cache name, and change the version when the stored format changes: during a deployment, the
    /// old and the new code read the same cache.
    /// </para>
    /// </remarks>
    public readonly record struct DistributedCacheKey
    {
        private const char Separator = ':';

        public DistributedCacheKey(string cacheName, string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cacheName);
            ArgumentException.ThrowIfNullOrEmpty(id);

            // Otherwise "a:b" + "c" and "a" + "b:c" would make the same key.
            if (cacheName.Contains(Separator))
            {
                throw new ArgumentException($"A cache name cannot contain '{Separator}', which separates it from the id in the key.", nameof(cacheName));
            }

            CacheName = cacheName;
            Id = id;
        }

        /// <summary>The kind of cached data that the value belongs to.</summary>
        public string CacheName { get; }

        /// <summary>The value within the kind of cached data.</summary>
        public string Id { get; }

        /// <summary>The key in Redis.</summary>
        public override string ToString() => $"{CacheName}{Separator}{Id}";
    }
}
