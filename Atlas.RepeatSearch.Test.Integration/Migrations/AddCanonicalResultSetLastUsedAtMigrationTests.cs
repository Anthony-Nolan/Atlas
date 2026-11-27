using System;
using System.Threading.Tasks;
using Atlas.RepeatSearch.Data.Models;
using AutoFixture;
using AwesomeAssertions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NUnit.Framework;
using RepeatSearchContextFactory = Atlas.RepeatSearch.Data.Context.ContextFactory;
using SearchTrackingContextFactory = Atlas.SearchTracking.Data.Context.ContextFactory;

namespace Atlas.RepeatSearch.Test.Integration.Migrations;

/// <summary>
/// Runs the backfill of the migration AddCanonicalResultSetLastUsedAt on data: the database goes back to the migration before it, gets
/// sets, repeat history and search tracking rows, and then migrates up again. In the shared Atlas database, the SearchTracking schema is
/// next to the RepeatSearch schema, so the test database gets the real SearchTracking tables too.
/// </summary>
[TestFixture]
public class AddCanonicalResultSetLastUsedAtMigrationTests
{
    private const string MigrationBeforeLastUsedAt = "20261118183340_SearchResultsIndexSetIdFirst";

    /// <summary>
    /// The first request time in the search tracking table of the test database: no test inserts an earlier one.
    /// </summary>
    private static readonly DateTime SearchTrackingStart = new(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private Fixture fixture;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await using var searchTrackingContext = new SearchTrackingContextFactory().Create(IntegrationTestSetUp.ConnectionString);
        await searchTrackingContext.Database.MigrateAsync();
    }

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
    }

    [TearDown]
    public async Task TearDown()
    {
        // Also after a failed test, the other tests need the latest schema.
        await MigrateTo(null);
    }

    [Test]
    public async Task Up_FillsLastUsedAt_WithLaterOfOriginalSearchAndLastRepeat_ElseSearchTrackingStart()
    {
        // The estimate for a set with no tracking row reads the tracked sets with lower ids, so the sets of earlier runs of this test go first.
        await DeleteSetsOfEarlierRuns();
        await MigrateTo(MigrationBeforeLastUsedAt);

        // The first tracked search: its request time is the start of search tracking.
        await InsertSearchRequest(NewSearchId(), SearchTrackingStart);

        // A tracking id that is not a GUID: the join compares text, so it never converts this column.
        await InsertSearchRequest(NewSearchId().Replace('-', 'x'), SearchTrackingStart.AddDays(90));

        // Sets stored before the first tracked set, as before search tracking started.
        // Not a GUID, and no tracking row or history: the join must not fail on it.
        var neither = await InsertSet($"NotAGuid-{fixture.Create<string>()}");

        var historyOnly = await InsertSet(NewSearchId());
        await InsertRepeatHistory(historyOnly, SearchTrackingStart.AddDays(10));
        await InsertRepeatHistory(historyOnly, SearchTrackingStart.AddDays(20));

        // Tracked sets.
        var requestOnly = await InsertSet(NewSearchId());
        await InsertSearchRequest(requestOnly, SearchTrackingStart.AddDays(30));

        var repeatAfterRequest = await InsertSet(NewSearchId());
        await InsertSearchRequest(repeatAfterRequest, SearchTrackingStart.AddDays(40));
        await InsertRepeatHistory(repeatAfterRequest, SearchTrackingStart.AddDays(50));

        var requestAfterRepeat = await InsertSet(NewSearchId());
        await InsertRepeatHistory(requestAfterRepeat, SearchTrackingStart.AddDays(60));
        await InsertSearchRequest(requestAfterRepeat, SearchTrackingStart.AddDays(70));

        // Search tracking stores lowercase ids.
        var upperCaseSearchId = NewSearchId();
        var upperCase = await InsertSet(upperCaseSearchId.ToUpperInvariant());
        await InsertSearchRequest(upperCaseSearchId, SearchTrackingStart.AddDays(80));

        // Stored after search tracking started, but with no tracking row (for example a lost tracking event) and no history.
        var untrackedAfterTrackedSets = await InsertSet(NewSearchId());

        await MigrateTo(null);

        (await GetLastUsedAt(neither)).Should().Be(AsUtcOffset(SearchTrackingStart));
        (await GetLastUsedAt(historyOnly)).Should().Be(AsUtcOffset(SearchTrackingStart.AddDays(20)));
        (await GetLastUsedAt(requestOnly)).Should().Be(AsUtcOffset(SearchTrackingStart.AddDays(30)));
        (await GetLastUsedAt(repeatAfterRequest)).Should().Be(AsUtcOffset(SearchTrackingStart.AddDays(50)));
        (await GetLastUsedAt(requestAfterRepeat)).Should().Be(AsUtcOffset(SearchTrackingStart.AddDays(70)));
        (await GetLastUsedAt(upperCase)).Should().Be(AsUtcOffset(SearchTrackingStart.AddDays(80)));
        // The latest request time of the tracked sets with lower ids.
        (await GetLastUsedAt(untrackedAfterTrackedSets)).Should().Be(AsUtcOffset(SearchTrackingStart.AddDays(80)));
    }

    private string NewSearchId() => fixture.Create<Guid>().ToString();

    private static DateTimeOffset AsUtcOffset(DateTime utcTime) => new(utcTime, TimeSpan.Zero);

    /// <summary>
    /// After the migration, the sets of this test are dated in the year after <see cref="SearchTrackingStart"/>; no other test uses dates
    /// that early.
    /// </summary>
    private static async Task DeleteSetsOfEarlierRuns()
    {
        await using var connection = new SqlConnection(IntegrationTestSetUp.ConnectionString);
        await connection.ExecuteAsync(
            "DELETE FROM RepeatSearch.CanonicalResultSets WHERE LastUsedAt < @before",
            new { before = AsUtcOffset(SearchTrackingStart.AddYears(1)) });
    }

    private static async Task MigrateTo(string targetMigration)
    {
        await using var context = new RepeatSearchContextFactory().Create(IntegrationTestSetUp.ConnectionString);
        await context.GetService<IMigrator>().MigrateAsync(targetMigration);
    }

    /// <returns>The search id of the set.</returns>
    private static async Task<string> InsertSet(string searchRequestId)
    {
        await using var connection = new SqlConnection(IntegrationTestSetUp.ConnectionString);
        await connection.ExecuteAsync(
            "INSERT INTO RepeatSearch.CanonicalResultSets (OriginalSearchRequestId, Status) VALUES (@searchRequestId, @Complete)",
            new { searchRequestId, Complete = (int)CanonicalResultSetStatus.Complete });
        return searchRequestId;
    }

    private async Task InsertRepeatHistory(string originalSearchRequestId, DateTime dateCreatedUtc)
    {
        await using var connection = new SqlConnection(IntegrationTestSetUp.ConnectionString);
        await connection.ExecuteAsync(
            @"INSERT INTO RepeatSearch.RepeatSearchHistoryRecords
                (OriginalSearchRequestId, RepeatSearchRequestId, SearchCutoffDate, DateCreated, UpdatedResultCount, AddedResultCount, RemovedResultCount)
              VALUES (@originalSearchRequestId, @repeatSearchRequestId, @dateCreated, @dateCreated, 0, 0, 0)",
            new { originalSearchRequestId, repeatSearchRequestId = NewSearchId(), dateCreated = AsUtcOffset(dateCreatedUtc) });
    }

    private async Task InsertSearchRequest(string searchIdentifier, DateTime requestTimeUtc)
    {
        await using var connection = new SqlConnection(IntegrationTestSetUp.ConnectionString);
        await connection.ExecuteAsync(
            @"INSERT INTO SearchTracking.SearchRequests
                (SearchIdentifier, IsRepeatSearch, RequestJson, SearchCriteria, DonorType, RequestTimeUtc, IsMatchPredictionRun, AreBetterMatchesIncluded)
              VALUES (@searchIdentifier, 0, @requestJson, @searchCriteria, @donorType, @requestTimeUtc, 0, 0)",
            new
            {
                searchIdentifier,
                requestJson = fixture.Create<string>(),
                searchCriteria = fixture.Create<string>()[..10],
                donorType = fixture.Create<string>()[..10],
                requestTimeUtc
            });
    }

    private static async Task<DateTimeOffset> GetLastUsedAt(string searchRequestId)
    {
        await using var connection = new SqlConnection(IntegrationTestSetUp.ConnectionString);
        return await connection.ExecuteScalarAsync<DateTimeOffset>(
            "SELECT LastUsedAt FROM RepeatSearch.CanonicalResultSets WHERE OriginalSearchRequestId = @searchRequestId",
            new { searchRequestId });
    }
}
