using Atlas.MatchingAlgorithm.ConfigSettings;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Services;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.ConfigurationProviders
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
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseB);
            
            var connectionString = activeConnectionStringProvider.GetConnectionString();

            connectionString.Should().Be(connectionStrings.TransientB);
        }

        [Test]
        public void GetConnectionString_WhenDatabaseBActive_ReturnsDatabaseA()
        {
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);
            
            var connectionString = activeConnectionStringProvider.GetConnectionString();

            connectionString.Should().Be(connectionStrings.TransientA);
        }
    }
}