using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace Atlas.Common.Caching
{
    /// <summary>
    /// The distributed cache: a cache in Redis, shared by every instance of every Atlas app that is configured with the
    /// same Redis. A value that one process stores, the other processes can read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The cache is an optimisation, never a dependency. No method throws because the cache cannot be reached or fails:
    /// a read is then a miss, and the caller reads its source of truth; a write then returns <c>false</c>. The fault is
    /// logged and counted. The methods throw only for an argument that is not valid, and when the caller cancels.
    /// </para>
    ///
    /// <para>
    /// This is the second level (L2) of a cache. It has no local level of its own, and it does not coordinate fills:
    /// each caller keeps its own first level (L1) in front of it, as each needs a different L1 policy. An L1 that fills
    /// with LazyCache's <c>GetOrAddAsync</c> already runs one fill for each key in each process.
    /// </para>
    ///
    /// <para>
    /// When no Redis is configured, every read misses and every write returns <c>true</c>: a cache that keeps nothing
    /// cannot serve a stale value.
    /// </para>
    /// </remarks>
    public interface IDistributedCacheProvider
    {
        /// <summary>
        /// <c>false</c> when no Redis is configured: every read then misses, and nothing is stored. So a caller can skip the
        /// work that only a stored value needs, such as encoding it.
        /// </summary>
        bool IsEnabled { get; }

        /// <returns>
        /// The value stored under <paramref name="key"/>, or <c>null</c> on a miss - which includes a read that could not
        /// reach the cache.
        /// </returns>
        Task<byte[]> GetAsync(DistributedCacheKey key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores <paramref name="value"/> under <paramref name="key"/>, for <paramref name="timeToLive"/> from now at most.
        /// </summary>
        /// <returns>
        /// <c>false</c> if the value was not stored, because the cache could not be reached. A value stored under the key
        /// before can then still be read, until its own time to live ends.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeToLive"/> is not positive.</exception>
        Task<bool> SetAsync(DistributedCacheKey key, byte[] value, TimeSpan timeToLive, CancellationToken cancellationToken = default);

        /// <summary>Removes the value stored under <paramref name="key"/>, if there is one.</summary>
        /// <returns>
        /// <c>false</c> if the cache could not be reached, so a value stored under the key can still be read.
        /// </returns>
        Task<bool> RemoveAsync(DistributedCacheKey key, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// The <see cref="IDistributedCacheProvider"/> over a cache store: Redis, through <c>RedisCache</c>, in an app; any
    /// <see cref="IDistributedCache"/> in a test. It keeps the faults of the store from its callers.
    /// </summary>
    internal sealed class DistributedCacheProvider : IDistributedCacheProvider, IDisposable
    {
        // The circuit opens when half of the calls in the sampling duration fail. Then no call goes to the store for the
        // break duration, so in a burst of calls a store that is down or slow costs the callers nothing. At low traffic
        // the circuit does not open: there, RedisConnection.CommandTimeout limits the cost of each call. The break is
        // longer than a failover of Azure Managed Redis, so the probe call after it usually finds Redis available again.
        internal const double BreakerFailureRatio = 0.5;
        internal const int BreakerMinimumThroughput = 10;
        internal static readonly TimeSpan BreakerSamplingDuration = TimeSpan.FromSeconds(30);
        internal static readonly TimeSpan BreakerBreakDuration = TimeSpan.FromSeconds(30);

        private readonly IDistributedCache store;
        private readonly IDistributedCacheMetrics metrics;
        private readonly ILogger<DistributedCacheProvider> logger;
        private readonly TimeProvider timeProvider;
        private readonly ResiliencePipeline pipeline;

        public DistributedCacheProvider(
            IDistributedCache store,
            IDistributedCacheMetrics metrics,
            ILogger<DistributedCacheProvider> logger,
            TimeProvider timeProvider)
        {
            this.store = store;
            this.metrics = metrics;
            this.logger = logger;
            this.timeProvider = timeProvider;
            pipeline = BuildPipeline();
        }

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public async Task<byte[]> GetAsync(DistributedCacheKey key, CancellationToken cancellationToken = default)
        {
            DistributedCacheArguments.ThrowIfNotValid(key);
            var storeKey = key.ToString();
            var started = timeProvider.GetTimestamp();

            var (result, value) = await Execute(
                DistributedCacheOperation.Get,
                key,
                async token => await store.GetAsync(storeKey, token),
                cancellationToken);

            if (result == DistributedCacheResult.Succeeded)
            {
                result = value == null ? DistributedCacheResult.Miss : DistributedCacheResult.Hit;
            }

            metrics.RecordOperation(key.CacheName, DistributedCacheOperation.Get, result, timeProvider.GetElapsedTime(started));
            if (result == DistributedCacheResult.Hit)
            {
                metrics.RecordValueSize(key.CacheName, DistributedCacheOperation.Get, value.Length);
            }

            return value;
        }

        /// <inheritdoc />
        public async Task<bool> SetAsync(DistributedCacheKey key, byte[] value, TimeSpan timeToLive, CancellationToken cancellationToken = default)
        {
            DistributedCacheArguments.ThrowIfNotValid(key);
            DistributedCacheArguments.ThrowIfNotValid(value, timeToLive);
            var storeKey = key.ToString();

            // An absolute expiry only. A sliding expiry would make RedisCache send a second command on every read.
            var entryOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = timeToLive };
            var started = timeProvider.GetTimestamp();

            var (result, _) = await Execute(
                DistributedCacheOperation.Set,
                key,
                async token =>
                {
                    await store.SetAsync(storeKey, value, entryOptions, token);
                    return true;
                },
                cancellationToken);

            metrics.RecordOperation(key.CacheName, DistributedCacheOperation.Set, result, timeProvider.GetElapsedTime(started));
            if (result == DistributedCacheResult.Succeeded)
            {
                metrics.RecordValueSize(key.CacheName, DistributedCacheOperation.Set, value.Length);
            }

            return result == DistributedCacheResult.Succeeded;
        }

        /// <inheritdoc />
        public async Task<bool> RemoveAsync(DistributedCacheKey key, CancellationToken cancellationToken = default)
        {
            DistributedCacheArguments.ThrowIfNotValid(key);
            var storeKey = key.ToString();
            var started = timeProvider.GetTimestamp();

            var (result, _) = await Execute(
                DistributedCacheOperation.Remove,
                key,
                async token =>
                {
                    await store.RemoveAsync(storeKey, token);
                    return true;
                },
                cancellationToken);

            metrics.RecordOperation(key.CacheName, DistributedCacheOperation.Remove, result, timeProvider.GetElapsedTime(started));

            return result == DistributedCacheResult.Succeeded;
        }

        /// <summary>Disposes the store, and so closes the connection to Redis.</summary>
        public void Dispose() => (store as IDisposable)?.Dispose();

        /// <returns>
        /// <see cref="DistributedCacheResult.Succeeded"/> and the value of <paramref name="call"/>; or
        /// <see cref="DistributedCacheResult.Failed"/> or <see cref="DistributedCacheResult.Skipped"/>, and the default value.
        /// </returns>
        private async Task<(DistributedCacheResult Result, T Value)> Execute<T>(
            DistributedCacheOperation operation,
            DistributedCacheKey key,
            Func<CancellationToken, ValueTask<T>> call,
            CancellationToken cancellationToken)
        {
            try
            {
                return (DistributedCacheResult.Succeeded, await pipeline.ExecuteAsync(call, cancellationToken));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (BrokenCircuitException)
            {
                // The call did not go to the store. The circuit was logged once, when it opened.
                return (DistributedCacheResult.Skipped, default);
            }
            catch (Exception exception)
            {
                // The cache name only, not the key: the id of a key can hold HLA typing data.
                logger.LogWarning(
                    exception,
                    "Distributed cache operation {Operation} failed for cache {CacheName}. A read is treated as a miss, and a write as not stored.",
                    operation,
                    key.CacheName);
                return (DistributedCacheResult.Failed, default);
            }
        }

        private ResiliencePipeline BuildPipeline()
        {
            return new ResiliencePipelineBuilder { TimeProvider = timeProvider }
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = BreakerFailureRatio,
                    MinimumThroughput = BreakerMinimumThroughput,
                    SamplingDuration = BreakerSamplingDuration,
                    BreakDuration = BreakerBreakDuration,
                    OnOpened = arguments =>
                    {
                        logger.LogWarning(
                            "Distributed cache circuit opened for {BreakDuration}, after repeated faults. Until it closes, reads miss and writes are not stored.",
                            arguments.BreakDuration);
                        return default;
                    },
                    OnClosed = _ =>
                    {
                        logger.LogInformation("Distributed cache circuit closed. Calls go to the cache again.");
                        return default;
                    },
                })
                .Build();
        }
    }

    /// <summary>
    /// The <see cref="IDistributedCacheProvider"/> of an app with no Redis configured. It keeps nothing: every read misses,
    /// and every write returns <c>true</c>, as a cache that keeps nothing cannot serve a stale value.
    /// </summary>
    internal sealed class DisabledDistributedCacheProvider : IDistributedCacheProvider
    {
        public DisabledDistributedCacheProvider(ILogger<DisabledDistributedCacheProvider> logger)
        {
            logger.LogInformation("No distributed cache is configured. Every read misses, and nothing is stored.");
        }

        /// <inheritdoc />
        public bool IsEnabled => false;

        /// <inheritdoc />
        public Task<byte[]> GetAsync(DistributedCacheKey key, CancellationToken cancellationToken = default)
        {
            DistributedCacheArguments.ThrowIfNotValid(key);
            return Task.FromResult<byte[]>(null);
        }

        /// <inheritdoc />
        public Task<bool> SetAsync(DistributedCacheKey key, byte[] value, TimeSpan timeToLive, CancellationToken cancellationToken = default)
        {
            DistributedCacheArguments.ThrowIfNotValid(key);
            DistributedCacheArguments.ThrowIfNotValid(value, timeToLive);
            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public Task<bool> RemoveAsync(DistributedCacheKey key, CancellationToken cancellationToken = default)
        {
            DistributedCacheArguments.ThrowIfNotValid(key);
            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// The argument checks of every <see cref="IDistributedCacheProvider"/>, so that a caller's mistake shows in an app
    /// with no Redis too.
    /// </summary>
    internal static class DistributedCacheArguments
    {
        public static void ThrowIfNotValid(DistributedCacheKey key)
        {
            // The default key has no cache name: every value under it would share the one key ":".
            if (key.CacheName == null)
            {
                throw new ArgumentException($"The key has no cache name. Make keys with the {nameof(DistributedCacheKey)} constructor.", nameof(key));
            }
        }

        public static void ThrowIfNotValid(byte[] value, TimeSpan timeToLive)
        {
            ArgumentNullException.ThrowIfNull(value);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeToLive, TimeSpan.Zero);
        }
    }
}
