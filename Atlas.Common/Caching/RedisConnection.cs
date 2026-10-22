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
        /// <summary>How long a command waits for Redis to answer, before it fails.</summary>
        internal static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(1);

        /// <summary>
        /// How long the connection factory fails at once after a failed attempt - as long as the break of the circuit in
        /// <see cref="DistributedCacheProvider"/>.
        /// </summary>
        internal static readonly TimeSpan FailedConnectBackOff = DistributedCacheProvider.BreakerBreakDuration;

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

            // While there is a connection, fail a command that Redis does not answer in time, rather than wait for the 5 s
            // default. Only async commands are sent, so this is their only timeout. The connect timeout keeps its 5 s
            // default: a shorter one makes the attempts to connect fail again and again while the client or Redis is busy.
            options.AsyncTimeout = (int)CommandTimeout.TotalMilliseconds;

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
        /// expires. After a failed attempt, the factory fails at once for <see cref="FailedConnectBackOff"/> - see
        /// <see cref="WithBackOffAfterFailure"/>.
        /// </summary>
        public static Func<Task<IConnectionMultiplexer>> ConnectionFactory(DistributedCacheSettings settings, TimeProvider timeProvider)
        {
            return WithBackOffAfterFailure(() => ConnectAsync(settings), timeProvider);
        }

        /// <summary>
        /// Calls <paramref name="connect"/>; but after a call that fails, fails each call at once, with no new attempt,
        /// until <see cref="FailedConnectBackOff"/> has passed.
        /// </summary>
        /// <remarks>
        /// RedisCache keeps nothing from a failed attempt, so it calls the factory again on its next read or write. An
        /// attempt that cannot get a Microsoft Entra ID token can take many seconds to fail, as the token library tries
        /// again, with back-off, before it throws. Without the back-off, every read and write would wait for such an
        /// attempt, one after the other, on the connection lock of RedisCache.
        /// </remarks>
        internal static Func<Task<IConnectionMultiplexer>> WithBackOffAfterFailure(Func<Task<IConnectionMultiplexer>> connect, TimeProvider timeProvider)
        {
            // RedisCache calls the factory under its connection lock, so only one call at a time reads or writes this.
            DateTimeOffset? nextAttempt = null;

            return async () =>
            {
                if (timeProvider.GetUtcNow() < nextAttempt)
                {
                    throw new RedisConnectionException(
                        ConnectionFailureType.UnableToConnect,
                        $"The last attempt to connect to Redis failed. There is no new attempt before {nextAttempt:O}.");
                }

                try
                {
                    return await connect();
                }
                catch
                {
                    nextAttempt = timeProvider.GetUtcNow() + FailedConnectBackOff;
                    throw;
                }
            };
        }

        private static async Task<IConnectionMultiplexer> ConnectAsync(DistributedCacheSettings settings)
        {
            // New options for each attempt, so that a retried connection does not configure the same options twice.
            // Not a Clone() of one set of options: Clone() does not copy IncludeDetailInExceptions.
            var attemptOptions = BuildOptions(settings);

            if (UsesEntraId(settings))
            {
                attemptOptions = await attemptOptions.ConfigureForAzureWithUserAssignedManagedIdentityAsync(settings.ManagedIdentityClientId);
            }

            return await ConnectionMultiplexer.ConnectAsync(attemptOptions);
        }

        private static bool UsesEntraId(DistributedCacheSettings settings) => !string.IsNullOrWhiteSpace(settings.ManagedIdentityClientId);
    }
}
