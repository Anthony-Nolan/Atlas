using FluentAssertions;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Data.Persistent.Repositories;
using Nova.SearchAlgorithm.Services;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services
{
    /// <summary>
    /// Note that the connection strings we assert on in this fixture are set in the `app.config` file for the unit test project.
    /// Any changes will need to be made in both locations. 
    /// </summary>
    [TestFixture]
    public class TransientSqlConnectionStringProviderTests
    {
        private IDataRefreshHistoryRepository historyRepository;

        [SetUp]
        public void SetUp()
        {
            historyRepository = Substitute.For<IDataRefreshHistoryRepository>();
        }

        [Test]
        public void GetConnectionString_WhenNoHistoryFound_DefaultsToDatabaseA()
        {
            var provider = new TransientSqlConnectionStringProvider(historyRepository);
            
            var connectionString = provider.GetConnectionString();

            connectionString.Should().Be("connectionStringA");
        }

        [Test]
        public void GetConnectionString_WhenLastDataMigrationWasAgainstDatabaseA_ReturnsDatabaseA()
        {
            historyRepository.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);
            var provider = new TransientSqlConnectionStringProvider(historyRepository);
            
            var connectionString = provider.GetConnectionString();

            connectionString.Should().Be("connectionStringA");
        }

        [Test]
        public void GetConnectionString_WhenLastDataMigrationWasAgainstDatabaseB_ReturnsDatabaseB()
        {
            historyRepository.GetActiveDatabase().Returns(TransientDatabase.DatabaseB);
            var provider = new TransientSqlConnectionStringProvider(historyRepository);
            
            var connectionString = provider.GetConnectionString();

            connectionString.Should().Be("connectionStringB");
        }
    }
}