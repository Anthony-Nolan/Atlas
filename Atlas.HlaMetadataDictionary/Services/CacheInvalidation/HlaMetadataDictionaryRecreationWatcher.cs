using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.HlaMetadataDictionary.InternalModels;
using Atlas.HlaMetadataDictionary.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Atlas.HlaMetadataDictionary.Services.CacheInvalidation
{
    /// <summary>
    /// Watches for recreations of the HLA Metadata Dictionary's stored data, and drops this process's cached copy of
    /// any version that has been recreated.
    /// </summary>
    /// <remarks>
    /// Polls rather than subscribing because every instance of an app has to find out, and a Service Bus subscription
    /// hands each message to a single receiver. See README_HlaMetadataDictionary.md for why a per-instance
    /// subscription was not workable.
    ///
    /// <para>
    /// The cost is latency: a recreation is noticed within one poll interval rather than at once. The process that did
    /// the recreating does not wait, because it evicts its own caches directly. Operators running
    /// <c>RefreshHlaMetadataDictionaryToSpecificVersion</c> should allow for the interval before re-testing.
    /// </para>
    /// </remarks>
    internal class HlaMetadataDictionaryRecreationWatcher : BackgroundService
    {
        /// <summary>
        /// How long before this process started a recreation can have been stamped and still be treated as possibly
        /// newer than what this process has cached.
        /// </summary>
        /// <remarks>
        /// A stamp's time comes from the clock of whichever machine did the recreating, so it is compared with this
        /// process's start time only loosely. Erring early costs one unnecessary refill; erring late serves stale data.
        /// </remarks>
        private static readonly TimeSpan ClockDifferenceAllowance = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory scopeFactory;

        /// <summary>No data can have been cached before this, so no recreation from before it can have been missed.</summary>
        private readonly DateTimeOffset processStartedAt;

        /// <summary>The stamp last seen for each version, as at the previous poll.</summary>
        private readonly Dictionary<string, string> lastSeenStamps = new();

        private bool hasPolled;

        /// <remarks>
        /// Takes only the scope factory. The invalidator and the logger are scoped, and a hosted service is a
        /// singleton, so resolving either of them here would fail DI validation at start-up.
        /// </remarks>
        public HlaMetadataDictionaryRecreationWatcher(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
            processStartedAt = DateTimeOffset.UtcNow;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var pollInterval = TimeSpan.FromSeconds(ResolvePollIntervalSeconds());

            while (!stoppingToken.IsCancellationRequested)
            {
                await PollOnce(stoppingToken);

                try
                {
                    await Task.Delay(pollInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        /// <remarks>
        /// Never throws. A storage problem must not stop the loop: the next tick retries, and until then the cache
        /// simply stays as it is - which is exactly where it would have been without this watcher at all.
        /// </remarks>
        internal async Task PollOnce(CancellationToken cancellationToken)
        {
            IAtlasLogger logger = null;

            try
            {
                using var scope = scopeFactory.CreateScope();

                logger = scope.ServiceProvider.GetRequiredService<IAtlasLogger>();

                var repository = scope.ServiceProvider.GetRequiredService<IHlaMetadataRecreationRepository>();
                var stamps = await repository.GetRecreationStamps(cancellationToken);

                var versionsToInvalidate = VersionsToInvalidate(stamps, repository);

                foreach (var version in versionsToInvalidate)
                {
                    scope.ServiceProvider.GetRequiredService<IHlaMetadataCacheInvalidator>().InvalidateCaches(version);
                }

                foreach (var (version, stamp) in stamps)
                {
                    lastSeenStamps[version] = stamp.Stamp;
                }

                hasPolled = true;
            }
            catch (Exception exception)
            {
                logger?.SendTrace(
                    "HLA-METADATA-DICTIONARY REFRESH: Could not read HLA Metadata Dictionary recreation stamps, so " +
                    $"cannot tell whether cached data is stale. Will try again on the next poll. Exception: {exception}",
                    LogLevel.Warn);
            }
        }

        /// <remarks>
        /// After the first poll, a version is invalidated when its stamp has changed since the previous poll.
        ///
        /// <para>
        /// The first poll has nothing to compare against, so it goes by time: it invalidates any version recreated
        /// since this process started. The cache cannot be assumed empty by then - start-up does not wait for the
        /// first read, and a failed read leaves the baseline unestablished, so the next successful read is treated as
        /// the first again. A recreation from before this process started was already in storage when the process
        /// first read it, so that one is left alone.
        /// </para>
        ///
        /// <para>
        /// A stamp this process wrote itself is skipped either way. The recreating process has already evicted
        /// directly, and evicting again would throw away data it has since started reloading - in a data refresh,
        /// the version it is about to process donors against.
        /// </para>
        /// </remarks>
        private IEnumerable<string> VersionsToInvalidate(
            IReadOnlyDictionary<string, HlaMetadataRecreationStamp> stamps,
            IHlaMetadataRecreationRepository repository)
        {
            foreach (var (version, stamp) in stamps)
            {
                if (repository.WasWrittenByThisProcess(version, stamp.Stamp))
                {
                    continue;
                }

                var isNewerThanCachedData = hasPolled
                    ? !lastSeenStamps.TryGetValue(version, out var lastSeen) || lastSeen != stamp.Stamp
                    : stamp.RecreatedAtUtc > processStartedAt - ClockDifferenceAllowance;

                if (isNewerThanCachedData)
                {
                    yield return version;
                }
            }
        }

        private int ResolvePollIntervalSeconds()
        {
            using var scope = scopeFactory.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<HlaMetadataDictionarySettings>();

            return settings.CacheInvalidationPollIntervalSeconds;
        }
    }
}
