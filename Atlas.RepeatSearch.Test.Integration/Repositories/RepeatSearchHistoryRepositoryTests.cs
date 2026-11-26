using System;
using System.Threading.Tasks;
using Atlas.RepeatSearch.Data.Models;
using Atlas.RepeatSearch.Data.Repositories;
using Atlas.RepeatSearch.Data.Settings;
using AutoFixture;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.RepeatSearch.Test.Integration.Repositories;

[TestFixture]
public class RepeatSearchHistoryRepositoryTests
{
    private Fixture fixture;
    private IRepeatSearchHistoryRepository historyRepository;
    private ICanonicalResultSetRepository canonicalResultSetRepository;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        var connectionStrings = new ConnectionStrings { RepeatSearchSqlConnectionString = IntegrationTestSetUp.ConnectionString };
        historyRepository = new RepeatSearchHistoryRepository(connectionStrings);
        canonicalResultSetRepository = new CanonicalResultSetRepository(
            connectionStrings,
            new StoreOriginalSearchResultsBulkCopySettings { BatchSize = 10000, Timeout = 30 });
    }

    [Test]
    public async Task IsRepeatSearchRecorded_AfterDiffIsApplied_ReturnsTrue()
    {
        var searchRequestId = fixture.Create<string>();
        var claim = await canonicalResultSetRepository.StartStoringResultSet(searchRequestId);
        await canonicalResultSetRepository.CompleteResultSet(claim);
        var historyRecord = fixture.Build<RepeatSearchHistoryRecord>().With(r => r.OriginalSearchRequestId, searchRequestId).Create();
        await canonicalResultSetRepository.ApplyDiffAndRecordRepeatSearch(historyRecord, Array.Empty<string>(), Array.Empty<string>());

        var isRecorded = await historyRepository.IsRepeatSearchRecorded(historyRecord.RepeatSearchRequestId);

        isRecorded.Should().BeTrue();
    }

    [Test]
    public async Task IsRepeatSearchRecorded_ForRepeatSearchWithoutRecord_ReturnsFalse()
    {
        var isRecorded = await historyRepository.IsRepeatSearchRecorded(fixture.Create<string>());

        isRecorded.Should().BeFalse();
    }
}
