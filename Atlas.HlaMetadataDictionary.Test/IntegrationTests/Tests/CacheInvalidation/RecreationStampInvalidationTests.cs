using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using Atlas.HlaMetadataDictionary.Services.CacheInvalidation;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.Tests.CacheInvalidation
{
    /// <summary>
    /// The whole invalidation loop against real Azure Table Storage: a recreation writes a stamp, a consuming
    /// process's watcher notices on its next poll, and that version's cached data is dropped.
    /// </summary>
    /// <remarks>
    /// Unit tests substitute the repository, so they prove the watcher reacts to a changed stamp but not that a stamp
    /// written by one process is seen by another. That crossing - a real write, a real read, a real table - is what
    /// this covers. Requires the Azure Storage Emulator (Azurite).
    /// </remarks>
    [TestFixture]
    internal class RecreationStampInvalidationTests
    {
        private const string RecreatedVersion = "3650";
        private const string OtherVersion = "3660";
        private const string CachedDataKey = "hlaMatchingLookup:" + RecreatedVersion;
        private const string OtherVersionKey = "hlaMatchingLookup:" + OtherVersion;

        private IHlaMetadataRecreationRepository repository;
        private IPersistentCacheProvider cacheProvider;
        private HlaMetadataDictionaryRecreationWatcher watcher;

        [SetUp]
        public void SetUp()
        {
            var settings = new HlaMetadataDictionarySettings { AzureStorageConnectionString = "UseDevelopmentStorage=true" };
            repository = new HlaMetadataRecreationRepository(new TableClientFactory(settings));

            cacheProvider = AppCacheBuilder.NewPersistentCacheProvider();

            var services = new ServiceCollection();
            services.AddScoped(_ => repository);
            services.AddScoped<IHlaMetadataCacheInvalidator>(_ =>
                new HlaMetadataCacheInvalidator(cacheProvider, Substitute.For<IAtlasLogger>()));
            services.AddScoped(_ => Substitute.For<IAtlasLogger>());
            services.AddScoped(_ => settings);

            watcher = new HlaMetadataDictionaryRecreationWatcher(
                services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>());
        }

        /// <summary>The scenario ATL's stale-cache defect describes, end to end.</summary>
        [Test]
        public async Task AStampWrittenByARecreation_CausesAConsumerToDropThatVersionsCache()
        {
            // A consumer holding cached data for both versions, with a baseline already established.
            await watcher.PollOnce(CancellationToken.None);
            cacheProvider.Cache.GetOrAdd(CachedDataKey, _ => "data-from-before-the-recreation");
            cacheProvider.Cache.GetOrAdd(OtherVersionKey, _ => "still-being-served");

            // The recreating process records that it rewrote this version's data.
            await repository.RecordRecreation(RecreatedVersion);

            await watcher.PollOnce(CancellationToken.None);

            cacheProvider.Cache.Get<string>(CachedDataKey).Should().BeNull(
                "the stamp for this version changed, so the consumer must drop what it cached before the recreation");
            cacheProvider.Cache.Get<string>(OtherVersionKey).Should().Be("still-being-served",
                "a recreation at one version must not cold-start the version a running app is still serving");
        }

        [Test]
        public async Task WithNoRecreation_TheConsumerKeepsItsCache()
        {
            await repository.RecordRecreation(RecreatedVersion);
            await watcher.PollOnce(CancellationToken.None);

            cacheProvider.Cache.GetOrAdd(CachedDataKey, _ => "current-data");
            await watcher.PollOnce(CancellationToken.None);

            cacheProvider.Cache.Get<string>(CachedDataKey).Should().Be("current-data");
        }

        /// <summary>A process starting after a recreation has nothing stale, so its first poll must not evict.</summary>
        [Test]
        public async Task AProcessStartingAfterARecreation_TreatsTheCurrentStampAsItsBaseline()
        {
            await repository.RecordRecreation(RecreatedVersion);

            cacheProvider.Cache.GetOrAdd(CachedDataKey, _ => "cached-after-the-recreation");
            await watcher.PollOnce(CancellationToken.None);

            cacheProvider.Cache.Get<string>(CachedDataKey).Should().Be("cached-after-the-recreation");
        }
    }
}
