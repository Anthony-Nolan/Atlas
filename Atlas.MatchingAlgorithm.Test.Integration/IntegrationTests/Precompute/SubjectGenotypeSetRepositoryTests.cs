using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using AutoFixture;
using AwesomeAssertions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ContextFactory = Atlas.MatchingAlgorithm.Data.Context.ContextFactory;
using Injection = Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection.DependencyInjection;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Precompute;

/// <summary>
/// The repository's behaviour against a real database.
/// <c>DonorSubjectGenotypeSetSchemaTests</c> proves the constraints exist; these prove the repository behaves
/// correctly given them.
///
/// <para>
/// <c>GetOrCreateValueIds_AcrossSeparateRepositoryInstances_ReturnsTheSameIds</c> is the test that fails if a
/// key-to-id cache is reintroduced. <c>GetOrCreateValueIds_ConcurrentlyForTheSameNewKey_StoresOneRowAndAgreesOnItsId</c>
/// asserts the OUTCOME of a race, which the repository defends twice over - the guarded insert's lock hints, and the
/// retry behind them - so it passes with either one alone and only fails when both are gone. It is also
/// timing-dependent by nature: it catches the bug often, it does not prove the absence of one.
/// </para>
/// </summary>
[TestFixture]
public class SubjectGenotypeSetRepositoryTests
{
    private const int LargePayloadSizeInBytes = 400_000;

    private Fixture fixture;
    private string transientConnectionString;
    private ISubjectGenotypeSetRepository repository;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();

        var connectionStringFactory = Injection.Provider.GetService<StaticallyChosenTransientSqlConnectionStringProviderFactory>();
        transientConnectionString = connectionStringFactory.GenerateConnectionStringProvider(TransientDatabase.DatabaseA).GetConnectionString();

        repository = NewRepository();

        DatabaseManager.ClearTransientDatabases();
    }

    [Test]
    public async Task GetOrCreateValueIds_ForANewKey_StoresOneRowAndReturnsItsId()
    {
        var value = NewValue();

        var ids = await repository.GetOrCreateValueIds([value]);

        ids.Should().ContainKey(value.Key);
        (await StoredValueCount()).Should().Be(1);
        (await StoredValue(ids[value.Key])).SubjectGenotypeSetData.Should().BeEquivalentTo(value.SubjectGenotypeSetData);
    }

    [Test]
    public async Task GetOrCreateValueIds_WithTheSameKeyTwiceInOneCall_StoresOneRow()
    {
        // The staging table's primary key is the natural key, so an un-deduplicated input would fail the bulk copy
        // rather than store twice - a caller mistake surfacing as an opaque constraint error.
        var key = NewKey();
        var first = new SubjectGenotypeSetValueToStore(key, false, fixture.CreateMany<byte>(64).ToArray());
        var second = new SubjectGenotypeSetValueToStore(key, false, fixture.CreateMany<byte>(64).ToArray());

        var ids = await repository.GetOrCreateValueIds([first, second]);

        ids.Should().HaveCount(1);
        (await StoredValueCount()).Should().Be(1);
    }

    [Test]
    public async Task GetOrCreateValueIds_AcrossSeparateRepositoryInstances_ReturnsTheSameIds()
    {
        // The test that fails if anyone gives this repository a key-to-id cache. A cache would also survive a swap of
        // the transient database, and hand out ids belonging to the other one.
        var value = NewValue();

        var firstIds = await repository.GetOrCreateValueIds([value]);
        var secondIds = await NewRepository().GetOrCreateValueIds([value]);

        secondIds.Should().BeEquivalentTo(firstIds);
        (await StoredValueCount()).Should().Be(1);
    }

    [Test]
    public async Task GetOrCreateValueIds_ForAKeyAlreadyStored_LeavesTheStoredPayloadUnchanged()
    {
        // The key is content-derived, so a second payload for it describes the same genotype set. Rewriting would cost
        // a varbinary(max) update to store what is already there - and this asserts byte-for-byte that it does not.
        var stored = NewValue();
        var ids = await repository.GetOrCreateValueIds([stored]);

        await repository.GetOrCreateValueIds([stored with { SubjectGenotypeSetData = fixture.CreateMany<byte>(128).ToArray() }]);

        (await StoredValue(ids[stored.Key])).SubjectGenotypeSetData.Should().BeEquivalentTo(stored.SubjectGenotypeSetData);
    }

    [Test]
    public async Task GetOrCreateValueIds_ForAMixOfNewAndStoredKeys_ReturnsIdsForAllAndInsertsOnlyTheNew()
    {
        var stored = NewValue();
        var storedIds = await repository.GetOrCreateValueIds([stored]);
        var newValue = NewValue();

        var ids = await repository.GetOrCreateValueIds([stored, newValue]);

        ids.Should().HaveCount(2);
        ids[stored.Key].Should().Be(storedIds[stored.Key]);
        (await StoredValueCount()).Should().Be(2);
    }

    [Test]
    public async Task GetOrCreateValueIds_ConcurrentlyForTheSameNewKey_StoresOneRowAndAgreesOnItsId()
    {
        // Eight repositories, therefore eight connections, racing on one key that no row exists for yet. With neither
        // the UPDLOCK/HOLDLOCK probe nor the retry, they all pass NOT EXISTS and the unique index rejects all but one
        // of the inserts - reproducibly, on every run.
        var value = NewValue();
        var repositories = Enumerable.Range(0, 8).Select(_ => NewRepository()).ToList();

        var results = await Task.WhenAll(repositories.Select(r => r.GetOrCreateValueIds([value])));

        (await StoredValueCount()).Should().Be(1);
        results.Select(ids => ids[value.Key]).Distinct().Should().ContainSingle();
    }

    [Test]
    public async Task GetOrCreateValueIds_ForALargePayload_RoundTripsEveryByte()
    {
        // Pins the varbinary(max) DataTable column type. The Columns.Add("Name") form used elsewhere in the data
        // project makes a STRING column, which either fails or mangles the payload.
        var value = new SubjectGenotypeSetValueToStore(NewKey(), false, fixture.CreateMany<byte>(LargePayloadSizeInBytes).ToArray());

        var ids = await repository.GetOrCreateValueIds([value]);

        (await StoredValue(ids[value.Key])).SubjectGenotypeSetData.Should().Equal(value.SubjectGenotypeSetData);
    }

    [Test]
    public async Task GetOrCreateValueIds_ForAnUnrepresentedValue_StoresTheFlagAndANullPayload()
    {
        var value = new SubjectGenotypeSetValueToStore(NewKey(), true, null);

        var ids = await repository.GetOrCreateValueIds([value]);

        var stored = await StoredValue(ids[value.Key]);
        stored.IsUnrepresented.Should().BeTrue();
        stored.SubjectGenotypeSetData.Should().BeNull();
    }

    [Test]
    public async Task GetOrCreateValueIds_ForEveryAllowedLociKey_StoresThemAsDistinctRows()
    {
        var hlaTypingKey = NewHlaTypingKey();
        var frequencySetId = fixture.Create<int>();

        var values = AllowedLociKeyExtensions.All
            .Select(allowedLociKey => NewValue(new SubjectGenotypeSetKey(hlaTypingKey, frequencySetId, allowedLociKey)))
            .ToList();

        var ids = await repository.GetOrCreateValueIds(values);

        ids.Should().HaveCount(4);
        ids.Values.Distinct().Should().HaveCount(4);
        (await StoredValueCount()).Should().Be(4);
    }

    [Test]
    public async Task GetOrCreateValueIds_AcrossMoreThanOneChunk_StoresEachKeyOnce()
    {
        // Enough values to span the staging chunk size, then a second call overlapping every one of them plus a few
        // new ones - so keys that were created in an earlier chunk are re-probed in a later one.
        var values = Enumerable.Range(0, SubjectGenotypeSetRepository.StagingChunkSize + 5).Select(_ => NewValue()).ToList();
        var firstIds = await repository.GetOrCreateValueIds(values);

        var extraValues = Enumerable.Range(0, 3).Select(_ => NewValue()).ToList();
        var secondIds = await repository.GetOrCreateValueIds([..values, ..extraValues]);

        (await StoredValueCount()).Should().Be(values.Count + extraValues.Count);
        foreach (var (key, id) in firstIds)
        {
            secondIds[key].Should().Be(id);
        }
    }

    [Test]
    public async Task GetOrCreateValueIds_WithoutKeyRangeLocks_ForAMixOfNewAndStoredKeys_ReturnsIdsForAllAndInsertsOnlyTheNew()
    {
        var stored = NewValue();
        var storedIds = await repository.GetOrCreateValueIds([stored]);
        var newValue = NewValue();

        var ids = await repository.GetOrCreateValueIds([stored, newValue], SubjectGenotypeSetValueInsertMode.RetryOnDuplicate);

        ids.Should().HaveCount(2);
        ids[stored.Key].Should().Be(storedIds[stored.Key]);
        (await StoredValueCount()).Should().Be(2);
        (await StoredValue(ids[newValue.Key])).SubjectGenotypeSetData.Should().BeEquivalentTo(newValue.SubjectGenotypeSetData);
    }

    [Test]
    public async Task GetOrCreateValueIds_WithoutKeyRangeLocks_ConcurrentlyForTheSameNewKey_StoresOneRowAndAgreesOnItsId()
    {
        // With no lock hints, the racing writers all pass NOT EXISTS. The unique index lets one insert through, and the
        // others fail it and run the insert again, which then finds the key. Only the retry stands between this and a
        // failed call.
        var value = NewValue();
        var repositories = Enumerable.Range(0, 8).Select(_ => NewRepository()).ToList();

        var results = await Task.WhenAll(repositories.Select(r => r.GetOrCreateValueIds([value], SubjectGenotypeSetValueInsertMode.RetryOnDuplicate)));

        (await StoredValueCount()).Should().Be(1);
        results.Select(ids => ids[value.Key]).Distinct().Should().ContainSingle();
    }

    [Test]
    public async Task GetOrCreateValueIds_WithoutKeyRangeLocks_ConcurrentlyForOverlappingChunks_StoresEachKeyOnce()
    {
        // Eight writers of the same 50 new keys, each in its own order: every writer but the first loses races, and all
        // must still agree on every id.
        var values = Enumerable.Range(0, 50).Select(_ => NewValue()).ToList();
        var repositories = Enumerable.Range(0, 8).Select(_ => NewRepository()).ToList();

        var results = await Task.WhenAll(repositories.Select((r, i) =>
            r.GetOrCreateValueIds(values.Skip(i * 5).Concat(values.Take(i * 5)).ToList(), SubjectGenotypeSetValueInsertMode.RetryOnDuplicate)));

        (await StoredValueCount()).Should().Be(values.Count);
        foreach (var value in values)
        {
            results.Select(ids => ids[value.Key]).Distinct().Should().ContainSingle();
        }
    }

    [Test]
    public async Task GetOrCreateValueIds_WithoutKeyRangeLocks_AcrossMoreThanOneChunk_StoresEachKeyOnce()
    {
        var values = Enumerable.Range(0, SubjectGenotypeSetRepository.StagingChunkSize + 5).Select(_ => NewValue()).ToList();
        var firstIds = await repository.GetOrCreateValueIds(values, SubjectGenotypeSetValueInsertMode.RetryOnDuplicate);

        var extraValues = Enumerable.Range(0, 3).Select(_ => NewValue()).ToList();
        var secondIds = await repository.GetOrCreateValueIds([..values, ..extraValues], SubjectGenotypeSetValueInsertMode.RetryOnDuplicate);

        (await StoredValueCount()).Should().Be(values.Count + extraValues.Count);
        foreach (var (key, id) in firstIds)
        {
            secondIds[key].Should().Be(id);
        }
    }

    [Test]
    public async Task GetOrCreateValueIds_UnderKeyRangeLocks_WaitsForAnUncommittedInsertOfTheNextKey()
    {
        // The locked probe of a new key locks the gap of the index up to the next key. Another writer's uncommitted
        // insert of that next key holds it, so the probe waits: the LCK_M_RS_U waits of ATL-473.
        var (newKey, nextKey) = AdjacentKeys();
        await using var blocker = await BeginUncommittedInsert(nextKey);

        var write = repository.GetOrCreateValueIds([NewValue(newKey)]);
        var finishedFirst = await Task.WhenAny(write, Task.Delay(TimeSpan.FromSeconds(3)));
        await blocker.Transaction.RollbackAsync();

        finishedFirst.Should().NotBeSameAs(write);
        (await write).Should().ContainKey(newKey);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task GetOrCreateValueIds_WithoutKeyRangeLocks_DoesNotWaitForAnUncommittedInsertOfTheNextKey(bool poolLeftAtSerializable)
    {
        // The data refresh stage writes thousands of new keys at once, and must not wait for each other's neighbours.
        // A pooled connection can keep SERIALIZABLE from its last user, a TransactionScope say, and then the probe takes
        // range locks with no hint. This test failed that way, now and then, when the donor import tests ran first.
        if (poolLeftAtSerializable)
        {
            await LeavePooledConnectionsAtSerializable();
        }

        var (newKey, nextKey) = AdjacentKeys();
        await using var blocker = await BeginUncommittedInsert(nextKey);

        var write = repository.GetOrCreateValueIds([NewValue(newKey)], SubjectGenotypeSetValueInsertMode.RetryOnDuplicate);
        var finishedFirst = await Task.WhenAny(write, Task.Delay(TimeSpan.FromSeconds(10)));
        await blocker.Transaction.RollbackAsync();

        finishedFirst.Should().BeSameAs(write);
        (await write).Should().ContainKey(newKey);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task GetExistingValueIds_DoesNotWaitForAnUncommittedInsertOfTheNextKey(bool poolLeftAtSerializable)
    {
        // The data refresh stage reads its keys before it writes them, on its own connection. That read must not wait
        // for another writer's neighbour either, also on a pooled connection left at SERIALIZABLE.
        if (poolLeftAtSerializable)
        {
            await LeavePooledConnectionsAtSerializable();
        }

        var (newKey, nextKey) = AdjacentKeys();
        await using var blocker = await BeginUncommittedInsert(nextKey);

        var read = repository.GetExistingValueIds([newKey]);
        var finishedFirst = await Task.WhenAny(read, Task.Delay(TimeSpan.FromSeconds(10)));
        await blocker.Transaction.RollbackAsync();

        finishedFirst.Should().BeSameAs(read);
        (await read).Should().BeEmpty();
    }

    [Test]
    public async Task GetOrCreateValueIds_WithNoValues_StoresNothing()
    {
        var ids = await repository.GetOrCreateValueIds([]);

        ids.Should().BeEmpty();
        (await StoredValueCount()).Should().Be(0);
    }

    [Test]
    public async Task GetExistingValueIds_ReturnsOnlyTheKeysThatAreStored()
    {
        var stored = NewValue();
        var storedIds = await repository.GetOrCreateValueIds([stored]);
        var absentKey = NewKey();

        var ids = await repository.GetExistingValueIds([stored.Key, absentKey]);

        ids.Should().HaveCount(1);
        ids[stored.Key].Should().Be(storedIds[stored.Key]);
    }

    [Test]
    public async Task GetExistingValueIds_StoresNothing()
    {
        // It stages the keys it probes, so the one thing it must not do is let those staged rows reach the real table.
        var ids = await repository.GetExistingValueIds([NewKey(), NewKey()]);

        ids.Should().BeEmpty();
        (await StoredValueCount()).Should().Be(0);
    }

    [Test]
    public async Task GetExistingValueIds_WithNoKeys_ReturnsEmpty()
    {
        var ids = await repository.GetExistingValueIds([]);

        ids.Should().BeEmpty();
    }

    [Test]
    public async Task UpsertDonorAssignments_ForNewAssignments_WritesOneRowPerDonorAndCombination()
    {
        var assignments = AssignmentsFor([1, 2], fixture.Create<int>());

        await repository.UpsertDonorAssignments(assignments);

        (await StoredAssignments()).Should().BeEquivalentTo(AsRows(assignments));
    }

    [Test]
    public async Task UpsertDonorAssignments_WithNoAssignments_DoesNotThrow()
    {
        var act = () => repository.UpsertDonorAssignments([]);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task UpsertDonorAssignments_WithTheSameAssignmentsTwice_DoesNotThrowAndKeepsOneRowEach()
    {
        // A continued refresh, a message delivered twice, a failed batch sent again: each sends rows already stored.
        var assignments = AssignmentsFor([1, 2], fixture.Create<int>());
        await repository.UpsertDonorAssignments(assignments);

        var act = () => NewRepository().UpsertDonorAssignments(assignments);

        await act.Should().NotThrowAsync();
        (await StoredAssignments()).Should().BeEquivalentTo(AsRows(assignments));
    }

    [Test]
    public async Task UpsertDonorAssignments_ForAnExistingPairWithADifferentValue_PointsItAtTheNewValue()
    {
        // A differential import of an updated donor: its rows exist, and must move to its new values.
        var oldValueId = fixture.Create<int>();
        var newValueId = oldValueId + 1;
        await repository.UpsertDonorAssignments(AssignmentsFor([1], oldValueId));

        await repository.UpsertDonorAssignments(AssignmentsFor([1], newValueId));

        (await StoredAssignments()).Should().BeEquivalentTo(AsRows(AssignmentsFor([1], newValueId)));
    }

    [Test]
    public async Task UpsertDonorAssignments_ForAMixOfNewUnchangedAndChangedRows_GivesEachRowItsValue()
    {
        var valueId = fixture.Create<int>();
        var unchanged = new DonorSubjectGenotypeSetAssignment(1, AllowedLociKey.ABCDrb1Dqb1, valueId);
        var toChange = new DonorSubjectGenotypeSetAssignment(2, AllowedLociKey.ABCDrb1Dqb1, valueId);
        await repository.UpsertDonorAssignments([unchanged, toChange]);

        var changed = toChange with { SubjectGenotypeSetValueId = valueId + 1 };
        var added = new DonorSubjectGenotypeSetAssignment(3, AllowedLociKey.ABDrb1, valueId + 2);
        await repository.UpsertDonorAssignments([unchanged, changed, added]);

        (await StoredAssignments()).Should().BeEquivalentTo(AsRows([unchanged, changed, added]));
    }

    [Test]
    public async Task UpsertDonorAssignments_LeavesOtherDonorsRowsAlone()
    {
        var otherDonor = AssignmentsFor([1], fixture.Create<int>());
        await repository.UpsertDonorAssignments(otherDonor);

        var donor = AssignmentsFor([2], fixture.Create<int>());
        await repository.UpsertDonorAssignments(donor);

        (await StoredAssignments()).Should().BeEquivalentTo(AsRows([..otherDonor, ..donor]));
    }

    [Test]
    public async Task UpsertDonorAssignments_WithTheSameAssignmentTwiceInOneCall_WritesOneRow()
    {
        // The staging table's primary key, and MERGE itself, both reject a pair staged twice - so exact repeats must
        // be removed before staging rather than surface as an unclear constraint error.
        var assignment = new DonorSubjectGenotypeSetAssignment(1, AllowedLociKey.ABCDrb1Dqb1, fixture.Create<int>());

        await repository.UpsertDonorAssignments([assignment, assignment]);

        (await StoredAssignments()).Should().BeEquivalentTo(AsRows([assignment]));
    }

    [Test]
    public async Task UpsertDonorAssignments_WithTwoValuesForOnePairInOneCall_ThrowsAndWritesNothing()
    {
        var valueId = fixture.Create<int>();
        var assignments = AssignmentsFor([1, 2], valueId);
        var conflicting = assignments[0] with { SubjectGenotypeSetValueId = valueId + 1 };

        var act = () => repository.UpsertDonorAssignments([..assignments, conflicting]);

        await act.Should().ThrowAsync<ArgumentException>();
        (await StoredAssignmentCount()).Should().Be(0);
    }

    [Test]
    public async Task UpsertDonorAssignments_ForMoreRowsThanAStagingChunk_WritesThemAll()
    {
        // A donor batch's worth: 2,000 donors x 4 combinations. The upsert is not chunked, so this pins that one
        // statement copes with a full batch.
        var assignments = AssignmentsFor(Enumerable.Range(1, 2_000).ToArray(), fixture.Create<int>());

        await repository.UpsertDonorAssignments(assignments);

        (await StoredAssignmentCount()).Should().Be(assignments.Count);
    }

    [Test]
    public async Task UpsertDonorAssignments_InsideAnAmbientTransactionThatRollsBack_KeepsNoRow()
    {
        // A caller that writes these rows inside its own scope must have a rollback there take them back out.
        using (new AsyncTransactionScope())
        {
            await repository.UpsertDonorAssignments(AssignmentsFor([1, 2], fixture.Create<int>()));
            // No Complete(): the scope rolls back on dispose.
        }

        (await StoredAssignmentCount()).Should().Be(0);
    }

    [Test]
    public async Task UpsertDonorAssignments_InsideAnAmbientTransactionWithAnotherWrite_KeepsBothWhenCompleted()
    {
        // Two writes over two connections in one scope. They must share the scope's transaction rather than fail by
        // needing a distributed one.
        var first = AssignmentsFor([1], fixture.Create<int>());
        var second = AssignmentsFor([2], fixture.Create<int>());

        using (var scope = new AsyncTransactionScope())
        {
            await repository.UpsertDonorAssignments(first);
            await NewRepository().UpsertDonorAssignments(second);
            scope.Complete();
        }

        (await StoredAssignments()).Should().BeEquivalentTo(AsRows([..first, ..second]));
    }

    [Test]
    public async Task DeleteDonorAssignments_DeletesEveryRowOfTheGivenDonors_AndLeavesOtherDonors()
    {
        var valueId = fixture.Create<int>();
        var kept = AssignmentsFor([3], valueId);
        await repository.UpsertDonorAssignments([..AssignmentsFor([1, 2], valueId), ..kept]);

        await repository.DeleteDonorAssignments([1, 2]);

        (await StoredAssignments()).Should().BeEquivalentTo(AsRows(kept));
    }

    [Test]
    public async Task DeleteDonorAssignments_LeavesTheValuesTheRowsPointedAt()
    {
        // Other donors with the same typing can point at the same value.
        var ids = await repository.GetOrCreateValueIds([NewValue()]);
        await repository.UpsertDonorAssignments(AssignmentsFor([1], ids.Values.Single()));

        await repository.DeleteDonorAssignments([1]);

        (await StoredAssignmentCount()).Should().Be(0);
        (await StoredValueCount()).Should().Be(1);
    }

    [Test]
    public async Task DeleteDonorAssignments_ForDonorsWithNoRows_OrNoDonors_DoesNotThrow()
    {
        var act = async () =>
        {
            await repository.DeleteDonorAssignments([42]);
            await repository.DeleteDonorAssignments([]);
        };

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task DeleteDonorAssignments_ForMoreDonorsThanOneStatementTakes_DeletesThemAll()
    {
        var donorIds = Enumerable.Range(1, 2500).ToArray();
        await repository.UpsertDonorAssignments(AssignmentsFor(donorIds, fixture.Create<int>()));

        await repository.DeleteDonorAssignments(donorIds);

        (await StoredAssignmentCount()).Should().Be(0);
    }

    [Test]
    public async Task DeleteDonorAssignments_InsideAnAmbientTransactionThatRollsBack_KeepsTheRows()
    {
        // The differential import deletes an updated donor's rows inside its donor transaction. If that transaction
        // rolls back, the donor keeps its old HLA, and must keep the rows that match it.
        var assignments = AssignmentsFor([1], fixture.Create<int>());
        await repository.UpsertDonorAssignments(assignments);

        using (new AsyncTransactionScope())
        {
            await repository.DeleteDonorAssignments([1]);
            // No Complete(): the scope rolls back on dispose.
        }

        (await StoredAssignments()).Should().BeEquivalentTo(AsRows(assignments));
    }

    [Test]
    public async Task DeleteDonorAssignments_InsideAnAmbientTransactionThatCompletes_DeletesTheRows()
    {
        await repository.UpsertDonorAssignments(AssignmentsFor([1], fixture.Create<int>()));

        using (var scope = new AsyncTransactionScope())
        {
            await repository.DeleteDonorAssignments([1]);
            scope.Complete();
        }

        (await StoredAssignmentCount()).Should().Be(0);
    }

    [Test]
    public async Task UpsertDonorAssignments_ConcurrentlyForTheSameDonor_DoesNotThrowAndKeepsOneRowEach()
    {
        // Eight connections racing to insert the same rows that do not exist yet. With row locks only (HOLDLOCK), they
        // deadlocked in 4 of 10 fixture runs and passed only because of the retry; the table lock makes them wait for
        // each other instead. This asserts the outcome; the absence of deadlocks is checked in the system_health session.
        var assignments = AssignmentsFor([1], fixture.Create<int>());
        var repositories = Enumerable.Range(0, 8).Select(_ => NewRepository()).ToList();

        var act = () => Task.WhenAll(repositories.Select(r => r.UpsertDonorAssignments(assignments)));

        await act.Should().NotThrowAsync();
        (await StoredAssignments()).Should().BeEquivalentTo(AsRows(assignments));
    }

    // ATL-495 experiment only: the insert that the donor genotype precomputation stage uses instead of the upsert.

    [Test]
    public async Task InsertMissingDonorAssignments_ForNewAssignments_WritesOneRowPerDonorAndCombination()
    {
        var assignments = AssignmentsFor([1, 2], fixture.Create<int>());

        await repository.InsertMissingDonorAssignments(assignments);

        (await StoredAssignments()).Should().BeEquivalentTo(AsRows(assignments));
    }

    [Test]
    public async Task InsertMissingDonorAssignments_WithTheSameAssignmentsTwice_DoesNotThrowAndKeepsOneRowEach()
    {
        var assignments = AssignmentsFor([1, 2], fixture.Create<int>());
        await repository.InsertMissingDonorAssignments(assignments);

        var act = () => NewRepository().InsertMissingDonorAssignments(assignments);

        await act.Should().NotThrowAsync();
        (await StoredAssignments()).Should().BeEquivalentTo(AsRows(assignments));
    }

    [Test]
    public async Task InsertMissingDonorAssignments_ForAnExistingPairWithADifferentValue_KeepsTheExistingRow()
    {
        var valueId = fixture.Create<int>();
        var existing = AssignmentsFor([1], valueId);
        await repository.InsertMissingDonorAssignments(existing);

        await repository.InsertMissingDonorAssignments([..AssignmentsFor([1], valueId + 1), ..AssignmentsFor([2], valueId)]);

        (await StoredAssignments()).Should().BeEquivalentTo(AsRows([..existing, ..AssignmentsFor([2], valueId)]));
    }

    [Test]
    public async Task InsertMissingDonorAssignments_ForMoreRowsThanOneStatement_WritesThemAll()
    {
        // A full chunk of the stage: 2,000 donors x 4 combinations, two insert statements.
        var assignments = AssignmentsFor(Enumerable.Range(1, 2_000).ToArray(), fixture.Create<int>());

        await repository.InsertMissingDonorAssignments(assignments);

        (await StoredAssignmentCount()).Should().Be(assignments.Count);
    }

    [Test]
    public async Task InsertMissingDonorAssignments_ConcurrentlyForDifferentDonors_WritesThemAll()
    {
        // The normal case of the stage: many batches, each with its own donors, at the same time.
        var batches = Enumerable.Range(0, 8)
            .Select(i => AssignmentsFor(Enumerable.Range(i * 1_000 + 1, 1_000).ToArray(), fixture.Create<int>()))
            .ToList();

        await Task.WhenAll(batches.Select(batch => NewRepository().InsertMissingDonorAssignments(batch)));

        // Sorted and compared in order: BeEquivalentTo compares each row with every other, which takes hours for 32,000.
        (await StoredAssignments()).Order().Should().Equal(AsRows(batches.SelectMany(batch => batch)).Order());
    }

    [Test]
    public async Task InsertMissingDonorAssignments_ConcurrentlyForTheSameDonor_DoesNotThrowAndKeepsOneRowEach()
    {
        // A batch that runs twice at the same time, after a lost lease.
        var assignments = AssignmentsFor([1], fixture.Create<int>());

        var act = () => Task.WhenAll(Enumerable.Range(0, 2).Select(_ => NewRepository().InsertMissingDonorAssignments(assignments)));

        await act.Should().NotThrowAsync();
        (await StoredAssignments()).Should().BeEquivalentTo(AsRows(assignments));
    }

    private static List<DonorSubjectGenotypeSetAssignment> AssignmentsFor(int[] donorIds, int valueId) =>
        donorIds
            .SelectMany(donorId => AllowedLociKeyExtensions.All
                .Select(allowedLociKey => new DonorSubjectGenotypeSetAssignment(donorId, allowedLociKey, valueId)))
            .ToList();

    private static IEnumerable<(int, AllowedLociKey, int)> AsRows(IEnumerable<DonorSubjectGenotypeSetAssignment> assignments) =>
        assignments.Select(a => (a.DonorId, a.AllowedLociKey, a.SubjectGenotypeSetValueId));

    private async Task<List<(int, AllowedLociKey, int)>> StoredAssignments()
    {
        await using var context = new ContextFactory().Create(transientConnectionString);
        var rows = await context.DonorSubjectGenotypeSets.ToListAsync();
        return rows.Select(row => (row.DonorId, row.AllowedLociKey, row.SubjectGenotypeSetValueId)).ToList();
    }

    /// <summary>Two keys that sort next to each other in the unique index, with no key between them.</summary>
    private (SubjectGenotypeSetKey NewKey, SubjectGenotypeSetKey NextKey) AdjacentKeys()
    {
        var frequencySetId = fixture.Create<int>();
        return (
            new SubjectGenotypeSetKey("7" + new string('f', 63), frequencySetId, AllowedLociKey.ABCDrb1Dqb1),
            new SubjectGenotypeSetKey("8" + new string('0', 63), frequencySetId, AllowedLociKey.ABCDrb1Dqb1));
    }

    /// <summary>
    /// Returns connections to the pool at <c>SERIALIZABLE</c>, as a <c>TransactionScope</c> does: the pool does not reset
    /// the isolation level of a connection that it hands out again.
    /// </summary>
    private async Task LeavePooledConnectionsAtSerializable()
    {
        var connections = Enumerable.Range(0, 4).Select(_ => new SqlConnection(transientConnectionString)).ToList();
        foreach (var connection in connections)
        {
            await connection.OpenAsync();
            await connection.ExecuteAsync("SET TRANSACTION ISOLATION LEVEL SERIALIZABLE");
        }

        foreach (var connection in connections)
        {
            await connection.DisposeAsync();
        }
    }

    /// <summary>Inserts a value row in a transaction that stays open, as a concurrent writer that has not committed yet.</summary>
    private async Task<OpenTransaction> BeginUncommittedInsert(SubjectGenotypeSetKey key)
    {
        var connection = new SqlConnection(transientConnectionString);
        await connection.OpenAsync();
        var transaction = (SqlTransaction) await connection.BeginTransactionAsync();
        await connection.ExecuteAsync(
            """
            INSERT INTO SubjectGenotypeSetValues (HlaTypingKey, HaplotypeFrequencySetId, AllowedLociKey, IsUnrepresented, SubjectGenotypeSetData)
            VALUES (@HlaTypingKey, @HaplotypeFrequencySetId, @AllowedLociKey, 1, NULL)
            """,
            new { key.HlaTypingKey, key.HaplotypeFrequencySetId, AllowedLociKey = key.AllowedLociKey.ToString() },
            transaction);
        return new OpenTransaction(connection, transaction);
    }

    private sealed record OpenTransaction(SqlConnection Connection, SqlTransaction Transaction) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Transaction.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }

    private static ISubjectGenotypeSetRepository NewRepository() =>
        Injection.Provider.GetService<IActiveRepositoryFactory>().GetSubjectGenotypeSetRepository();

    private SubjectGenotypeSetValueToStore NewValue(SubjectGenotypeSetKey? key = null) =>
        new(key ?? NewKey(), false, fixture.CreateMany<byte>(64).ToArray());

    private SubjectGenotypeSetKey NewKey(AllowedLociKey allowedLociKey = AllowedLociKey.ABCDrb1Dqb1) =>
        new(NewHlaTypingKey(), fixture.Create<int>(), allowedLociKey);

    /// <summary>A distinct lower-case hex digest, shaped as <c>SubjectGenotypeSetKeyGenerator</c> produces them.</summary>
    private static string NewHlaTypingKey() => Convert.ToHexStringLower(Guid.NewGuid().ToByteArray()) + Convert.ToHexStringLower(Guid.NewGuid().ToByteArray());

    private async Task<int> StoredValueCount()
    {
        await using var context = new ContextFactory().Create(transientConnectionString);
        return await context.SubjectGenotypeSetValues.CountAsync();
    }

    private async Task<int> StoredAssignmentCount()
    {
        await using var context = new ContextFactory().Create(transientConnectionString);
        return await context.DonorSubjectGenotypeSets.CountAsync();
    }

    private async Task<SubjectGenotypeSetValue> StoredValue(int id)
    {
        await using var context = new ContextFactory().Create(transientConnectionString);
        return await context.SubjectGenotypeSetValues.SingleAsync(value => value.Id == id);
    }
}
