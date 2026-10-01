using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.ConfigurationProviders
{
    [TestFixture]
    public class ActiveDatabaseProviderTests
    {
        private IActiveDataRefreshRecordAccessor activeDataRefreshRecordAccessor;
        private IActiveDatabaseProvider activeDatabaseProvider;

        [SetUp]
        public void SetUp()
        {
            activeDataRefreshRecordAccessor = Substitute.For<IActiveDataRefreshRecordAccessor>();

            activeDatabaseProvider = new ActiveDatabaseProvider(activeDataRefreshRecordAccessor);
        }

        [Test]
        public void GetActiveDatabase_WhenNoHistoryFound_DefaultsToDatabaseA()
        {
            activeDataRefreshRecordAccessor.GetActiveRecord().Returns((ActiveDataRefreshRecord)null);

            var database = activeDatabaseProvider.GetActiveDatabase();

            database.Should().Be(TransientDatabase.DatabaseA);
        }

        [Test]
        public void GetActiveDatabase_WhenLastDataMigrationWasAgainstDatabaseA_ReturnsDatabaseA()
        {
            GivenActiveDatabase(TransientDatabase.DatabaseA);

            var database = activeDatabaseProvider.GetActiveDatabase();

            database.Should().Be(TransientDatabase.DatabaseA);
        }

        [Test]
        public void GetActiveDatabase_WhenLastDataMigrationWasAgainstDatabaseB_ReturnsDatabaseB()
        {
            GivenActiveDatabase(TransientDatabase.DatabaseB);

            var database = activeDatabaseProvider.GetActiveDatabase();

            database.Should().Be(TransientDatabase.DatabaseB);
        }

        [Test]
        public void GetDormantDatabase_WhenNoHistoryFound_DefaultsToDatabaseB()
        {
            activeDataRefreshRecordAccessor.GetActiveRecord().Returns((ActiveDataRefreshRecord)null);

            var database = activeDatabaseProvider.GetDormantDatabase();

            database.Should().Be(TransientDatabase.DatabaseB);
        }

        [Test]
        public void GetDormantDatabase_WhenLastDataMigrationWasAgainstDatabaseA_ReturnsDatabaseB()
        {
            GivenActiveDatabase(TransientDatabase.DatabaseA);

            var database = activeDatabaseProvider.GetDormantDatabase();

            database.Should().Be(TransientDatabase.DatabaseB);
        }

        [Test]
        public void GetDormantDatabase_WhenLastDataMigrationWasAgainstDatabaseB_ReturnsDatabaseA()
        {
            GivenActiveDatabase(TransientDatabase.DatabaseB);

            var database = activeDatabaseProvider.GetDormantDatabase();

            database.Should().Be(TransientDatabase.DatabaseA);
        }

        private void GivenActiveDatabase(TransientDatabase database)
        {
            activeDataRefreshRecordAccessor.GetActiveRecord().Returns(new ActiveDataRefreshRecord(1, database, "version"));
        }
    }
}
