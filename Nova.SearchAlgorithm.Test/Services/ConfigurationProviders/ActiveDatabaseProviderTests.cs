using FluentAssertions;
using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Data.Persistent.Repositories;
using Nova.SearchAlgorithm.Data.Services;
using Nova.SearchAlgorithm.Services.ConfigurationProviders;
using Nova.SearchAlgorithm.Settings;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.ConfigurationProviders
{
    [TestFixture]
    public class ActiveDatabaseProviderTests
    {

        private IDataRefreshHistoryRepository historyRepository;

        private IAppCache cache;

        private IActiveDatabaseProvider activeDatabaseProvider;

        [SetUp]
        public void SetUp()
        {
            historyRepository = Substitute.For<IDataRefreshHistoryRepository>();
            cache = new CachingService(new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions())));

            activeDatabaseProvider = new ActiveDatabaseProvider(historyRepository, cache);
        }

        [Test]
        public void GetConnectionString_WhenNoHistoryFound_DefaultsToDatabaseA()
        {
            var database = activeDatabaseProvider.GetActiveDatabase();

            database.Should().Be(TransientDatabase.DatabaseA);
        }

        [Test]
        public void GetConnectionString_WhenLastDataMigrationWasAgainstDatabaseA_ReturnsDatabaseA()
        {
            historyRepository.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);

            var database = activeDatabaseProvider.GetActiveDatabase();

            database.Should().Be(TransientDatabase.DatabaseA);
        }

        [Test]
        public void GetConnectionString_WhenLastDataMigrationWasAgainstDatabaseB_ReturnsDatabaseB()
        {
            historyRepository.GetActiveDatabase().Returns(TransientDatabase.DatabaseB);

            var database = activeDatabaseProvider.GetActiveDatabase();

            database.Should().Be(TransientDatabase.DatabaseB);
        }

        [Test]
        public void GetConnectionString_CachesDatabaseValue()
        {
            historyRepository.GetActiveDatabase().Returns(TransientDatabase.DatabaseA, TransientDatabase.DatabaseB);

            var database1 = activeDatabaseProvider.GetActiveDatabase();
            var database2 = activeDatabaseProvider.GetActiveDatabase();

            database1.Should().Be(TransientDatabase.DatabaseA);
            database2.Should().Be(TransientDatabase.DatabaseA);
        }
    }
}