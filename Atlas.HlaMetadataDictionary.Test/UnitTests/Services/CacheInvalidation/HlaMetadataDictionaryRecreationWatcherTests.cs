using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.HlaMetadataDictionary.InternalModels;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Services.CacheInvalidation;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.CacheInvalidation
{
    [TestFixture]
    internal class HlaMetadataDictionaryRecreationWatcherTests
    {
        private const string ActiveVersion = "3650";
        private const string OtherVersion = "3660";

        /// <summary>Well outside the allowance for clock differences, so unambiguously before any watcher was created.</summary>
        private static readonly DateTimeOffset LongBeforeStart = DateTimeOffset.UtcNow.AddDays(-1);

        private IHlaMetadataRecreationRepository repository;
        private IHlaMetadataCacheInvalidator invalidator;
        private IAtlasLogger logger;
        private HlaMetadataDictionaryRecreationWatcher watcher;

        [SetUp]
        public void SetUp()
        {
            repository = Substitute.For<IHlaMetadataRecreationRepository>();
            invalidator = Substitute.For<IHlaMetadataCacheInvalidator>();
            logger = Substitute.For<IAtlasLogger>();

            var services = new ServiceCollection();
            services.AddScoped(_ => repository);
            services.AddScoped(_ => invalidator);
            services.AddScoped(_ => new HlaMetadataDictionarySettings());
            services.AddScoped(_ => logger);
            var provider = services.BuildServiceProvider();

            watcher = new HlaMetadataDictionaryRecreationWatcher(provider.GetRequiredService<IServiceScopeFactory>());
        }

        /// <summary>Stamps recreated long before the watcher started, unless given a time.</summary>
        private void StampsAre(params (string Version, string Stamp)[] stamps) =>
            StampsAre(stamps.Select(s => (s.Version, s.Stamp, LongBeforeStart)).ToArray());

        private void StampsAre(params (string Version, string Stamp, DateTimeOffset RecreatedAtUtc)[] stamps)
        {
            var asDictionary = new Dictionary<string, HlaMetadataRecreationStamp>();
            foreach (var (version, stamp, recreatedAtUtc) in stamps)
            {
                asDictionary[version] = new HlaMetadataRecreationStamp(stamp, recreatedAtUtc);
            }

            repository.GetRecreationStamps(Arg.Any<CancellationToken>())
                .Returns<IReadOnlyDictionary<string, HlaMetadataRecreationStamp>>(_ => asDictionary);
        }

        [Test]
        public async Task PollOnce_WhenAStampHasChanged_InvalidatesThatVersion()
        {
            StampsAre((ActiveVersion, "stamp-1"));
            await watcher.PollOnce(CancellationToken.None);
            invalidator.ClearReceivedCalls();

            StampsAre((ActiveVersion, "stamp-2"));
            await watcher.PollOnce(CancellationToken.None);

            invalidator.Received(1).InvalidateCaches(ActiveVersion);
        }

        [Test]
        public async Task PollOnce_WhenNoStampHasChanged_InvalidatesNothing()
        {
            StampsAre((ActiveVersion, "stamp-1"));
            await watcher.PollOnce(CancellationToken.None);
            invalidator.ClearReceivedCalls();

            await watcher.PollOnce(CancellationToken.None);

            invalidator.DidNotReceiveWithAnyArgs().InvalidateCaches(default);
        }

        /// <summary>
        /// A recreation at a new version must not cost a running matching app the warm cache of the version it is
        /// still serving. When WMDA has published a new version, a data refresh recreates at it hours before it
        /// becomes active.
        /// </summary>
        [Test]
        public async Task PollOnce_InvalidatesOnlyTheVersionWhoseStampChanged()
        {
            StampsAre((ActiveVersion, "stamp-1"), (OtherVersion, "stamp-1"));
            await watcher.PollOnce(CancellationToken.None);
            invalidator.ClearReceivedCalls();

            StampsAre((ActiveVersion, "stamp-1"), (OtherVersion, "stamp-2"));
            await watcher.PollOnce(CancellationToken.None);

            invalidator.Received(1).InvalidateCaches(OtherVersion);
            invalidator.DidNotReceive().InvalidateCaches(ActiveVersion);
        }

        /// <summary>
        /// A recreation from before this process started was already in storage when the process first read it, so
        /// nothing cached can predate it. The first poll records where the stamps stood and invalidates nothing.
        /// </summary>
        [Test]
        public async Task PollOnce_OnTheFirstPoll_IgnoresRecreationsFromBeforeThisProcessStarted()
        {
            StampsAre((ActiveVersion, "stamp-1"), (OtherVersion, "stamp-1"));

            await watcher.PollOnce(CancellationToken.None);

            invalidator.DidNotReceiveWithAnyArgs().InvalidateCaches(default);
        }

        /// <summary>
        /// Start-up does not wait for the first poll, so the cache may already hold data by the time the baseline is
        /// read. A recreation since this process started may have superseded it.
        /// </summary>
        [Test]
        public async Task PollOnce_OnTheFirstPoll_InvalidatesAVersionRecreatedSinceThisProcessStarted()
        {
            StampsAre((ActiveVersion, "stamp-1", DateTimeOffset.UtcNow), (OtherVersion, "stamp-1", LongBeforeStart));

            await watcher.PollOnce(CancellationToken.None);

            invalidator.Received(1).InvalidateCaches(ActiveVersion);
            invalidator.DidNotReceive().InvalidateCaches(OtherVersion);
        }

        /// <summary>
        /// A stamp's time comes from the recreating machine's clock, which may run behind this one - so a recreation
        /// apparently just before start could really have finished after it.
        /// </summary>
        [Test]
        public async Task PollOnce_OnTheFirstPoll_TreatsARecreationJustBeforeStartAsPossiblyNewer()
        {
            StampsAre((ActiveVersion, "stamp-1", DateTimeOffset.UtcNow.AddMinutes(-2)));

            await watcher.PollOnce(CancellationToken.None);

            invalidator.Received(1).InvalidateCaches(ActiveVersion);
        }

        [Test]
        public async Task PollOnce_WhenStampsCannotBeRead_LogsAWarningAndDoesNotThrow()
        {
            repository.GetRecreationStamps(Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("storage is unavailable"));

            await watcher.Invoking(w => w.PollOnce(CancellationToken.None)).Should().NotThrowAsync();

            logger.Received().SendTrace(Arg.Any<string>(), LogLevel.Warn);
        }

        /// <summary>A read failure must not leave the watcher believing it has a baseline it never established.</summary>
        [Test]
        public async Task PollOnce_AfterAFailedRead_StillTreatsTheNextReadAsTheFirst()
        {
            repository.GetRecreationStamps(Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("storage is unavailable"));
            await watcher.PollOnce(CancellationToken.None);

            StampsAre((ActiveVersion, "stamp-1"));
            await watcher.PollOnce(CancellationToken.None);

            // That read is the baseline, not a change - the failed one established nothing.
            invalidator.DidNotReceiveWithAnyArgs().InvalidateCaches(default);

            StampsAre((ActiveVersion, "stamp-2"));
            await watcher.PollOnce(CancellationToken.None);

            invalidator.Received(1).InvalidateCaches(ActiveVersion);
        }

        /// <summary>
        /// The cache fills while storage is briefly unreachable, and a recreation lands before the next read succeeds.
        /// That read has no earlier baseline, but must still catch the recreation.
        /// </summary>
        [Test]
        public async Task PollOnce_WhenTheFirstReadFailsThenARecreationLands_InvalidatesOnTheNextSuccessfulRead()
        {
            repository.GetRecreationStamps(Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("storage is unavailable"));
            await watcher.PollOnce(CancellationToken.None);

            StampsAre((ActiveVersion, "stamp-1", DateTimeOffset.UtcNow));
            await watcher.PollOnce(CancellationToken.None);

            invalidator.Received(1).InvalidateCaches(ActiveVersion);
        }

        /// <summary>
        /// The recreating process has already evicted directly. Evicting again would throw away data it has since
        /// started reloading - but a later recreation by another process must still get through.
        /// </summary>
        [Test]
        public async Task PollOnce_WhenTheChangedStampWasWrittenByThisProcess_InvalidatesNothing()
        {
            StampsAre((ActiveVersion, "stamp-1"));
            await watcher.PollOnce(CancellationToken.None);

            repository.WasWrittenByThisProcess(ActiveVersion, "own-stamp").Returns(true);
            StampsAre((ActiveVersion, "own-stamp"));
            await watcher.PollOnce(CancellationToken.None);

            invalidator.DidNotReceiveWithAnyArgs().InvalidateCaches(default);

            StampsAre((ActiveVersion, "another-process's-stamp"));
            await watcher.PollOnce(CancellationToken.None);

            invalidator.Received(1).InvalidateCaches(ActiveVersion);
        }
    }
}
