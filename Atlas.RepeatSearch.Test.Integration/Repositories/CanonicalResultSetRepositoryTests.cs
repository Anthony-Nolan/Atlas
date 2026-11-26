using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.RepeatSearch.Data.Models;
using Atlas.RepeatSearch.Data.Repositories;
using Atlas.RepeatSearch.Data.Settings;
using AutoFixture;
using AwesomeAssertions;
using Dapper;
using Microsoft.Data.SqlClient;
using NUnit.Framework;

namespace Atlas.RepeatSearch.Test.Integration.Repositories;

[TestFixture]
public class CanonicalResultSetRepositoryTests
{
    /// <summary>
    /// The retention tests make their old sets older than this cut-off. All other sets are last used now.
    /// </summary>
    private static readonly DateTimeOffset ExpiryCutOff = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// The retention tests give their sets dates before this time only.
    /// </summary>
    private static readonly DateTimeOffset RetentionTestDatesBefore = ExpiryCutOff.AddDays(1);

    private static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(1);

    private Fixture fixture;
    private ICanonicalResultSetRepository repository;

    // Each test uses its own search id, so the tests do not need to clear the tables.
    private string searchRequestId;

    [SetUp]
    public async Task SetUp()
    {
        repository = new CanonicalResultSetRepository(
            new ConnectionStrings { RepeatSearchSqlConnectionString = IntegrationTestSetUp.ConnectionString },
            new StoreOriginalSearchResultsBulkCopySettings { BatchSize = 10000, Timeout = 30 });

        fixture = new Fixture();
        searchRequestId = fixture.Create<string>();

        // The retention job takes the oldest set in the whole database, so the sets that earlier retention tests left are deleted first.
        await using var connection = new SqlConnection(IntegrationTestSetUp.ConnectionString);
        await connection.ExecuteAsync(
            "DELETE FROM RepeatSearch.CanonicalResultSets WHERE LastUsedAt < @RetentionTestDatesBefore",
            new { RetentionTestDatesBefore });
    }

    [Test]
    public async Task StartStoringResultSet_ForNewSearch_CreatesSetInProgress()
    {
        var claim = await repository.StartStoringResultSet(searchRequestId);

        claim.Should().NotBeNull();
        claim.TookOver.Should().BeFalse();
        var set = await repository.GetCanonicalResultSetSummary(searchRequestId);
        set.Id.Should().Be(claim.SetId);
        set.Status.Should().Be(CanonicalResultSetStatus.InProgress);
        set.StoreAttemptId.Should().Be(claim.StoreAttemptId);
    }

    [Test]
    public async Task CompleteResultSet_AfterChunks_StoresAllDonorCodesAndCompletesSet()
    {
        var donorCodes = fixture.CreateMany<string>(3).ToList();
        var claim = await repository.StartStoringResultSet(searchRequestId);

        await repository.AddResultsChunk(claim, donorCodes.Take(2).ToList());
        await repository.AddResultsChunk(claim, donorCodes.Skip(2).ToList());
        await repository.CompleteResultSet(claim);

        (await GetStoredDonorCodes()).Should().BeEquivalentTo(donorCodes);
        (await repository.GetCanonicalResultSetSummary(searchRequestId)).Status.Should().Be(CanonicalResultSetStatus.Complete);
    }

    [Test]
    public async Task StartStoringResultSet_AfterPartialInsert_TakesOverAndRetryStoresExactSet()
    {
        var donorCodes = fixture.CreateMany<string>(3).ToList();
        var firstClaim = await repository.StartStoringResultSet(searchRequestId);
        await repository.AddResultsChunk(firstClaim, donorCodes.Take(2).ToList());
        // The first delivery fails here, before the next chunk.

        var retryClaim = await repository.StartStoringResultSet(searchRequestId);
        retryClaim.TookOver.Should().BeTrue();
        retryClaim.SetId.Should().Be(firstClaim.SetId);
        (await GetStoredDonorCodes()).Should().BeEmpty();

        await repository.AddResultsChunk(retryClaim, donorCodes.Take(2).ToList());
        await repository.AddResultsChunk(retryClaim, donorCodes.Skip(2).ToList());
        await repository.CompleteResultSet(retryClaim);

        (await GetStoredDonorCodes()).Should().BeEquivalentTo(donorCodes);
        (await repository.GetCanonicalResultSetSummary(searchRequestId)).Status.Should().Be(CanonicalResultSetStatus.Complete);
    }

    [Test]
    public async Task StartStoringResultSet_WhenTakingOverLargeSet_DeletesAllRowsInBatches()
    {
        var firstClaim = await repository.StartStoringResultSet(searchRequestId);
        // More than two delete batches of 10,000.
        await repository.AddResultsChunk(firstClaim, fixture.CreateMany<string>(25001).ToList());

        await repository.StartStoringResultSet(searchRequestId);

        (await GetStoredDonorCodes()).Should().BeEmpty();
    }

    [Test]
    public async Task AddResultsChunk_AfterAnotherDeliveryTookOver_Throws()
    {
        var lostClaim = await repository.StartStoringResultSet(searchRequestId);
        await repository.StartStoringResultSet(searchRequestId);

        var act = () => repository.AddResultsChunk(lostClaim, fixture.CreateMany<string>(1).ToList());

        await act.Should().ThrowAsync<CanonicalResultSetClaimLostException>();
        (await GetStoredDonorCodes()).Should().BeEmpty();
    }

    [Test]
    public async Task AddResultsChunk_WithDonorCodeAlreadyInSet_Throws()
    {
        // The unique index on (CanonicalResultSetId, ExternalDonorCode) stops a chunk of a superseded delivery from adding a code twice (ATL-528).
        var donorCode = fixture.Create<string>();
        var claim = await repository.StartStoringResultSet(searchRequestId);
        await repository.AddResultsChunk(claim, new List<string> { donorCode });

        var act = () => repository.AddResultsChunk(claim, new List<string> { donorCode });

        await act.Should().ThrowAsync<SqlException>().Where(e => e.Number == 2601);
        (await GetStoredDonorCodes()).Should().ContainSingle();
    }

    [Test]
    public async Task CompleteResultSet_AfterAnotherDeliveryTookOver_ThrowsAndLeavesSetInProgress()
    {
        var lostClaim = await repository.StartStoringResultSet(searchRequestId);
        await repository.StartStoringResultSet(searchRequestId);

        var act = () => repository.CompleteResultSet(lostClaim);

        await act.Should().ThrowAsync<CanonicalResultSetClaimLostException>();
        (await repository.GetCanonicalResultSetSummary(searchRequestId)).Status.Should().Be(CanonicalResultSetStatus.InProgress);
    }

    [Test]
    public async Task StartStoringResultSet_AfterSetIsComplete_ReturnsNullAndKeepsRows()
    {
        var donorCodes = fixture.CreateMany<string>(2).ToList();
        var claim = await repository.StartStoringResultSet(searchRequestId);
        await repository.AddResultsChunk(claim, donorCodes);
        await repository.CompleteResultSet(claim);

        var repeatDeliveryClaim = await repository.StartStoringResultSet(searchRequestId);

        repeatDeliveryClaim.Should().BeNull();
        (await GetStoredDonorCodes()).Should().BeEquivalentTo(donorCodes);
        (await repository.GetCanonicalResultSetSummary(searchRequestId)).Status.Should().Be(CanonicalResultSetStatus.Complete);
    }

    [Test]
    public async Task CanonicalResultSet_InsertedWithoutStatus_IsComplete()
    {
        // As a set stored before the Status column existed: the migration gives these sets the default value.
        await using (var connection = new SqlConnection(IntegrationTestSetUp.ConnectionString))
        {
            await connection.ExecuteAsync(
                "INSERT INTO RepeatSearch.CanonicalResultSets (OriginalSearchRequestId) VALUES (@searchRequestId)",
                new { searchRequestId });
        }

        (await repository.GetCanonicalResultSetSummary(searchRequestId)).Status.Should().Be(CanonicalResultSetStatus.Complete);
        (await repository.StartStoringResultSet(searchRequestId)).Should().BeNull();
    }

    [Test]
    public async Task CanonicalResultSet_InsertedWithoutLastUsedAt_IsLastUsedNow()
    {
        // The column default: a row inserted without a value can never be expired at once.
        await using (var connection = new SqlConnection(IntegrationTestSetUp.ConnectionString))
        {
            await connection.ExecuteAsync(
                "INSERT INTO RepeatSearch.CanonicalResultSets (OriginalSearchRequestId) VALUES (@searchRequestId)",
                new { searchRequestId });
        }

        (await repository.GetCanonicalResultSetSummary(searchRequestId)).LastUsedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, ClockTolerance);
    }

    [Test]
    public async Task StartStoringResultSet_ForNewSearch_SetsLastUsedAtToNow()
    {
        await repository.StartStoringResultSet(searchRequestId);

        (await repository.GetCanonicalResultSetSummary(searchRequestId)).LastUsedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, ClockTolerance);
    }

    [Test]
    public async Task StartStoringResultSet_WhenTakingOver_SetsLastUsedAtToNow()
    {
        var firstClaim = await repository.StartStoringResultSet(searchRequestId);
        await SetLastUsedAt(firstClaim.SetId, ExpiryCutOff.AddDays(-1));

        await repository.StartStoringResultSet(searchRequestId);

        (await repository.GetCanonicalResultSetSummary(searchRequestId)).LastUsedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, ClockTolerance);
    }

    [Test]
    public async Task MarkResultSetUsed_ForCompleteSet_ReturnsTrueAndSetsLastUsedAtToNow()
    {
        var claim = await StoreCompleteSet(searchRequestId, fixture.CreateMany<string>().ToList());
        await SetLastUsedAt(claim.SetId, ExpiryCutOff.AddDays(-1));

        var isMarked = await repository.MarkResultSetUsed(searchRequestId);

        isMarked.Should().BeTrue();
        (await repository.GetCanonicalResultSetSummary(searchRequestId)).LastUsedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, ClockTolerance);
    }

    [Test]
    public async Task MarkResultSetUsed_ForSetInProgress_ReturnsFalseAndKeepsLastUsedAt()
    {
        var oldDate = ExpiryCutOff.AddDays(-1);
        var claim = await repository.StartStoringResultSet(searchRequestId);
        await SetLastUsedAt(claim.SetId, oldDate);

        var isMarked = await repository.MarkResultSetUsed(searchRequestId);

        isMarked.Should().BeFalse();
        (await repository.GetCanonicalResultSetSummary(searchRequestId)).LastUsedAt.Should().Be(oldDate);
    }

    [Test]
    public async Task MarkResultSetUsed_ForUnknownSearch_ReturnsFalse()
    {
        var isMarked = await repository.MarkResultSetUsed(searchRequestId);

        isMarked.Should().BeFalse();
    }

    [Test]
    public async Task StartDeletingExpiredResultSet_WithSetsOnBothSidesOfCutOff_MarksOnlyTheOldSetDeleting()
    {
        var oldClaim = await StoreCompleteSet(searchRequestId, fixture.CreateMany<string>().ToList());
        await SetLastUsedAt(oldClaim.SetId, ExpiryCutOff.AddMinutes(-1));
        var recentSearchRequestId = fixture.Create<string>();
        var recentDonorCodes = fixture.CreateMany<string>().ToList();
        var recentClaim = await StoreCompleteSet(recentSearchRequestId, recentDonorCodes);
        await SetLastUsedAt(recentClaim.SetId, ExpiryCutOff.AddMinutes(1));

        var setId = await repository.StartDeletingExpiredResultSet(ExpiryCutOff);

        setId.Should().Be(oldClaim.SetId);
        (await repository.GetCanonicalResultSetSummary(searchRequestId)).Status.Should().Be(CanonicalResultSetStatus.Deleting);
        (await repository.GetCanonicalResultSetSummary(recentSearchRequestId)).Status.Should().Be(CanonicalResultSetStatus.Complete);
        (await repository.MarkResultSetUsed(recentSearchRequestId)).Should().BeTrue();
        (await GetStoredDonorCodes(recentSearchRequestId)).Should().BeEquivalentTo(recentDonorCodes);
    }

    [Test]
    public async Task StartDeletingExpiredResultSet_ForSetLeftDeletingByEarlierRun_ReturnsItAgain()
    {
        var claim = await StoreCompleteSet(searchRequestId, fixture.CreateMany<string>().ToList());
        await SetLastUsedAt(claim.SetId, ExpiryCutOff.AddDays(-1));
        await repository.StartDeletingExpiredResultSet(ExpiryCutOff);

        var setId = await repository.StartDeletingExpiredResultSet(ExpiryCutOff);

        setId.Should().Be(claim.SetId);
    }

    [Test]
    public async Task StartDeletingExpiredResultSet_ForSetInProgressOlderThanCutOff_MarksItDeleting()
    {
        // An abandoned store: its delivery can no longer add chunks after the set is taken.
        var claim = await repository.StartStoringResultSet(searchRequestId);
        await SetLastUsedAt(claim.SetId, ExpiryCutOff.AddDays(-1));

        var setId = await repository.StartDeletingExpiredResultSet(ExpiryCutOff);

        setId.Should().Be(claim.SetId);
        (await repository.GetCanonicalResultSetSummary(searchRequestId)).Status.Should().Be(CanonicalResultSetStatus.Deleting);
        var act = () => repository.AddResultsChunk(claim, fixture.CreateMany<string>().ToList());
        await act.Should().ThrowAsync<CanonicalResultSetClaimLostException>();
    }

    [Test]
    public async Task StartDeletingExpiredResultSet_WithTwoExpiredSets_TakesOldestFirst()
    {
        var newerClaim = await StoreCompleteSet(searchRequestId, fixture.CreateMany<string>().ToList());
        await SetLastUsedAt(newerClaim.SetId, ExpiryCutOff.AddDays(-1));
        var olderClaim = await StoreCompleteSet(fixture.Create<string>(), fixture.CreateMany<string>().ToList());
        await SetLastUsedAt(olderClaim.SetId, ExpiryCutOff.AddDays(-2));

        var firstSetId = await repository.StartDeletingExpiredResultSet(ExpiryCutOff);
        await repository.DeleteResultsBatch(olderClaim.SetId, 10);
        await repository.DeleteResultSet(olderClaim.SetId);
        var secondSetId = await repository.StartDeletingExpiredResultSet(ExpiryCutOff);

        firstSetId.Should().Be(olderClaim.SetId);
        secondSetId.Should().Be(newerClaim.SetId);
    }

    [Test]
    public async Task StartDeletingExpiredResultSet_WhenNoSetIsOlderThanCutOff_ReturnsNull()
    {
        await StoreCompleteSet(searchRequestId, fixture.CreateMany<string>().ToList());

        var setId = await repository.StartDeletingExpiredResultSet(ExpiryCutOff);

        setId.Should().BeNull();
    }

    [Test]
    public async Task StartDeletingExpiredResultSet_AfterRepeatSearchMarkedOldSetUsed_DoesNotTakeIt()
    {
        var claim = await StoreCompleteSet(searchRequestId, fixture.CreateMany<string>().ToList());
        await SetLastUsedAt(claim.SetId, ExpiryCutOff.AddDays(-1));
        await repository.MarkResultSetUsed(searchRequestId);

        var setId = await repository.StartDeletingExpiredResultSet(ExpiryCutOff);

        setId.Should().BeNull();
        (await repository.GetCanonicalResultSetSummary(searchRequestId)).Status.Should().Be(CanonicalResultSetStatus.Complete);
    }

    [Test]
    public async Task MarkResultSetUsed_AfterSetIsMarkedDeleting_ReturnsFalse()
    {
        var oldDate = ExpiryCutOff.AddDays(-1);
        var claim = await StoreCompleteSet(searchRequestId, fixture.CreateMany<string>().ToList());
        await SetLastUsedAt(claim.SetId, oldDate);
        await repository.StartDeletingExpiredResultSet(ExpiryCutOff);

        var isMarked = await repository.MarkResultSetUsed(searchRequestId);

        isMarked.Should().BeFalse();
        (await repository.GetCanonicalResultSetSummary(searchRequestId)).LastUsedAt.Should().Be(oldDate);
    }

    [Test]
    public async Task DeleteResultsBatch_DeletesAtMostBatchSizeRows_OnlyFromThatSet()
    {
        var claim = await StoreCompleteSet(searchRequestId, fixture.CreateMany<string>(5).ToList());
        var otherSearchRequestId = fixture.Create<string>();
        var otherDonorCodes = fixture.CreateMany<string>(3).ToList();
        await StoreCompleteSet(otherSearchRequestId, otherDonorCodes);

        var firstBatch = await repository.DeleteResultsBatch(claim.SetId, 2);
        var secondBatch = await repository.DeleteResultsBatch(claim.SetId, 4);

        firstBatch.Should().Be(2);
        secondBatch.Should().Be(3);
        (await GetStoredDonorCodes()).Should().BeEmpty();
        (await GetStoredDonorCodes(otherSearchRequestId)).Should().BeEquivalentTo(otherDonorCodes);
    }

    [Test]
    public async Task DeleteResultSet_ForSetBeingDeleted_DeletesSetRow()
    {
        var claim = await StoreCompleteSet(searchRequestId, fixture.CreateMany<string>().ToList());
        await SetLastUsedAt(claim.SetId, ExpiryCutOff.AddDays(-1));
        await repository.StartDeletingExpiredResultSet(ExpiryCutOff);
        await repository.DeleteResultsBatch(claim.SetId, 10);

        await repository.DeleteResultSet(claim.SetId);

        (await repository.GetCanonicalResultSetSummary(searchRequestId)).Should().BeNull();
    }

    [Test]
    public async Task DeleteResultSet_ForCompleteSet_KeepsSet()
    {
        var donorCodes = fixture.CreateMany<string>().ToList();
        var claim = await StoreCompleteSet(searchRequestId, donorCodes);

        await repository.DeleteResultSet(claim.SetId);

        (await repository.GetCanonicalResultSetSummary(searchRequestId)).Status.Should().Be(CanonicalResultSetStatus.Complete);
        (await GetStoredDonorCodes()).Should().BeEquivalentTo(donorCodes);
    }

    private async Task<CanonicalResultSetStoreClaim> StoreCompleteSet(string id, IReadOnlyCollection<string> donorCodes)
    {
        var claim = await repository.StartStoringResultSet(id);
        await repository.AddResultsChunk(claim, donorCodes);
        await repository.CompleteResultSet(claim);
        return claim;
    }

    private static async Task SetLastUsedAt(int setId, DateTimeOffset lastUsedAt)
    {
        await using var connection = new SqlConnection(IntegrationTestSetUp.ConnectionString);
        await connection.ExecuteAsync(
            "UPDATE RepeatSearch.CanonicalResultSets SET LastUsedAt = @lastUsedAt WHERE Id = @setId",
            new { setId, lastUsedAt });
    }

    private Task<IReadOnlyCollection<string>> GetStoredDonorCodes() => GetStoredDonorCodes(searchRequestId);

    private async Task<IReadOnlyCollection<string>> GetStoredDonorCodes(string id)
    {
        return (await repository.GetCanonicalResults(id)).Select(r => r.ExternalDonorCode).ToList();
    }
}
