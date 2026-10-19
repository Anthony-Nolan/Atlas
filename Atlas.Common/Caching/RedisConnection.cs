using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Atlas.Common.Caching
{
    /// <summary>
    /// How the distributed cache connects to Redis.
    /// </summary>
    internal static class RedisConnection
    {
        /// <summary>
        /// The connection options, from <see cref="DistributedCacheSettings.ConnectionString"/>.
        /// </summary>
        /// <exception cref="ArgumentException">The connection string is not valid: e.g. it has an unknown option.</exception>
        public static ConfigurationOptions BuildOptions(DistributedCacheSettings settings)
        {
            var options = ConfigurationOptions.Parse(settings.ConnectionString);

            // Keep trying to connect in the background, rather than let one failed attempt end the connection for good.
            options.AbortOnConnectFail = false;

            // While there is no connection, fail each command at once, rather than queue it until it times out: to a
            // cache, a miss costs less than a wait.
            options.BacklogPolicy = BacklogPolicy.FailFast;

            // One connection for commands and subscriptions, so that the refresh of a Microsoft Entra ID token
            // re-authenticates all of it.
            options.Protocol = RedisProtocol.Resp3;

            // Keep key names out of exception messages: each fault is logged with its exception, and the id of a key
            // can hold HLA typing data.
            options.IncludeDetailInExceptions = false;

            if (UsesEntraId(settings))
            {
                // A Microsoft Entra ID token is a credential: never send it without TLS.
                options.Ssl = true;
            }

            return options;
        }

        /// <summary>
        /// Connects with the options from <see cref="BuildOptions"/>. If <paramref name="settings"/> has a managed
        /// identity, the connection signs in as it, with Microsoft Entra ID - and keeps its token fresh, before the token
        /// expires.
        /// </summary>
        public static Func<Task<IConnectionMultiplexer>> ConnectionFactory(DistributedCacheSettings settings)
        {
            return async () =>
            {
                // New options for each attempt, so that a retried connection does not configure the same options twice.
                // Not a Clone() of one set of options: Clone() does not copy IncludeDetailInExceptions.
                var attemptOptions = BuildOptions(settings);

                if (UsesEntraId(settings))
                {
                    attemptOptions = await attemptOptions.ConfigureForAzureWithUserAssignedManagedIdentityAsync(settings.ManagedIdentityClientId);
                }

                return await ConnectionMultiplexer.ConnectAsync(attemptOptions);
            };
        }

        private static bool UsesEntraId(DistributedCacheSettings settings) => !string.IsNullOrWhiteSpace(settings.ManagedIdentityClientId);
    }
}
