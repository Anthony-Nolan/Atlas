using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
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
        private readonly IServiceScopeFactory scopeFactory;
        private readonly IAtlasLogger logger;

        /// <summary>The stamp last seen for each version, as at the previous poll.</summary>
        private readonly Dictionary<string, string> lastSeenStamps = new();

        private bool hasPolled;

        public HlaMetadataDictionaryRecreationWatcher(IServiceScopeFactory scopeFactory, IAtlasLogger logger)
        {
            this.scopeFactory = scopeFactory;
            this.logger = logger;
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
            try
            {
                using var scope = scopeFactory.CreateScope();

                var stamps = await scope.ServiceProvider
                    .GetRequiredService<IHlaMetadataRecreationRepository>()
                    .GetRecreationStamps(cancellationToken);

                var versionsToInvalidate = VersionsWhoseStampChanged(stamps);

                foreach (var version in versionsToInvalidate)
                {
                    scope.ServiceProvider.GetRequiredService<IHlaMetadataCacheInvalidator>().InvalidateCaches(version);
                }

                foreach (var (version, stamp) in stamps)
                {
                    lastSeenStamps[version] = stamp;
                }

                hasPolled = true;
            }
            catch (Exception exception)
            {
                logger.SendTrace(
                    "HLA-METADATA-DICTIONARY REFRESH: Could not read HLA Metadata Dictionary recreation stamps, so " +
                    $"cannot tell whether cached data is stale. Will try again on the next poll. Exception: {exception}",
                    LogLevel.Warn);
            }
        }

        /// <remarks>
        /// The first poll establishes the baseline and invalidates nothing: a process that has just started has an
        /// empty cache, so there is nothing that could be stale. Only a stamp that changes AFTER that baseline means
        /// this process is holding data the recreation has superseded.
        ///
        /// <para>
        /// A failed read leaves the baseline unestablished, so the next successful read is treated as the first
        /// again - the watcher must never believe it has a baseline it never actually recorded.
        /// </para>
        /// </remarks>
        private IEnumerable<string> VersionsWhoseStampChanged(IReadOnlyDictionary<string, string> stamps)
        {
            foreach (var (version, stamp) in stamps)
            {
                if (hasPolled && (!lastSeenStamps.TryGetValue(version, out var lastSeen) || lastSeen != stamp))
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
