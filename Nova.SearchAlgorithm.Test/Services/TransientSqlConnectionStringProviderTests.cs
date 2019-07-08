using FluentAssertions;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Data.Persistent.Repositories;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Services.ConfigurationProviders;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services
{
    [TestFixture]
    public class TransientSqlConnectionStringProviderTests
    {
        private IDataRefreshHistoryRepository historyRepository;

        private const string ConnectionStringA = "connection-A";
        private const string ConnectionStringB = "connection-B";
        
        [SetUp]
        public void SetUp()
        {
            historyRepository = Substitute.For<IDataRefreshHistoryRepository>();
        }

        [Test]
        public void GetConnectionString_WhenNoHistoryFound_DefaultsToDatabaseA()
        {
            var provider = new TransientSqlConnectionStringProvider(historyRepository, ConnectionStringA, ConnectionStringB);
            
            var connectionString = provider.GetConnectionString();

            connectionString.Should().Be(ConnectionStringA);
        }

        [Test]
        public void GetConnectionString_WhenLastDataMigrationWasAgainstDatabaseA_ReturnsDatabaseA()
        {
            historyRepository.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);
            var provider = new TransientSqlConnectionStringProvider(historyRepository, ConnectionStringA, ConnectionStringB);
            
            var connectionString = provider.GetConnectionString();

            connectionString.Should().Be(ConnectionStringA);
        }

        [Test]
        public void GetConnectionString_WhenLastDataMigrationWasAgainstDatabaseB_ReturnsDatabaseB()
        {
            historyRepository.GetActiveDatabase().Returns(TransientDatabase.DatabaseB);
            var provider = new TransientSqlConnectionStringProvider(historyRepository, ConnectionStringA, ConnectionStringB);
            
            var connectionString = provider.GetConnectionString();

            connectionString.Should().Be(ConnectionStringB);
        }
    }
}