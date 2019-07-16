using FluentAssertions;
using Nova.SearchAlgorithm.Data.Persistent.Models;
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

        private IActiveDatabaseProvider activeDatabaseProvider;

        private IConnectionStringProvider activeConnectionStringProvider;
        
        [SetUp]
        public void SetUp()
        {
            activeDatabaseProvider = Substitute.For<IActiveDatabaseProvider>();
            
            activeConnectionStringProvider = new DormantTransientSqlConnectionStringProvider(connectionStrings, activeDatabaseProvider);
        }

        [Test]
        public void GetConnectionString_WhenDatabaseAActive_ReturnsDatabaseB()
        {
            activeDatabaseProvider.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);
            
            var connectionString = activeConnectionStringProvider.GetConnectionString();

            connectionString.Should().Be(connectionStrings.TransientB);
        }

        [Test]
        public void GetConnectionString_WhenDatabaseBActive_ReturnsDatabaseA()
        {
            activeDatabaseProvider.GetActiveDatabase().Returns(TransientDatabase.DatabaseB);
            
            var connectionString = activeConnectionStringProvider.GetConnectionString();

            connectionString.Should().Be(connectionStrings.TransientA);
        }
    }
}