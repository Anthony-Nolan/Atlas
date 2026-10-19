using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.AzureStorage.TableStorage;
using AwesomeAssertions;
using Azure;
using Azure.Data.Tables;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.Common.Test.AzureStorage.TableStorage
{
    [TestFixture]
    public class AzureCosmosLibraryTableExtensionsTests
    {
        private const string PartitionKey = "A";
        private const int OneKilobyte = 1024;

        private TableClient table;
        private List<List<TableEntity>> submittedBatches;

        [SetUp]
        public void SetUp()
        {
            submittedBatches = new List<List<TableEntity>>();

            table = Substitute.For<TableClient>();
            table.SubmitTransactionAsync(default, default)
                .ReturnsForAnyArgs(Task.FromResult(Substitute.For<Response<IReadOnlyList<Response>>>()));
            table.WhenForAnyArgs(t => t.SubmitTransactionAsync(default, default))
                .Do(call => submittedBatches.Add(call.Arg<IEnumerable<TableTransactionAction>>().Select(a => (TableEntity)a.Entity).ToList()));
        }

        [Test]
        public async Task BatchInsert_WithSmallEntities_SubmitsBatchesOfFifty()
        {
            var entities = Entities(PartitionKey, 120, dataLength: 10);

            await table.BatchInsert(entities);

            submittedBatches.Select(b => b.Count).Should().Equal(50, 50, 20);
        }

        [Test]
        public async Task BatchInsert_InsertsEveryEntityOnceAndInOrder()
        {
            var entities = Entities(PartitionKey, 40, dataLength: 200 * OneKilobyte);

            await table.BatchInsert(entities);

            submittedBatches.SelectMany(b => b).Should().Equal(entities);
        }

        [Test]
        public async Task BatchInsert_WhenFiftyEntitiesAreOverTheSizeLimit_KeepsEachBatchUnderTheLimit()
        {
            // 50 entities of 100 KB of quotes: about 5 MB as raw text, and about 30 MB once each quote is escaped.
            var entities = Entities(PartitionKey, 50, dataLength: 100 * OneKilobyte, dataCharacter: '"');

            await table.BatchInsert(entities);

            submittedBatches.Should().HaveCountGreaterThan(1);
            submittedBatches.Should().AllSatisfy(batch =>
                batch.Sum(AzureCosmosLibraryTableExtensions.EstimatedRequestBytes).Should()
                    .BeLessThanOrEqualTo(AzureCosmosLibraryTableExtensions.MaxBatchRequestBytes));
        }

        [Test]
        public async Task BatchInsert_WhenOneEntityIsOverTheSizeLimit_SubmitsItInABatchOfItsOwn()
        {
            var smallBefore = Entities(PartitionKey, 2, dataLength: 10, rowKeyPrefix: "before");
            var tooLarge = Entities(PartitionKey, 1, dataLength: AzureCosmosLibraryTableExtensions.MaxBatchRequestBytes, rowKeyPrefix: "large");
            var smallAfter = Entities(PartitionKey, 2, dataLength: 10, rowKeyPrefix: "after");

            await table.BatchInsert(smallBefore.Concat(tooLarge).Concat(smallAfter));

            submittedBatches.Should().HaveCount(3);
            submittedBatches[1].Should().Equal(tooLarge);
        }

        [Test]
        public async Task BatchInsert_SubmitsEachPartitionInSeparateBatches()
        {
            var entities = Entities("A", 3, dataLength: 10).Concat(Entities("B", 3, dataLength: 10)).ToList();

            await table.BatchInsert(entities);

            submittedBatches.Should().HaveCount(2);
            submittedBatches.Should().AllSatisfy(batch => batch.Select(e => e.PartitionKey).Distinct().Should().ContainSingle());
        }

        [Test]
        public void EstimatedRequestBytes_CountsEscapedQuotesAtTheirEscapedSize()
        {
            const int quoteCount = 1000;
            var entity = Entities(PartitionKey, 1, dataLength: quoteCount, dataCharacter: '"').Single();

            AzureCosmosLibraryTableExtensions.EstimatedRequestBytes(entity).Should().BeGreaterThanOrEqualTo(quoteCount * 6);
        }

        private static List<TableEntity> Entities(
            string partitionKey,
            int count,
            int dataLength,
            char dataCharacter = 'x',
            string rowKeyPrefix = "row")
        {
            return Enumerable.Range(0, count)
                .Select(i => new TableEntity(partitionKey, $"{rowKeyPrefix}-{i}") { ["Data"] = new string(dataCharacter, dataLength) })
                .ToList();
        }
    }
}
