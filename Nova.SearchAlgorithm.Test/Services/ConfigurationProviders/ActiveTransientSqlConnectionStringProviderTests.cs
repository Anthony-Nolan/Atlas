using FluentAssertions;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Data.Services;
using Nova.SearchAlgorithm.Services.ConfigurationProviders;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Nova.SearchAlgorithm.Settings;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.ConfigurationProviders
{
    [TestFixture]
    public class ActiveTransientSqlConnectionStringProviderTests
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
            
            activeConnectionStringProvider = new ActiveTransientSqlConnectionStringProvider(connectionStrings, activeDatabaseProvider);
        }

        [Test]
        public void GetConnectionString_WhenDatabaseAActive_ReturnsDatabaseA()
        {
            activeDatabaseProvider.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);
            
            var connectionString = activeConnectionStringProvider.GetConnectionString();

            connectionString.Should().Be(connectionStrings.TransientA);
        }

        [Test]
        public void GetConnectionString_WhenDatabaseBActive_ReturnsDatabaseB()
        {
            activeDatabaseProvider.GetActiveDatabase().Returns(TransientDatabase.DatabaseB);
            
            var connectionString = activeConnectionStringProvider.GetConnectionString();

            connectionString.Should().Be(connectionStrings.TransientB);
        }
    }
}