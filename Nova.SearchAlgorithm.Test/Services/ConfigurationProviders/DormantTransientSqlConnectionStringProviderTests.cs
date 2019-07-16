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
    public class DormantTransientSqlConnectionStringProviderTests
    {
        private readonly ConnectionStrings connectionStrings = new ConnectionStrings
        {
            TransientA = "connection-A",
            TransientB = "connection-B"
        };

        private IDataRefreshHistoryRepository historyRepository;

        private IAppCache cache;

        private IConnectionStringProvider dormantConnectionStringProvider;
        
        [SetUp]
        public void SetUp()
        {
            historyRepository = Substitute.For<IDataRefreshHistoryRepository>();
            cache = new CachingService(new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions())));
            
            dormantConnectionStringProvider = new DormantTransientSqlConnectionStringProvider(historyRepository, connectionStrings, cache);
        }

        [Test]
        public void GetConnectionString_WhenNoHistoryFound_DefaultsToDatabaseB()
        {
            var connectionString = dormantConnectionStringProvider.GetConnectionString();

            connectionString.Should().Be(connectionStrings.TransientB);
        }

        [Test]
        public void GetConnectionString_WhenLastDataMigrationWasAgainstDatabaseA_ReturnsDatabaseB()
        {
            historyRepository.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);
            
            var connectionString = dormantConnectionStringProvider.GetConnectionString();

            connectionString.Should().Be(connectionStrings.TransientB);
        }

        [Test]
        public void GetConnectionString_WhenLastDataMigrationWasAgainstDatabaseB_ReturnsDatabaseA()
        {
            historyRepository.GetActiveDatabase().Returns(TransientDatabase.DatabaseB);
            
            var connectionString = dormantConnectionStringProvider.GetConnectionString();

            connectionString.Should().Be(connectionStrings.TransientA);
        }
    }
}