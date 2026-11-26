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
    private Fixture fixture;
    private ICanonicalResultSetRepository repository;

    // Each test uses its own search id, so the tests do not need to clear the tables.
    private string searchRequestId;

    [SetUp]
    public void SetUp()
    {
        repository = new CanonicalResultSetRepository(
            new ConnectionStrings { RepeatSearchSqlConnectionString = IntegrationTestSetUp.ConnectionString },
            new StoreOriginalSearchResultsBulkCopySettings { BatchSize = 10000, Timeout = 30 });

        fixture = new Fixture();
        searchRequestId = fixture.Create<string>();
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
    public async Task ApplyDiffAndRecordRepeatSearch_AddsAndRemovesDonorsAndWritesHistory()
    {
        var keptDonorCode = fixture.Create<string>();
        var removedDonorCodes = fixture.CreateMany<string>(2).ToList();
        var newDonorCodes = fixture.CreateMany<string>(2).ToList();
        await StoreCompleteSet(removedDonorCodes.Append(keptDonorCode).ToList());
        var historyRecord = BuildHistoryRecord();

        var applied = await repository.ApplyDiffAndRecordRepeatSearch(historyRecord, newDonorCodes, removedDonorCodes);

        applied.Should().BeTrue();
        (await GetStoredDonorCodes()).Should().BeEquivalentTo(newDonorCodes.Append(keptDonorCode));
        (await GetHistoryRecords(historyRecord.RepeatSearchRequestId)).Should().ContainSingle()
            .Which.Should().BeEquivalentTo(historyRecord, options => options.Excluding(r => r.Id));
    }

    [Test]
    public async Task ApplyDiffAndRecordRepeatSearch_ForRecordedRepeatSearch_ReturnsFalseAndChangesNothing()
    {
        var newDonorCode = fixture.Create<string>();
        await StoreCompleteSet(new List<string>());
        var historyRecord = BuildHistoryRecord();
        await repository.ApplyDiffAndRecordRepeatSearch(historyRecord, new[] { newDonorCode }, Array.Empty<string>());

        var appliedAgain = await repository.ApplyDiffAndRecordRepeatSearch(
            historyRecord, new[] { fixture.Create<string>() }, new[] { newDonorCode });

        appliedAgain.Should().BeFalse();
        (await GetStoredDonorCodes()).Should().BeEquivalentTo(new[] { newDonorCode });
        (await GetHistoryRecords(historyRecord.RepeatSearchRequestId)).Should().ContainSingle();
    }

    [Test]
    public async Task ApplyDiffAndRecordRepeatSearch_ForConcurrentDeliveries_ChangesSetOnce()
    {
        var newDonorCode = fixture.Create<string>();
        await StoreCompleteSet(new List<string>());
        var historyRecord = BuildHistoryRecord();

        var results = await Task.WhenAll(
            repository.ApplyDiffAndRecordRepeatSearch(historyRecord, new[] { newDonorCode }, Array.Empty<string>()),
            repository.ApplyDiffAndRecordRepeatSearch(historyRecord, new[] { newDonorCode }, Array.Empty<string>()));

        results.Should().BeEquivalentTo(new[] { true, false });
        (await GetStoredDonorCodes()).Should().BeEquivalentTo(new[] { newDonorCode });
        (await GetHistoryRecords(historyRecord.RepeatSearchRequestId)).Should().ContainSingle();
    }

    [Test]
    public async Task ApplyDiffAndRecordRepeatSearch_WhenSetChangeFails_DoesNotWriteHistoryOrChangeSet()
    {
        var donorCodeInSet = fixture.Create<string>();
        var removedDonorCode = fixture.Create<string>();
        await StoreCompleteSet(new List<string> { donorCodeInSet, removedDonorCode });
        var historyRecord = BuildHistoryRecord();

        // A donor code that is already in the set breaks the unique index of SearchResults.
        var act = () => repository.ApplyDiffAndRecordRepeatSearch(historyRecord, new[] { donorCodeInSet }, new[] { removedDonorCode });

        await act.Should().ThrowAsync<SqlException>().Where(e => e.Number == 2601);
        (await GetStoredDonorCodes()).Should().BeEquivalentTo(new[] { donorCodeInSet, removedDonorCode });
        (await GetHistoryRecords(historyRecord.RepeatSearchRequestId)).Should().BeEmpty();
    }

    [Test]
    public async Task ApplyDiffAndRecordRepeatSearch_WhenHistoryInsertFails_DoesNotChangeSet()
    {
        var removedDonorCode = fixture.Create<string>();
        var newDonorCode = fixture.Create<string>();
        await StoreCompleteSet(new List<string> { removedDonorCode });
        // RepeatSearchRequestId is required, so the insert of the history record fails after the donors are added and removed.
        var historyRecord = BuildHistoryRecord();
        historyRecord.RepeatSearchRequestId = null;

        var act = () => repository.ApplyDiffAndRecordRepeatSearch(historyRecord, new[] { newDonorCode }, new[] { removedDonorCode });

        await act.Should().ThrowAsync<SqlException>().Where(e => e.Number == 515);
        (await GetStoredDonorCodes()).Should().BeEquivalentTo(new[] { removedDonorCode });
    }

    private async Task StoreCompleteSet(IReadOnlyCollection<string> donorCodes)
    {
        var claim = await repository.StartStoringResultSet(searchRequestId);
        await repository.AddResultsChunk(claim, donorCodes);
        await repository.CompleteResultSet(claim);
    }

    private RepeatSearchHistoryRecord BuildHistoryRecord() =>
        fixture.Build<RepeatSearchHistoryRecord>().With(r => r.OriginalSearchRequestId, searchRequestId).Create();

    private static async Task<IReadOnlyCollection<RepeatSearchHistoryRecord>> GetHistoryRecords(string repeatSearchRequestId)
    {
        await using var connection = new SqlConnection(IntegrationTestSetUp.ConnectionString);
        return (await connection.QueryAsync<RepeatSearchHistoryRecord>(
            "SELECT * FROM RepeatSearch.RepeatSearchHistoryRecords WHERE RepeatSearchRequestId = @repeatSearchRequestId",
            new { repeatSearchRequestId })).ToList();
    }

    private async Task<IReadOnlyCollection<string>> GetStoredDonorCodes()
    {
        return (await repository.GetCanonicalResults(searchRequestId)).Select(r => r.ExternalDonorCode).ToList();
    }
}
