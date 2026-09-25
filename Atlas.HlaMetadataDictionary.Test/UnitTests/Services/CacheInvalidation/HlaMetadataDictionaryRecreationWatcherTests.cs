using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
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
            var provider = services.BuildServiceProvider();

            watcher = new HlaMetadataDictionaryRecreationWatcher(
                provider.GetRequiredService<IServiceScopeFactory>(), logger);
        }

        private void StampsAre(params (string Version, string Stamp)[] stamps)
        {
            var asDictionary = new Dictionary<string, string>();
            foreach (var (version, stamp) in stamps)
            {
                asDictionary[version] = stamp;
            }

            repository.GetRecreationStamps(Arg.Any<CancellationToken>())
                .Returns<IReadOnlyDictionary<string, string>>(_ => asDictionary);
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
        /// still serving - a data refresh recreates at a new version hours before that version becomes active.
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
        /// A process that has just started has an empty cache, so nothing it holds can be stale. The first poll only
        /// records where the stamps stood; only a change from that point means this process is holding superseded data.
        /// </summary>
        [Test]
        public async Task PollOnce_OnTheFirstPoll_OnlyRecordsTheStartingPoint()
        {
            StampsAre((ActiveVersion, "stamp-1"), (OtherVersion, "stamp-1"));

            await watcher.PollOnce(CancellationToken.None);

            invalidator.DidNotReceiveWithAnyArgs().InvalidateCaches(default);
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
    }
}
