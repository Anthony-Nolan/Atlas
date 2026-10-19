using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.AzureStorage.TableStorage;
using AwesomeAssertions;
using Azure;
using Azure.Core.Pipeline;
using Azure.Data.Tables;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.Common.Test.AzureStorage.TableStorage;

[TestFixture]
public class AzureCosmosLibraryTableExtensionsTests
{
    private const string PartitionKey = "A";
    private const int OneKilobyte = 1024;
    private const int TableStorageBatchLimitBytes = 4 * 1024 * 1024;

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

    /// <summary>
    /// Checks the request bodies that the SDK really writes, not only the estimate. If a later SDK version writes larger bodies,
    /// this test fails before the storage service starts to reject batches.
    /// </summary>
    [Test]
    public async Task BatchInsert_WithRealTableClient_SendsBodiesUnderTheLimitAndNoLargerThanEstimated()
    {
        // Like the HLA Metadata Dictionary's serology to alleles rows: about 100 KB of serialised allele names each, full of quotes.
        var entities = Enumerable.Range(0, 60)
            .Select(i => new TableEntity(PartitionKey, $"row-{i}") { ["SerialisedHlaInfo"] = SerialisedAlleleList(100 * OneKilobyte) })
            .ToList();
        var handler = new CapturingHandler();

        await RealTableClient(handler).BatchInsert(entities);

        handler.Requests.Should().HaveCountGreaterThan(1);
        handler.Requests.Sum(r => r.OperationCount).Should().Be(entities.Count);
        handler.Requests.Should().AllSatisfy(r => r.BodyBytes.Should().BeLessThanOrEqualTo(TableStorageBatchLimitBytes));

        var remainingEntities = entities.AsEnumerable();
        foreach (var request in handler.Requests)
        {
            var estimatedBytes = remainingEntities.Take(request.OperationCount).Sum(AzureCosmosLibraryTableExtensions.EstimatedRequestBytes);
            request.BodyBytes.Should().BeLessThanOrEqualTo(estimatedBytes);
            remainingEntities = remainingEntities.Skip(request.OperationCount);
        }
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

    private static string SerialisedAlleleList(int minimumLength)
    {
        var json = new StringBuilder("[");
        for (var i = 0; json.Length < minimumLength; i++)
        {
            json.Append($"\"02:{i / 100:D2}:{i % 100:D2}\",");
        }

        json[^1] = ']';
        return json.ToString();
    }

    private static TableClient RealTableClient(HttpMessageHandler handler)
    {
        return new TableClient(
            new Uri("https://account.table.core.windows.net"),
            "table",
            new TableSharedKeyCredential("account", Convert.ToBase64String(new byte[64])),
            new TableClientOptions { Transport = new HttpClientTransport(new HttpClient(handler)), Retry = { MaxRetries = 0 } });
    }

    /// <summary>
    /// Records each batch request, and answers it with one "204 No Content" part for each operation, so that the transaction succeeds.
    /// </summary>
    private class CapturingHandler : HttpMessageHandler
    {
        public List<(long BodyBytes, int OperationCount)> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
            var operationCount = Regex.Matches(Encoding.UTF8.GetString(body), "application/http").Count;
            Requests.Add((body.LongLength, operationCount));

            var response = new StringBuilder("--batchresponse\r\nContent-Type: multipart/mixed; boundary=changesetresponse\r\n\r\n");
            for (var i = 0; i < operationCount; i++)
            {
                response.Append("--changesetresponse\r\nContent-Type: application/http\r\nContent-Transfer-Encoding: binary\r\n\r\n");
                response.Append("HTTP/1.1 204 No Content\r\nDataServiceVersion: 3.0;\r\n\r\n\r\n");
            }

            response.Append("--changesetresponse--\r\n--batchresponse--\r\n");

            var message = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent(response.ToString()) };
            message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/mixed; boundary=batchresponse");
            return message;
        }
    }
}
