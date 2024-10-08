using Atlas.Common.Caching;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.ConfigurationProviders
{
    [TestFixture]
    public class ActiveDatabaseProviderTests
    {
        private IDataRefreshHistoryRepository historyRepository;
        private IActiveDatabaseProvider activeDatabaseProvider;

        [SetUp]
        public void SetUp()
        {
            historyRepository = Substitute.For<IDataRefreshHistoryRepository>();
            var cache = AppCacheBuilder.NewDefaultCache();
            var cacheProvider = new TransientCacheProvider(cache);
            
            activeDatabaseProvider = new ActiveDatabaseProvider(historyRepository, cacheProvider);
        }

        [Test]
        public void GetActiveDatabase_WhenNoHistoryFound_DefaultsToDatabaseA()
        {
            var database = activeDatabaseProvider.GetActiveDatabase();

            database.Should().Be(TransientDatabase.DatabaseA);
        }

        [Test]
        public void GetActiveDatabase_WhenLastDataMigrationWasAgainstDatabaseA_ReturnsDatabaseA()
        {
            historyRepository.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);

            var database = activeDatabaseProvider.GetActiveDatabase();

            database.Should().Be(TransientDatabase.DatabaseA);
        }

        [Test]
        public void GetActiveDatabase_WhenLastDataMigrationWasAgainstDatabaseB_ReturnsDatabaseB()
        {
            historyRepository.GetActiveDatabase().Returns(TransientDatabase.DatabaseB);

            var database = activeDatabaseProvider.GetActiveDatabase();

            database.Should().Be(TransientDatabase.DatabaseB);
        }

        [Test]
        public void GetActiveDatabase_CachesDatabaseValue()
        {
            historyRepository.GetActiveDatabase().Returns(TransientDatabase.DatabaseA, TransientDatabase.DatabaseB);

            var database1 = activeDatabaseProvider.GetActiveDatabase();
            var database2 = activeDatabaseProvider.GetActiveDatabase();

            database1.Should().Be(TransientDatabase.DatabaseA);
            database2.Should().Be(TransientDatabase.DatabaseA);
        }

        [Test]
        public void GetDormantDatabase_WhenNoHistoryFound_DefaultsToDatabaseB()
        {
            var database = activeDatabaseProvider.GetDormantDatabase();

            database.Should().Be(TransientDatabase.DatabaseB);
        }

        [Test]
        public void GetDormantDatabase_WhenLastDataMigrationWasAgainstDatabaseA_ReturnsDatabaseB()
        {
            historyRepository.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);

            var database = activeDatabaseProvider.GetDormantDatabase();

            database.Should().Be(TransientDatabase.DatabaseB);
        }

        [Test]
        public void GetDormantDatabase_WhenLastDataMigrationWasAgainstDatabaseB_ReturnsDatabaseA()
        {
            historyRepository.GetActiveDatabase().Returns(TransientDatabase.DatabaseB);

            var database = activeDatabaseProvider.GetDormantDatabase();

            database.Should().Be(TransientDatabase.DatabaseA);
        }

        [Test]
        public void GetDormantDatabase_CachesDatabaseValue()
        {
            historyRepository.GetActiveDatabase().Returns(TransientDatabase.DatabaseA, TransientDatabase.DatabaseB);

            var database1 = activeDatabaseProvider.GetDormantDatabase();
            var database2 = activeDatabaseProvider.GetDormantDatabase();

            database1.Should().Be(TransientDatabase.DatabaseB);
            database2.Should().Be(TransientDatabase.DatabaseB);
        }
    }
}