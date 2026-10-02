using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using AwesomeAssertions;
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

    private string transientConnectionString;
    private ISubjectGenotypeSetRepository repository;
    private int nextHlaTypingKeySuffix;

    [SetUp]
    public void SetUp()
    {
        var connectionStringFactory = Injection.Provider.GetService<StaticallyChosenTransientSqlConnectionStringProviderFactory>();
        var connectionStringProvider = connectionStringFactory.GenerateConnectionStringProvider(TransientDatabase.DatabaseA);
        transientConnectionString = connectionStringProvider.GetConnectionString();
        repository = new SubjectGenotypeSetRepository(connectionStringProvider);

        DatabaseManager.ClearTransientDatabases();
    }

    [Test]
    public async Task GetDonorSubjectGenotypeSets_ReturnsTheStoredSetOfEachDonorAtTheGivenKeyOnly()
    {
        var valueIds = await repository.GetOrCreateValueIds([
            new SubjectGenotypeSetValueToStore(NewKey(frequencySetId: 7), false, [1, 2, 3]),
            new SubjectGenotypeSetValueToStore(NewKey(frequencySetId: 8), true, null),
            new SubjectGenotypeSetValueToStore(NewKey(frequencySetId: 9, allowedLociKey: AllowedLociKey.ABDrb1), false, [4]),
        ]);
        var ids = valueIds.ToDictionary(kv => kv.Key.HaplotypeFrequencySetId, kv => kv.Value);
        await repository.UpsertDonorAssignments([
            new DonorSubjectGenotypeSetAssignment(1, Key, ids[7]),
            new DonorSubjectGenotypeSetAssignment(2, Key, ids[8]),
            new DonorSubjectGenotypeSetAssignment(3, AllowedLociKey.ABDrb1, ids[9]),
        ]);

        var stored = await repository.GetDonorSubjectGenotypeSets([1, 2, 3, 4], Key);

        stored.Keys.Should().BeEquivalentTo([1, 2]);
        stored[1].HaplotypeFrequencySetId.Should().Be(7);
        stored[1].IsUnrepresented.Should().BeFalse();
        stored[1].SubjectGenotypeSetData.Should().Equal(1, 2, 3);
        stored[2].IsUnrepresented.Should().BeTrue();
        stored[2].SubjectGenotypeSetData.Should().BeNull();
    }

    [Test]
    public async Task GetDonorSubjectGenotypeSets_ForMoreDonorsThanOneStatementTakes_ReturnsThemAll()
    {
        var valueId = (await repository.GetOrCreateValueIds([new SubjectGenotypeSetValueToStore(NewKey(), false, [1])])).Single().Value;
        var donorIds = Enumerable.Range(1, 4500).ToList();
        await repository.UpsertDonorAssignments(donorIds.Select(id => new DonorSubjectGenotypeSetAssignment(id, Key, valueId)).ToList());

        var stored = await repository.GetDonorSubjectGenotypeSets(donorIds, Key);

        stored.Should().HaveCount(donorIds.Count);
    }

    [Test]
    public async Task GetDonorSubjectGenotypeSets_ForNoDonors_ReturnsNothing()
    {
        (await repository.GetDonorSubjectGenotypeSets([], Key)).Should().BeEmpty();
    }

    [Test]
    public async Task UpsertDonorAssignmentsWhereTypingUnchanged_ForDonorsWhoseTypingIsUnchanged_WritesTheirAssignments()
    {
        var typing = Typing("a");
        await InsertDonors((1, typing), (2, typing));
        var valueId = await NewValueId();

        var result = await repository.UpsertDonorAssignmentsWhereTypingUnchanged([Guarded(1, valueId, typing), Guarded(2, valueId, typing)]);

        result.UpsertedCount.Should().Be(2);
        result.SkippedTypingChangedCount.Should().Be(0);
        (await StoredAssignments()).Should().BeEquivalentTo([(1, Key, valueId), (2, Key, valueId)]);
    }

    [Test]
    public async Task UpsertDonorAssignmentsWhereTypingUnchanged_SkipsADonorWhoseTypingChanged_AndADonorThatIsGone()
    {
        await InsertDonors((1, Typing("now")), (2, Typing("a")));
        var valueId = await NewValueId();

        var result = await repository.UpsertDonorAssignmentsWhereTypingUnchanged([
            Guarded(1, valueId, Typing("before")),
            Guarded(2, valueId, Typing("a")),
            Guarded(3, valueId, Typing("a")),
        ]);

        result.UpsertedCount.Should().Be(1);
        result.SkippedTypingChangedCount.Should().Be(2);
        (await StoredAssignments()).Should().BeEquivalentTo([(2, Key, valueId)]);
    }

    [Test]
    public async Task UpsertDonorAssignmentsWhereTypingUnchanged_TreatsNullAndEmptyAsTheSameTyping()
    {
        await InsertDonors((1, Typing("a").SetPosition(Locus.Dqb1, LocusPosition.One, null)));
        var valueId = await NewValueId();

        var result = await repository.UpsertDonorAssignmentsWhereTypingUnchanged([
            Guarded(1, valueId, Typing("a").SetPosition(Locus.Dqb1, LocusPosition.One, string.Empty))
        ]);

        result.UpsertedCount.Should().Be(1);
    }

    [Test]
    public async Task UpsertDonorAssignmentsWhereTypingUnchanged_IgnoresADpb1Change()
    {
        // DPB1 is not a match prediction locus, so it does not change the genotype set.
        await InsertDonors((1, Typing("a").SetPosition(Locus.Dpb1, LocusPosition.One, "DPB1*NOW")));
        var valueId = await NewValueId();

        var result = await repository.UpsertDonorAssignmentsWhereTypingUnchanged([
            Guarded(1, valueId, Typing("a").SetPosition(Locus.Dpb1, LocusPosition.One, "DPB1*BEFORE"))
        ]);

        result.UpsertedCount.Should().Be(1);
    }

    [Test]
    public async Task UpsertDonorAssignmentsWhereTypingUnchanged_ReplacesAStaleAssignment_AndIsSafeToRepeat()
    {
        var typing = Typing("a");
        await InsertDonors((1, typing));
        var staleValueId = await NewValueId();
        var freshValueId = await NewValueId();
        await repository.UpsertDonorAssignments([new DonorSubjectGenotypeSetAssignment(1, Key, staleValueId)]);

        await repository.UpsertDonorAssignmentsWhereTypingUnchanged([Guarded(1, freshValueId, typing)]);
        await repository.UpsertDonorAssignmentsWhereTypingUnchanged([Guarded(1, freshValueId, typing)]);

        (await StoredAssignments()).Should().BeEquivalentTo([(1, Key, freshValueId)]);
    }

    [Test]
    public async Task UpsertDonorAssignmentsWhereTypingUnchanged_ForNoAssignments_DoesNothing()
    {
        var result = await repository.UpsertDonorAssignmentsWhereTypingUnchanged([]);

        result.Should().Be(new TypingGuardedUpsertResult(0, 0));
    }

    private static TypingGuardedDonorAssignment Guarded(int donorId, int valueId, PhenotypeInfo<string> typing) =>
        new(new DonorSubjectGenotypeSetAssignment(donorId, Key, valueId), typing);

    /// <summary>A typing with a distinct name at every match prediction locus, and at DPB1.</summary>
    private static PhenotypeInfo<string> Typing(string prefix) =>
        new((locus, position) => $"{locus.ToString().ToUpperInvariant()}*{prefix}-{position}");

    private async Task<int> NewValueId() =>
        (await repository.GetOrCreateValueIds([new SubjectGenotypeSetValueToStore(NewKey(), false, [1])])).Single().Value;

    private SubjectGenotypeSetKey NewKey(int frequencySetId = 1, AllowedLociKey allowedLociKey = Key) =>
        new($"{++nextHlaTypingKeySuffix:D64}", frequencySetId, allowedLociKey);

    private async Task InsertDonors(params (int DonorId, PhenotypeInfo<string> Typing)[] donors)
    {
        await using var context = new ContextFactory().Create(transientConnectionString);
        context.Donors.AddRange(donors.Select(d => new Donor
        {
            DonorId = d.DonorId,
            DonorType = DonorType.Adult,
            IsAvailableForSearch = true,
            ExternalDonorCode = $"donor-{d.DonorId}",
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
