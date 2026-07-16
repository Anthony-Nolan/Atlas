using Atlas.Client.Models.Search.Requests;
using Atlas.Common.ApplicationInsights;
using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.Validation;
using Atlas.ManualTesting.Models;
using Atlas.ManualTesting.Services;
using Atlas.SearchTracking.Data.Context;
using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;
using MatchPredictionRecord = Atlas.SearchTracking.Data.Models.SearchRequestMatchPrediction;
using TrackedSearchRequest = Atlas.SearchTracking.Data.Models.SearchRequest;

namespace Atlas.ManualTesting.Test.Tests;

[TestFixture]
internal class FailedParallelSearchReplayerTests
{
    // Window used by every test unless stated otherwise.
    private static readonly DateTime WindowFrom = new(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime WindowTo = new(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime InWindow = new(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OutOfWindow = new(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);

    private SqliteConnection connection;
    private SearchTrackingContext context;
    private IPublicApiFunctionsClient publicApiClient;
    private IFailedParallelSearchReplayer replayer;

    [SetUp]
    public async Task SetUp()
    {
        // SQLite in-memory, matching the repo's SearchTrackingContext test pattern (see SqliteMemoryContext).
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<SearchTrackingContext>()
            .UseSqlite(connection)
            .Options;
        context = new SearchTrackingContext(options);
        await context.Database.EnsureCreatedAsync();

        publicApiClient = Substitute.For<IPublicApiFunctionsClient>();
        publicApiClient.PostSearchRequest(Arg.Any<SearchRequest>())
            .Returns(SuccessResponse("new-search-id"));
        publicApiClient.PostRepeatSearchRequest(Arg.Any<RepeatSearchRequest>())
            .Returns(SuccessResponse(repeatSearchId: "new-repeat-id"));

        replayer = new FailedParallelSearchReplayer(context, publicApiClient, Substitute.For<IAtlasLogger>());
    }

    [TearDown]
    public void TearDown()
    {
        context.Dispose();
        connection.Close();
    }

    [Test]
    public async Task ReplayFailedParallelSearches_DryRun_ReturnsCandidatesButDispatchesNothing()
    {
        await AddSearch(isParallel: true, isSuccessful: false, resultsSent: null);

        var response = await replayer.ReplayFailedParallelSearches(Request(dryRun: true));

        response.DryRun.Should().BeTrue();
        response.CandidateCount.Should().Be(1);
        response.Replays.Should().BeEmpty();
        response.ReplayedCount.Should().Be(0);
        await publicApiClient.DidNotReceive().PostSearchRequest(Arg.Any<SearchRequest>());
    }

    [Test]
    public async Task ReplayFailedParallelSearches_SelectsFailedAndIncompleteParallelSearchesOnly()
    {
        var failed = await AddSearch(isParallel: true, isSuccessful: false, resultsSent: null);
        var incomplete = await AddSearch(isParallel: true, isSuccessful: null, resultsSent: null);
        await AddSearch(isParallel: true, isSuccessful: true, resultsSent: true);   // succeeded → excluded
        await AddSearch(isParallel: false, isSuccessful: false, resultsSent: null);  // sequential → excluded

        var response = await replayer.ReplayFailedParallelSearches(Request(dryRun: true));

        response.Candidates.Select(c => c.SearchIdentifier)
            .Should().BeEquivalentTo(new[] { failed.SearchIdentifier, incomplete.SearchIdentifier });
    }

    [Test]
    public async Task ReplayFailedParallelSearches_ExcludesSearchesOutsideTheTimeWindow()
    {
        await AddSearch(isParallel: true, isSuccessful: false, resultsSent: null, requestTime: OutOfWindow);

        var response = await replayer.ReplayFailedParallelSearches(Request(dryRun: true));

        response.CandidateCount.Should().Be(0);
    }

    [Test]
    public async Task ReplayFailedParallelSearches_RegularSearch_ReDispatchesWithParallelMatchPredictionFalse()
    {
        await AddSearch(isParallel: true, isSuccessful: false, resultsSent: null, requestJson: "{}");

        var response = await replayer.ReplayFailedParallelSearches(Request(dryRun: false));

        await publicApiClient.Received(1)
            .PostSearchRequest(Arg.Is<SearchRequest>(r => r.ParallelMatchPrediction == false));
        response.ReplayedCount.Should().Be(1);
        response.Replays.Single().WasReplayed.Should().BeTrue();
        response.Replays.Single().NewSearchIdentifier.Should().Be("new-search-id");
    }

    [Test]
    public async Task ReplayFailedParallelSearches_RepeatSearch_UsesRepeatEndpointWithParallelMatchPredictionFalse()
    {
        await AddSearch(isParallel: true, isSuccessful: false, resultsSent: null,
            isRepeat: true, requestJson: "{\"SearchRequest\":{}}");

        var response = await replayer.ReplayFailedParallelSearches(Request(dryRun: false));

        await publicApiClient.Received(1)
            .PostRepeatSearchRequest(Arg.Is<RepeatSearchRequest>(r => r.SearchRequest.ParallelMatchPrediction == false));
        await publicApiClient.DidNotReceive().PostSearchRequest(Arg.Any<SearchRequest>());
        response.Replays.Single().NewSearchIdentifier.Should().Be("new-repeat-id");
    }

    [Test]
    public async Task ReplayFailedParallelSearches_WithAllowList_OnlyReplaysListedIdentifiers()
    {
        var chosen = await AddSearch(isParallel: true, isSuccessful: false, resultsSent: null);
        await AddSearch(isParallel: true, isSuccessful: false, resultsSent: null);

        var response = await replayer.ReplayFailedParallelSearches(
            Request(dryRun: false, searchIdentifiers: new List<Guid> { chosen.SearchIdentifier }));

        response.ReplayedCount.Should().Be(1);
        await publicApiClient.Received(1).PostSearchRequest(Arg.Any<SearchRequest>());
        response.Replays.Single().OriginalSearchIdentifier.Should().Be(chosen.SearchIdentifier);
    }

    [Test]
    public async Task ReplayFailedParallelSearches_WhenDispatchValidationFails_RecordsFailedOutcome()
    {
        await AddSearch(isParallel: true, isSuccessful: false, resultsSent: null);
        publicApiClient.PostSearchRequest(Arg.Any<SearchRequest>())
            .Returns(new ResponseFromValidatedRequest<SearchInitiationResponse>(new List<RequestValidationFailure>()));

        var response = await replayer.ReplayFailedParallelSearches(Request(dryRun: false));

        response.ReplayedCount.Should().Be(0);
        response.FailedToReplayCount.Should().Be(1);
        response.Replays.Single().WasReplayed.Should().BeFalse();
        response.Replays.Single().Error.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task ReplayFailedParallelSearches_WhenStoredRequestJsonIsInvalid_RecordsErrorWithoutThrowing()
    {
        await AddSearch(isParallel: true, isSuccessful: false, resultsSent: null, requestJson: "not-json");

        var response = await replayer.ReplayFailedParallelSearches(Request(dryRun: false));

        response.FailedToReplayCount.Should().Be(1);
        response.Replays.Single().WasReplayed.Should().BeFalse();
        response.Replays.Single().Error.Should().NotBeNullOrEmpty();
    }

    private async Task<TrackedSearchRequest> AddSearch(
        bool isParallel,
        bool? isSuccessful,
        bool? resultsSent,
        DateTime? requestTime = null,
        bool isRepeat = false,
        string requestJson = "{}")
    {
        var time = requestTime ?? InWindow;
        var searchRequest = new TrackedSearchRequest
        {
            SearchIdentifier = Guid.NewGuid(),
            IsRepeatSearch = isRepeat,
            OriginalSearchIdentifier = isRepeat ? Guid.NewGuid() : null,
            RequestJson = requestJson,
            SearchCriteria = "SIX",
            DonorType = "Adult",
            RequestTimeUtc = time,
            ResultsSent = resultsSent,
            IsMatchPredictionRun = true,
            MatchPrediction = new MatchPredictionRecord
            {
                InitiationTimeUtc = time,
                StartTimeUtc = time,
                IsParallelMatchPrediction = isParallel,
                IsSuccessful = isSuccessful
            }
        };

        context.SearchRequests.Add(searchRequest);
        await context.SaveChangesAsync();
        return searchRequest;
    }

    private static ParallelMatchPredictionReplayRequest Request(bool dryRun, List<Guid> searchIdentifiers = null) => new()
    {
        FromRequestTimeUtc = WindowFrom,
        ToRequestTimeUtc = WindowTo,
        DryRun = dryRun,
        SearchIdentifiers = searchIdentifiers
    };

    private static ResponseFromValidatedRequest<SearchInitiationResponse> SuccessResponse(
        string searchId = null, string repeatSearchId = null) =>
        new(new SearchInitiationResponse { SearchIdentifier = searchId, RepeatSearchIdentifier = repeatSearchId });
}
