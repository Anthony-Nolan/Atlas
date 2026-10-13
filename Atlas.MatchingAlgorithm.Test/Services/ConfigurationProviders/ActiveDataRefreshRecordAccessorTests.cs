using Atlas.Common.Caching;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.ConfigurationProviders
{
    [TestFixture]
    public class ActiveDataRefreshRecordAccessorTests
    {
        private static readonly ActiveDataRefreshRecord RecordForDatabaseA = new(1, TransientDatabase.DatabaseA, "3330");
        private static readonly ActiveDataRefreshRecord RecordForDatabaseB = new(2, TransientDatabase.DatabaseB, "3440");

        private IDataRefreshHistoryRepository historyRepository;
        private IActiveDataRefreshRecordAccessor activeDataRefreshRecordAccessor;

        [SetUp]
        public void SetUp()
        {
            historyRepository = Substitute.For<IDataRefreshHistoryRepository>();

            activeDataRefreshRecordAccessor = new ActiveDataRefreshRecordAccessor(
                historyRepository,
                new TransientCacheProvider(AppCacheBuilder.NewDefaultCache()));
        }

        [Test]
        public void GetActiveRecord_ReturnsRecordFromRepository()
        {
            historyRepository.GetActiveRecord().Returns(RecordForDatabaseB);

            var record = activeDataRefreshRecordAccessor.GetActiveRecord();

            record.Should().Be(RecordForDatabaseB);
        }

        [Test]
        public void GetActiveRecord_WhenNoRefreshHasCompleted_ReturnsNull()
        {
            historyRepository.GetActiveRecord().Returns((ActiveDataRefreshRecord)null);

            var record = activeDataRefreshRecordAccessor.GetActiveRecord();

            record.Should().BeNull();
        }

        [Test]
        public void GetActiveRecord_CachesRecord()
        {
            historyRepository.GetActiveRecord().Returns(RecordForDatabaseA, RecordForDatabaseB);

            var record1 = activeDataRefreshRecordAccessor.GetActiveRecord();
            var record2 = activeDataRefreshRecordAccessor.GetActiveRecord();

            record1.Should().Be(RecordForDatabaseA);
            record2.Should().Be(RecordForDatabaseA);
            historyRepository.Received(1).GetActiveRecord();
        }

        /// <summary>
        /// A refresh that completes between the first read of the active database and the first read of the active
        /// HLA version must not make them disagree.
        /// </summary>
        [Test]
        public void ActiveDatabaseAndVersion_WhenRefreshCompletesBetweenReads_ComeFromSameRecord()
        {
            historyRepository.GetActiveRecord().Returns(RecordForDatabaseA, RecordForDatabaseB);
            var databaseProvider = new ActiveDatabaseProvider(activeDataRefreshRecordAccessor);
            var versionAccessor = new ActiveHlaNomenclatureVersionAccessor(activeDataRefreshRecordAccessor);

            var database = databaseProvider.GetActiveDatabase();
            var version = versionAccessor.GetActiveHlaNomenclatureVersion();

            database.Should().Be(RecordForDatabaseA.Database);
            version.Should().Be(RecordForDatabaseA.HlaNomenclatureVersion);
        }
    }
}
