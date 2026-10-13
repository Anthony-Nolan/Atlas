using System;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.ConfigurationProviders
{
    [TestFixture]
    public class ActiveHlaNomenclatureVersionAccessorTests
    {
        private IActiveDataRefreshRecordAccessor activeDataRefreshRecordAccessor;

        private IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor;

        [SetUp]
        public void SetUp()
        {
            activeDataRefreshRecordAccessor = Substitute.For<IActiveDataRefreshRecordAccessor>();

            hlaNomenclatureVersionAccessor = new ActiveHlaNomenclatureVersionAccessor(activeDataRefreshRecordAccessor);
        }

        [Test,
        TestCase(null),
        TestCase(""),
        TestCase("   "),
        TestCase("\t\r\n ")]
        public void GetActiveHlaNomenclatureVersion_WhenActiveVersionIsNull_ThrowsException(string badVersionValues)
        {
            GivenActiveVersion(badVersionValues);

            hlaNomenclatureVersionAccessor.Invoking(provider => provider.GetActiveHlaNomenclatureVersion()).Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void GetActiveHlaNomenclatureVersion_WhenNoActiveRecord_ThrowsException()
        {
            activeDataRefreshRecordAccessor.GetActiveRecord().Returns((ActiveDataRefreshRecord)null);

            hlaNomenclatureVersionAccessor.Invoking(provider => provider.GetActiveHlaNomenclatureVersion()).Should().Throw<ArgumentNullException>();
        }

        [Test,
         TestCase(null),
         TestCase(""),
         TestCase("   "),
         TestCase("\t\r\n ")]
        public void DoesActiveHlaNomenclatureVersionExist_WhenActiveVersionIsNull_ReturnsFalse(string badVersionValues)
        {
            GivenActiveVersion(badVersionValues);

            var doesActiveVersionExist = hlaNomenclatureVersionAccessor.DoesActiveHlaNomenclatureVersionExist();

            doesActiveVersionExist.Should().BeFalse();
        }

        [Test]
        public void DoesActiveHlaNomenclatureVersionExist_WhenNoActiveRecord_ReturnsFalse()
        {
            activeDataRefreshRecordAccessor.GetActiveRecord().Returns((ActiveDataRefreshRecord)null);

            var doesActiveVersionExist = hlaNomenclatureVersionAccessor.DoesActiveHlaNomenclatureVersionExist();

            doesActiveVersionExist.Should().BeFalse();
        }

        [Test]
        public void GetActiveHlaNomenclatureVersion_WhenActiveVersionIsNotNull_ReturnsValue()
        {
            const string activeVersion = "version";
            GivenActiveVersion(activeVersion);

            var activeVersionReturned = hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion();

            activeVersionReturned.Should().Be(activeVersion);
        }

        [Test]
        public void DoesActiveHlaNomenclatureVersionExist_WhenActiveVersionIsNotNull_ReturnsTrue()
        {
            const string activeVersion = "version";
            GivenActiveVersion(activeVersion);

            var doesActiveVersionExist = hlaNomenclatureVersionAccessor.DoesActiveHlaNomenclatureVersionExist();

            doesActiveVersionExist.Should().BeTrue();
        }

        private void GivenActiveVersion(string version)
        {
            activeDataRefreshRecordAccessor.GetActiveRecord().Returns(new ActiveDataRefreshRecord(1, TransientDatabase.DatabaseA, version));
        }
    }
}
