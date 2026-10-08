using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Services.Search.Precompute;
using Atlas.MatchPrediction.Services.Precompute;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Precompute;

[TestFixture]
internal class PrecomputedDonorGenotypeSetWriterTests
{
    private const int PinnedRecordId = 42;
    private const int FrequencySetId = 7;
    private static readonly IReadOnlySet<Locus> FiveLoci = new HashSet<Locus> { Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1 };

    private IDataRefreshHistoryRepository historyRepository;
    private ISubjectGenotypeSetRepositoryFactory repositoryFactory;
    private ISubjectGenotypeSetRepository repository;
    private IPrecomputedDonorGenotypeSetWriter writer;

    [SetUp]
    public void SetUp()
    {
        historyRepository = Substitute.For<IDataRefreshHistoryRepository>();
        repositoryFactory = Substitute.For<ISubjectGenotypeSetRepositoryFactory>();
        repository = Substitute.For<ISubjectGenotypeSetRepository>();
        repositoryFactory.GetForDatabase(default).ReturnsForAnyArgs(repository);

        historyRepository.GetActiveRecord().Returns(new ActiveDataRefreshRecord(PinnedRecordId, TransientDatabase.DatabaseA, "3330"));

        // Give each staged key a distinct id, in the order staged.
        repository.GetOrCreateValueIds(default).ReturnsForAnyArgs(call => (IReadOnlyDictionary<SubjectGenotypeSetKey, int>)
            call.Arg<IReadOnlyCollection<SubjectGenotypeSetValueToStore>>()
                .Select((value, index) => (value.Key, Id: 100 + index))
                .ToDictionary(x => x.Key, x => x.Id));
        repository.UpsertDonorAssignmentsWhereDonorUnchanged(default).ReturnsForAnyArgs(call =>
            new GuardedUpsertResult(call.Arg<IReadOnlyCollection<GuardedDonorAssignment>>().Count - 1, 1));

        writer = new PrecomputedDonorGenotypeSetWriter(historyRepository, repositoryFactory);
    }

    [Test]
    public async Task Store_WhenPinnedRecordIsStillActive_StoresValuesThenGuardedAssignmentsInItsDatabase()
    {
        var hla = new PhenotypeInfo<string>("hla");
        var sets = new List<DonorGenotypeSetToStore>
        {
            new([1, 2], hla, "reg", "eth", FrequencySetId, false, [9, 9]),
            new([3], new PhenotypeInfo<string>("other-hla"), "reg-3", null, FrequencySetId, true, null)
        };

        var result = await writer.Store(sets, FiveLoci, PinnedRecordId);

        repositoryFactory.Received(1).GetForDatabase(TransientDatabase.DatabaseA);

        var expectedKey = new SubjectGenotypeSetKey(
            SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(hla, AllowedLociKey.ABCDrb1Dqb1), FrequencySetId, AllowedLociKey.ABCDrb1Dqb1);
        await repository.Received(1).GetOrCreateValueIds(Arg.Is<IReadOnlyCollection<SubjectGenotypeSetValueToStore>>(values =>
            values.Count == 2
            && values.Any(v => v.Key == expectedKey && !v.IsUnrepresented && v.SubjectGenotypeSetData.SequenceEqual(new byte[] { 9, 9 }))
            && values.Any(v => v.IsUnrepresented && v.SubjectGenotypeSetData == null)));

        await repository.Received(1).UpsertDonorAssignmentsWhereDonorUnchanged(Arg.Is<IReadOnlyCollection<GuardedDonorAssignment>>(a =>
            a.Count == 3
            && a.Where(x => x.Assignment.DonorId == 1 || x.Assignment.DonorId == 2).All(x =>
                x.Assignment.SubjectGenotypeSetValueId == 100 && ReferenceEquals(x.ExpectedHla, hla)
                && x.ExpectedRegistryCode == "reg" && x.ExpectedEthnicityCode == "eth")
            && a.Single(x => x.Assignment.DonorId == 3).Assignment.SubjectGenotypeSetValueId == 101
            && a.Single(x => x.Assignment.DonorId == 3).ExpectedRegistryCode == "reg-3"
            && a.Single(x => x.Assignment.DonorId == 3).ExpectedEthnicityCode == null
            && a.All(x => x.Assignment.AllowedLociKey == AllowedLociKey.ABCDrb1Dqb1)));

        result.StoredCount.Should().Be(2);
        result.SkippedDonorChangedCount.Should().Be(1);
        result.SkippedDatabaseChangedCount.Should().Be(0);
    }

    [Test]
    public async Task Store_WhenAnotherRecordIsNowActive_WritesNothingAndCountsEveryDonorAsSkipped()
    {
        historyRepository.GetActiveRecord().Returns(new ActiveDataRefreshRecord(PinnedRecordId + 1, TransientDatabase.DatabaseB, "3440"));

        var result = await writer.Store([new([1, 2], new PhenotypeInfo<string>("hla"), "reg", "eth", FrequencySetId, false, [1])], FiveLoci, PinnedRecordId);

        repositoryFactory.DidNotReceiveWithAnyArgs().GetForDatabase(default);
        result.SkippedDatabaseChangedCount.Should().Be(2);
        result.StoredCount.Should().Be(0);
    }

    [Test]
    public async Task Store_TreatsEmptyHlaNamesAsNullWhenBuildingTheKey_SoItMatchesTheDataRefreshKey()
    {
        var withEmpty = new PhenotypeInfo<string>("hla").SetPosition(Locus.C, LocusPosition.Two, string.Empty);
        var withNull = new PhenotypeInfo<string>("hla").SetPosition(Locus.C, LocusPosition.Two, null);

        await writer.Store([new([1], withEmpty, "reg", "eth", FrequencySetId, false, [1])], FiveLoci, PinnedRecordId);

        var expectedTypingKey = SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(withNull, AllowedLociKey.ABCDrb1Dqb1);
        await repository.Received(1).GetOrCreateValueIds(Arg.Is<IReadOnlyCollection<SubjectGenotypeSetValueToStore>>(values =>
            values.Single().Key.HlaTypingKey == expectedTypingKey));
    }

    [Test]
    public async Task Store_ForLociThatAreNotOneOfTheFourKeys_WritesNothing()
    {
        var result = await writer.Store(
            [new([1], new PhenotypeInfo<string>("hla"), "reg", "eth", FrequencySetId, false, [1])],
            new HashSet<Locus> { Locus.A, Locus.B, Locus.C },
            PinnedRecordId);

        result.Should().Be(DonorGenotypeSetStoreResult.None);
        repositoryFactory.DidNotReceiveWithAnyArgs().GetForDatabase(default);
    }
}
