using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using AutoFixture;
using AwesomeAssertions;
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

    /// <summary>SQL Server's error for a duplicate key in a unique index, as against a primary key or a constraint.</summary>
    private const int DuplicateKeyInUniqueIndexErrorNumber = 2601;

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
    public async Task WriteDonorAssignments_WritesOneRowPerDonorAndCombination()
    {
        var valueId = fixture.Create<int>();
        var assignments = new[] { 1, 2 }
            .SelectMany(donorId => AllowedLociKeyExtensions.All
                .Select(allowedLociKey => new DonorSubjectGenotypeSetAssignment(donorId, allowedLociKey, valueId)))
            .ToList();

        await repository.WriteDonorAssignments(assignments);

        await using var context = new ContextFactory().Create(transientConnectionString);
        var written = await context.DonorSubjectGenotypeSets.ToListAsync();

        written.Should().HaveCount(8);
        written.Select(row => (row.DonorId, row.AllowedLociKey, row.SubjectGenotypeSetValueId))
            .Should().BeEquivalentTo(assignments.Select(a => (a.DonorId, a.AllowedLociKey, a.SubjectGenotypeSetValueId)));
    }

    [Test]
    public async Task WriteDonorAssignments_WithNoAssignments_DoesNotThrow()
    {
        var act = () => repository.WriteDonorAssignments([]);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task WriteDonorAssignments_WhenTheUniqueIndexRejectsARow_KeepsNoRowFromTheCall()
    {
        // The rejected row is the last one, after more rows than a StagingChunkSize batch holds. A write committed
        // batch by batch would keep the rows before it, and the same assignments sent again would collide with them.
        var valueId = fixture.Create<int>();
        var assignments = Enumerable.Range(1, SubjectGenotypeSetRepository.StagingChunkSize)
            .SelectMany(donorId => AllowedLociKeyExtensions.All
                .Select(allowedLociKey => new DonorSubjectGenotypeSetAssignment(donorId, allowedLociKey, valueId)))
            .ToList();

        var act = () => repository.WriteDonorAssignments([..assignments, assignments[0]]);

        await act.Should().ThrowAsync<SqlException>().Where(exception => exception.Number == DuplicateKeyInUniqueIndexErrorNumber);
        (await StoredAssignmentCount()).Should().Be(0);
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
