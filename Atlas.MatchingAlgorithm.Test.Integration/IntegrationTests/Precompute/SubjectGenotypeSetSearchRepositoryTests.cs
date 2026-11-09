using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using AutoFixture;
using AwesomeAssertions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ContextFactory = Atlas.MatchingAlgorithm.Data.Context.ContextFactory;
using DonorType = Atlas.MatchingAlgorithm.Client.Models.Donors.DonorType;
using Injection = Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection.DependencyInjection;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Precompute;

/// <summary>
/// The repository methods search uses to read stored donor genotype sets and to store the ones it computed live (ATL-221).
/// </summary>
[TestFixture]
public class SubjectGenotypeSetSearchRepositoryTests
{
    private const AllowedLociKey Key = AllowedLociKey.ABCDrb1Dqb1;
    private const string DefaultRegistryCode = "reg";
    private const string DefaultEthnicityCode = "eth";

    private Fixture fixture;
    private string transientConnectionString;
    private ISubjectGenotypeSetRepository repository;
    private int nextHlaTypingKeySuffix;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        var connectionStringFactory = Injection.Provider.GetService<StaticallyChosenTransientSqlConnectionStringProviderFactory>();
        var connectionStringProvider = connectionStringFactory.GenerateConnectionStringProvider(TransientDatabase.DatabaseA);
        transientConnectionString = connectionStringProvider.GetConnectionString();
        repository = new SubjectGenotypeSetRepository(connectionStringProvider);

        DatabaseManager.ClearTransientDatabases();
    }

    /// <summary>
    /// Search reads a donor input's set (ATL-424) and a patient's set (ATL-426) by its key: no donor needs to point at it.
    /// The same typing with another frequency set or other loci is another key.
    /// </summary>
    [Test]
    public async Task GetSubjectGenotypeSetValues_ReturnsTheValueOfEachExactKey_WithNoDonorAssignment()
    {
        var key = NewKey(frequencySetId: fixture.Create<int>());
        var otherTypingKey = NewKey(frequencySetId: key.HaplotypeFrequencySetId);
        var otherSetKey = key with { HaplotypeFrequencySetId = key.HaplotypeFrequencySetId + 1 };
        var otherLociKey = key with { AllowedLociKey = AllowedLociKey.ABDrb1 };
        var values = new[] { key, otherTypingKey, otherSetKey, otherLociKey }.Select(NewValue).ToList();
        await repository.GetOrCreateValueIds(values);

        var stored = await repository.GetSubjectGenotypeSetValues([key, otherTypingKey]);

        stored.Keys.Should().BeEquivalentTo([key, otherTypingKey]);
        stored[key].IsUnrepresented.Should().BeFalse();
        stored[key].SubjectGenotypeSetData.Should().Equal(values[0].SubjectGenotypeSetData);
        stored[otherTypingKey].SubjectGenotypeSetData.Should().Equal(values[1].SubjectGenotypeSetData);
        (await StoredAssignments()).Should().BeEmpty();
    }

    [Test]
    public async Task GetSubjectGenotypeSetValues_ForAnUnrepresentedValue_ReturnsItWithNoData()
    {
        var key = NewKey();
        await repository.GetOrCreateValueIds([new SubjectGenotypeSetValueToStore(key, true, null)]);

        var stored = (await repository.GetSubjectGenotypeSetValues([key]))[key];

        stored.IsUnrepresented.Should().BeTrue();
        stored.SubjectGenotypeSetData.Should().BeNull();
    }

    [Test]
    public async Task GetSubjectGenotypeSetValues_LeavesOutAKeyWithNoValue()
    {
        var storedKey = NewKey(frequencySetId: fixture.Create<int>());
        await repository.GetOrCreateValueIds([NewValue(storedKey)]);
        // A new key: a different typing at the same frequency set.
        var missingKey = NewKey(frequencySetId: storedKey.HaplotypeFrequencySetId);

        var stored = await repository.GetSubjectGenotypeSetValues([storedKey, missingKey]);

        stored.Keys.Should().BeEquivalentTo([storedKey]);
    }

    /// <summary>A batch can ask for one key twice: for two donor inputs, or for a donor input and the patient.</summary>
    [Test]
    public async Task GetSubjectGenotypeSetValues_ForAKeyAskedForTwice_ReturnsItOnce()
    {
        var key = NewKey();
        await repository.GetOrCreateValueIds([NewValue(key)]);

        var stored = await repository.GetSubjectGenotypeSetValues([key, key]);

        stored.Keys.Should().BeEquivalentTo([key]);
    }

    [Test]
    public async Task GetSubjectGenotypeSetValues_ForMoreKeysThanOneChunkTakes_ReturnsThemAll()
    {
        var keys = Enumerable.Range(0, SubjectGenotypeSetRepository.StagingChunkSize + 5).Select(_ => NewKey()).ToList();
        await repository.GetOrCreateValueIds(keys.Select(NewValue).ToList());

        var stored = await repository.GetSubjectGenotypeSetValues(keys);

        stored.Keys.Should().BeEquivalentTo(keys);
    }

    [Test]
    public async Task GetSubjectGenotypeSetValues_ForNoKeys_ReturnsNothing()
    {
        (await repository.GetSubjectGenotypeSetValues([])).Should().BeEmpty();
    }

    /// <summary>
    /// Search reads the keys in one statement (ATL-531), and the writers compare them through their staging table. Both
    /// must agree about what "the same key" means: letter case (the collation), the frequency set and the loci.
    /// </summary>
    [Test]
    public async Task GetSubjectGenotypeSetValues_FindsTheSameKeysAsGetExistingValueIds()
    {
        var upperCaseKey = NewHexKey();
        var unchangedKey = NewHexKey();
        var otherSetKey = NewHexKey();
        var otherLociKey = NewHexKey();
        await repository.GetOrCreateValueIds([.. new[] { upperCaseKey, unchangedKey, otherSetKey, otherLociKey }.Select(NewValue)]);
        // Not a key and its upper-case form in one call: the writers' staging table rejects them as one key twice.
        SubjectGenotypeSetKey[] keys =
        [
            upperCaseKey with { HlaTypingKey = upperCaseKey.HlaTypingKey.ToUpperInvariant() },
            unchangedKey,
            otherSetKey with { HaplotypeFrequencySetId = otherSetKey.HaplotypeFrequencySetId + 1 },
            otherLociKey with { AllowedLociKey = AllowedLociKey.ABDrb1 },
            NewHexKey()
        ];

        var read = await repository.GetSubjectGenotypeSetValues(keys);
        var existing = await repository.GetExistingValueIds(keys);

        read.Keys.Should().BeEquivalentTo(existing.Keys);
        read.Keys.Should().Contain(unchangedKey);
    }

    /// <summary>
    /// DEV timing only (ATL-531): inside a timing scope, the read adds each of its steps and the SqlClient statistics of its
    /// connection, and still returns its values. The read in one statement has no staging steps, and one round trip.
    /// </summary>
    [Test]
    public async Task GetSubjectGenotypeSetValues_InATimingScope_AddsEachStepOfTheRead()
    {
        var key = NewKey();
        await repository.GetOrCreateValueIds([NewValue(key)]);

        IReadOnlyDictionary<string, StoredSubjectGenotypeSetValue> stored;
        IReadOnlyDictionary<string, double> steps;
        using (ReadStepTimings.Begin(out steps))
        {
            stored = (await repository.GetSubjectGenotypeSetValues([key])).ToDictionary(value => value.Key.HlaTypingKey, value => value.Value);
        }

        stored.Keys.Should().BeEquivalentTo(new[] { key.HlaTypingKey });
        steps.Keys.Should().Contain(new[]
        {
            "ReadOpenMs", "ReadNewConnection", "ReadFirstRowMs", "ReadRestRowsMs", "ReadCloseMs", "ReadServerRoundtrips",
            "ReadNetworkServerMs", "ReadSqlClientExecutionMs", "ReadBytesReceived"
        });
        steps.Keys.Should().NotContain(new[] { "ReadIsolationMs", "ReadCreateTableMs", "ReadBulkCopyMs" });
        steps["ReadServerRoundtrips"].Should().Be(1);
        foreach (var (step, value) in steps)
        {
            await TestContext.Out.WriteLineAsync($"{step} = {value}");
        }
    }

    /// <summary>
    /// The warm step may run twice for one search (a retried activity), or for two searches of the same patient at once.
    /// Both must agree on one row, and the second must not rewrite the first.
    /// </summary>
    [Test]
    public async Task GetOrCreateValueIds_ForAPatientSetStoredTwice_KeepsOneRowAndTheFirstPayload()
    {
        var key = NewKey();
        var firstId = (await repository.GetOrCreateValueIds([new SubjectGenotypeSetValueToStore(key, false, [1])])).Single().Value;
        var secondId = (await repository.GetOrCreateValueIds([new SubjectGenotypeSetValueToStore(key, false, [2])])).Single().Value;

        secondId.Should().Be(firstId);
        (await repository.GetSubjectGenotypeSetValues([key]))[key].SubjectGenotypeSetData.Should().Equal(1);
        (await StoredAssignments()).Should().BeEmpty();
    }

    [Test]
    public async Task UpsertDonorAssignmentsWhereDonorUnchanged_ForDonorsWhoseTypingIsUnchanged_WritesTheirAssignments()
    {
        var typing = Typing("a");
        await InsertDonors((1, typing), (2, typing));
        var valueId = await NewValueId();

        var result = await repository.UpsertDonorAssignmentsWhereDonorUnchanged([Guarded(1, valueId, typing), Guarded(2, valueId, typing)]);

        result.UpsertedCount.Should().Be(2);
        result.SkippedDonorChangedCount.Should().Be(0);
        (await StoredAssignments()).Should().BeEquivalentTo([(1, Key, valueId), (2, Key, valueId)]);
    }

    [Test]
    public async Task UpsertDonorAssignmentsWhereDonorUnchanged_SkipsADonorWhoseTypingChanged_AndADonorThatIsGone()
    {
        await InsertDonors((1, Typing("now")), (2, Typing("a")));
        var valueId = await NewValueId();

        var result = await repository.UpsertDonorAssignmentsWhereDonorUnchanged([
            Guarded(1, valueId, Typing("before")),
            Guarded(2, valueId, Typing("a")),
            Guarded(3, valueId, Typing("a")),
        ]);

        result.UpsertedCount.Should().Be(1);
        result.SkippedDonorChangedCount.Should().Be(2);
        (await StoredAssignments()).Should().BeEquivalentTo([(2, Key, valueId)]);
    }

    [Test]
    public async Task UpsertDonorAssignmentsWhereDonorUnchanged_TreatsNullAndEmptyAsTheSameTyping()
    {
        await InsertDonors((1, Typing("a").SetPosition(Locus.Dqb1, LocusPosition.One, null)));
        var valueId = await NewValueId();

        var result = await repository.UpsertDonorAssignmentsWhereDonorUnchanged([
            Guarded(1, valueId, Typing("a").SetPosition(Locus.Dqb1, LocusPosition.One, string.Empty))
        ]);

        result.UpsertedCount.Should().Be(1);
    }

    [Test]
    public async Task UpsertDonorAssignmentsWhereDonorUnchanged_IgnoresADpb1Change()
    {
        // DPB1 is not a match prediction locus, so it does not change the genotype set.
        await InsertDonors((1, Typing("a").SetPosition(Locus.Dpb1, LocusPosition.One, "DPB1*NOW")));
        var valueId = await NewValueId();

        var result = await repository.UpsertDonorAssignmentsWhereDonorUnchanged([
            Guarded(1, valueId, Typing("a").SetPosition(Locus.Dpb1, LocusPosition.One, "DPB1*BEFORE"))
        ]);

        result.UpsertedCount.Should().Be(1);
    }

    [Test]
    public async Task UpsertDonorAssignmentsWhereDonorUnchanged_ReplacesAStaleAssignment_AndIsSafeToRepeat()
    {
        var typing = Typing("a");
        await InsertDonors((1, typing));
        var staleValueId = await NewValueId();
        var freshValueId = await NewValueId();
        await repository.UpsertDonorAssignments([new DonorSubjectGenotypeSetAssignment(1, Key, staleValueId)]);

        await repository.UpsertDonorAssignmentsWhereDonorUnchanged([Guarded(1, freshValueId, typing)]);
        await repository.UpsertDonorAssignmentsWhereDonorUnchanged([Guarded(1, freshValueId, typing)]);

        (await StoredAssignments()).Should().BeEquivalentTo([(1, Key, freshValueId)]);
    }

    /// <summary>
    /// The registry and ethnicity codes choose the frequency set. A donor whose codes changed after matching must not be
    /// pointed back at a value computed with its old frequency set, over the import's correct row.
    /// </summary>
    [Test]
    public async Task UpsertDonorAssignmentsWhereDonorUnchanged_SkipsADonorWhoseRegistryOrEthnicityCodeChanged()
    {
        var typing = Typing("a");
        await InsertDonors((1, typing, "reg-now", "eth"), (2, typing, "reg", "eth-now"), (3, typing, "reg", "eth"));
        var valueId = await NewValueId();

        var result = await repository.UpsertDonorAssignmentsWhereDonorUnchanged([
            Guarded(1, valueId, typing, "reg-before", "eth"),
            Guarded(2, valueId, typing, "reg", "eth-before"),
            Guarded(3, valueId, typing, "reg", "eth"),
        ]);

        result.UpsertedCount.Should().Be(1);
        result.SkippedDonorChangedCount.Should().Be(2);
        (await StoredAssignments()).Should().BeEquivalentTo([(3, Key, valueId)]);
    }

    [Test]
    public async Task UpsertDonorAssignmentsWhereDonorUnchanged_TreatsNullAndEmptyCodesAsTheSame()
    {
        var typing = Typing("a");
        await InsertDonors((1, typing, "reg", null));
        var valueId = await NewValueId();

        var result = await repository.UpsertDonorAssignmentsWhereDonorUnchanged([Guarded(1, valueId, typing, "reg", string.Empty)]);

        result.UpsertedCount.Should().Be(1);
    }

    /// <summary>
    /// A best-effort store must not wait for minutes behind another writer: with the session's short lock timeout, a
    /// held table lock makes it fail fast (error 1222), and the caller logs and skips the store.
    /// </summary>
    [Test]
    public async Task UpsertDonorAssignmentsWhereDonorUnchanged_WhenTheTableIsLockedByAnotherTransaction_FailsFastWithALockTimeout()
    {
        var typing = Typing("a");
        await InsertDonors((1, typing));
        var valueId = await NewValueId();

        await using var blocker = new SqlConnection(transientConnectionString);
        await blocker.OpenAsync();
        await using var blockingTransaction = (SqlTransaction) await blocker.BeginTransactionAsync();
        await blocker.ExecuteAsync("SELECT TOP (1) 1 FROM DonorSubjectGenotypeSets WITH (TABLOCKX, HOLDLOCK)", transaction: blockingTransaction);

        var stopwatch = Stopwatch.StartNew();
        var act = () => repository.UpsertDonorAssignmentsWhereDonorUnchanged([Guarded(1, valueId, typing)]);

        (await act.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(1222);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(30));

        await blockingTransaction.RollbackAsync();
    }

    [Test]
    public async Task UpsertDonorAssignmentsWhereDonorUnchanged_ForNoAssignments_DoesNothing()
    {
        var result = await repository.UpsertDonorAssignmentsWhereDonorUnchanged([]);

        result.Should().Be(new GuardedUpsertResult(0, 0));
    }

    private static GuardedDonorAssignment Guarded(
        int donorId,
        int valueId,
        PhenotypeInfo<string> typing,
        string registryCode = DefaultRegistryCode,
        string ethnicityCode = DefaultEthnicityCode) =>
        new(new DonorSubjectGenotypeSetAssignment(donorId, Key, valueId), typing, registryCode, ethnicityCode);

    /// <summary>A typing with a distinct name at every match prediction locus, and at DPB1.</summary>
    private static PhenotypeInfo<string> Typing(string prefix) =>
        new((locus, position) => $"{locus.ToString().ToUpperInvariant()}*{prefix}-{position}");

    private async Task<int> NewValueId() =>
        (await repository.GetOrCreateValueIds([new SubjectGenotypeSetValueToStore(NewKey(), false, [1])])).Single().Value;

    private SubjectGenotypeSetKey NewKey(int frequencySetId = 1, AllowedLociKey allowedLociKey = Key) =>
        new($"{++nextHlaTypingKeySuffix:D64}", frequencySetId, allowedLociKey);

    /// <summary>A key whose typing key is like the generator's: 32 bytes as lower-case hex.</summary>
    private SubjectGenotypeSetKey NewHexKey() =>
        new(Convert.ToHexStringLower(fixture.CreateMany<byte>(32).ToArray()), fixture.Create<int>(), Key);

    private SubjectGenotypeSetValueToStore NewValue(SubjectGenotypeSetKey key) => new(key, false, fixture.CreateMany<byte>().ToArray());

    private Task InsertDonors(params (int DonorId, PhenotypeInfo<string> Typing)[] donors) =>
        InsertDonors(donors.Select(d => (d.DonorId, d.Typing, DefaultRegistryCode, DefaultEthnicityCode)).ToArray());

    private async Task InsertDonors(params (int DonorId, PhenotypeInfo<string> Typing, string RegistryCode, string EthnicityCode)[] donors)
    {
        await using var context = new ContextFactory().Create(transientConnectionString);
        context.Donors.AddRange(donors.Select(d => new Donor
        {
            DonorId = d.DonorId,
            DonorType = DonorType.Adult,
            IsAvailableForSearch = true,
            ExternalDonorCode = $"donor-{d.DonorId}",
            RegistryCode = d.RegistryCode,
            EthnicityCode = d.EthnicityCode,
            A_1 = d.Typing.A.Position1, A_2 = d.Typing.A.Position2,
            B_1 = d.Typing.B.Position1, B_2 = d.Typing.B.Position2,
            C_1 = d.Typing.C.Position1, C_2 = d.Typing.C.Position2,
            DPB1_1 = d.Typing.Dpb1.Position1, DPB1_2 = d.Typing.Dpb1.Position2,
            DQB1_1 = d.Typing.Dqb1.Position1, DQB1_2 = d.Typing.Dqb1.Position2,
            DRB1_1 = d.Typing.Drb1.Position1, DRB1_2 = d.Typing.Drb1.Position2,
        }));
        await context.SaveChangesAsync();
    }

    private async Task<List<(int, AllowedLociKey, int)>> StoredAssignments()
    {
        await using var context = new ContextFactory().Create(transientConnectionString);
        var rows = await context.DonorSubjectGenotypeSets.ToListAsync();
        return rows.Select(row => (row.DonorId, row.AllowedLociKey, row.SubjectGenotypeSetValueId)).ToList();
    }
}
