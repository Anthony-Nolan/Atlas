namespace Atlas.Common.Caching
{
    /// <summary>
    /// The settings of the distributed cache - see <see cref="IDistributedCacheProvider"/>.
    /// </summary>
    public class DistributedCacheSettings
    {
        /// <summary>
        /// The StackExchange.Redis configuration string of the Redis that holds the cache: e.g.
        /// <c>&lt;name&gt;.&lt;region&gt;.redis.azure.net:10000</c> for Azure Managed Redis, or <c>localhost:6379</c> for
        /// a local Redis.
        /// </summary>
        /// <remarks>
        /// It holds no secret: Azure Managed Redis accepts only Microsoft Entra ID, with the identity in
        /// <see cref="ManagedIdentityClientId"/>. Empty means that there is no distributed cache: every read misses, and
        /// nothing is stored.
        /// </remarks>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The client ID of the user-assigned managed identity that signs in to the cache with Microsoft Entra ID.
        /// </summary>
        /// <remarks>
        /// Empty means that the connection does not use Microsoft Entra ID - e.g. to a local Redis.
        /// </remarks>
        public string ManagedIdentityClientId { get; set; }
    }
}
