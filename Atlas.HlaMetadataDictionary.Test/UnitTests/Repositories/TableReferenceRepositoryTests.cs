using System;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Repositories
{
    [TestFixture]
    internal class TableReferenceRepositoryTests
    {
        private TableReferenceRepository repository;

        [SetUp]
        public void SetUp()
        {
            repository = new TableReferenceRepository(Substitute.For<ITableClientFactory>());
        }

        [Test]
        public void GetNewTableReference_EndsPrefixWithSnapshotToTheMillisecond()
        {
            var snapshotUtc = new DateTime(2026, 10, 5, 9, 4, 7, 123, DateTimeKind.Utc);

            var reference = repository.GetNewTableReference("HlaMatchingLookupData3560", snapshotUtc);

            reference.Should().Be("HlaMatchingLookupData356020261005090407123");
        }

        [Test]
        public void GetNewTableReference_UsesTwentyFourHourClock()
        {
            var snapshotUtc = new DateTime(2026, 10, 5, 15, 4, 0, 0, DateTimeKind.Utc);

            var reference = repository.GetNewTableReference("prefix", snapshotUtc);

            reference.Should().Be("prefix20261005150400000");
        }
    }
}
