using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Services.Search.Precompute;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.Precompute;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Precompute;

[TestFixture]
internal class PrecomputedDonorGenotypeSetReaderTests
{
    private const int PinnedRecordId = 42;
    private static readonly IReadOnlySet<Locus> FiveLoci = new HashSet<Locus> { Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1 };
    private static readonly PhenotypeInfo<string> SearchTyping = new((locus, position) => $"{locus}*search-{position}");
    private static readonly PhenotypeInfo<string> NewTyping = new((locus, position) => $"{locus}*new-{position}");

    /// <summary>One donor input of two donors, with the typing matching read.</summary>
    private static readonly IReadOnlyCollection<DonorInput> Donors =
        [new() { DonorIds = [1, 2], DonorHla = SearchTyping.ToPhenotypeInfoTransfer() }];

    private static readonly string SearchTypingKey = SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(SearchTyping, AllowedLociKey.ABCDrb1Dqb1);

    private IDataRefreshHistoryRepository historyRepository;
    private ISubjectGenotypeSetRepositoryFactory repositoryFactory;
    private ISubjectGenotypeSetRepository repository;
    private IPrecomputedDonorGenotypeSetReader reader;

    [SetUp]
    public void SetUp()
    {
        historyRepository = Substitute.For<IDataRefreshHistoryRepository>();
        repositoryFactory = Substitute.For<ISubjectGenotypeSetRepositoryFactory>();
        repository = Substitute.For<ISubjectGenotypeSetRepository>();
        repositoryFactory.GetForDatabase(default).ReturnsForAnyArgs(repository);
        repository.GetDonorSubjectGenotypeSets(default, default).ReturnsForAnyArgs(new Dictionary<int, StoredDonorSubjectGenotypeSet>
        {
            { 1, new StoredDonorSubjectGenotypeSet(1, SearchTypingKey, 7, false, [1, 2, 3]) }
        });

        historyRepository.GetActiveRecord().Returns(new ActiveDataRefreshRecord(PinnedRecordId, TransientDatabase.DatabaseB, "3330"));

        reader = new PrecomputedDonorGenotypeSetReader(historyRepository, repositoryFactory);
    }

    [Test]
    public async Task GetDonorGenotypeSets_WhenPinnedRecordIsStillActive_ReadsTheRowsFromItsDatabase()
    {
        var lookup = await reader.GetDonorGenotypeSets(Donors, FiveLoci, PinnedRecordId);

        repositoryFactory.Received(1).GetForDatabase(TransientDatabase.DatabaseB);
        await repository.Received(1).GetDonorSubjectGenotypeSets(
            Arg.Is<IReadOnlyCollection<int>>(ids => ids.Order().SequenceEqual(new[] { 1, 2 })),
            AllowedLociKey.ABCDrb1Dqb1);
        lookup.BatchFallbackReason.Should().BeNull();
        lookup.AllowedLociKey.Should().Be("ABCDrb1Dqb1");
        lookup.Rows.Should().ContainKey(1);
        lookup.Rows[1].HaplotypeFrequencySetId.Should().Be(7);
        lookup.Rows[1].SubjectGenotypeSetData.Should().Equal(1, 2, 3);
        lookup.Rows[1].ComputedFromSearchTyping.Should().BeTrue();
    }

    /// <summary>
    /// The donor was updated after matching read it: its row now holds a set for its new typing. The row is returned,
    /// marked as not computed from the search's typing, so the caller computes the donor live instead of using it.
    /// </summary>
    [Test]
    public async Task GetDonorGenotypeSets_ForARowComputedFromAnotherTyping_MarksItNotComputedFromTheSearchTyping()
    {
        var newTypingKey = SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(NewTyping, AllowedLociKey.ABCDrb1Dqb1);
        repository.GetDonorSubjectGenotypeSets(default, default).ReturnsForAnyArgs(new Dictionary<int, StoredDonorSubjectGenotypeSet>
        {
            { 1, new StoredDonorSubjectGenotypeSet(1, newTypingKey, 7, false, [1]) },
            { 2, new StoredDonorSubjectGenotypeSet(2, SearchTypingKey, 7, false, [2]) }
        });

        var lookup = await reader.GetDonorGenotypeSets(Donors, FiveLoci, PinnedRecordId);

        lookup.Rows[1].ComputedFromSearchTyping.Should().BeFalse();
        lookup.Rows[2].ComputedFromSearchTyping.Should().BeTrue();
    }

    /// <summary>The key treats null and empty names as the same typing, as the pre-computation does when it writes it.</summary>
    [Test]
    public async Task GetDonorGenotypeSets_WhenTheSearchTypingHasEmptyNamesWhereTheStoredTypingHasNull_StillMatches()
    {
        var withNull = SearchTyping.SetPosition(Locus.C, LocusPosition.Two, null);
        var withEmpty = SearchTyping.SetPosition(Locus.C, LocusPosition.Two, string.Empty);
        repository.GetDonorSubjectGenotypeSets(default, default).ReturnsForAnyArgs(new Dictionary<int, StoredDonorSubjectGenotypeSet>
        {
            { 1, new StoredDonorSubjectGenotypeSet(1, SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(withNull, AllowedLociKey.ABCDrb1Dqb1), 7, false, [1]) }
        });

        var lookup = await reader.GetDonorGenotypeSets(
            [new DonorInput { DonorIds = [1], DonorHla = withEmpty.ToPhenotypeInfoTransfer() }],
            FiveLoci,
            PinnedRecordId);

        lookup.Rows[1].ComputedFromSearchTyping.Should().BeTrue();
    }

    [Test]
    public async Task GetDonorGenotypeSets_WhenAnotherRecordIsNowActive_ReturnsActiveDatabaseChangedAndNeverTouchesEitherDatabase()
    {
        historyRepository.GetActiveRecord().Returns(new ActiveDataRefreshRecord(PinnedRecordId + 1, TransientDatabase.DatabaseA, "3440"));

        var lookup = await reader.GetDonorGenotypeSets(Donors, FiveLoci, PinnedRecordId);

        lookup.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.ActiveDatabaseChanged);
        lookup.Rows.Should().BeEmpty();
        repositoryFactory.DidNotReceiveWithAnyArgs().GetForDatabase(default);
    }

    [Test]
    public async Task GetDonorGenotypeSets_WhenNoRefreshIsActive_ReturnsActiveDatabaseChanged()
    {
        historyRepository.GetActiveRecord().Returns((ActiveDataRefreshRecord) null);

        var lookup = await reader.GetDonorGenotypeSets(Donors, FiveLoci, PinnedRecordId);

        lookup.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.ActiveDatabaseChanged);
        repositoryFactory.DidNotReceiveWithAnyArgs().GetForDatabase(default);
    }

    [Test]
    public async Task GetDonorGenotypeSets_WithNoPinnedRecord_ReturnsActiveDatabaseUnknown()
    {
        var lookup = await reader.GetDonorGenotypeSets(Donors, FiveLoci, null);

        lookup.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.ActiveDatabaseUnknown);
        historyRepository.DidNotReceive().GetActiveRecord();
        repositoryFactory.DidNotReceiveWithAnyArgs().GetForDatabase(default);
    }

    [Test]
    public async Task GetDonorGenotypeSets_ForLociThatAreNotOneOfTheFourKeys_ReturnsUncoveredAllowedLoci()
    {
        var lookup = await reader.GetDonorGenotypeSets(Donors, new HashSet<Locus> { Locus.A, Locus.B, Locus.C }, PinnedRecordId);

        lookup.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.UncoveredAllowedLoci);
        lookup.AllowedLociKey.Should().BeNull();
        repositoryFactory.DidNotReceiveWithAnyArgs().GetForDatabase(default);
    }

    [Test]
    public async Task GetDonorGenotypeSets_ReadsTheActiveRecordFreshOnEveryCall()
    {
        await reader.GetDonorGenotypeSets(Donors, FiveLoci, PinnedRecordId);
        await reader.GetDonorGenotypeSets(Donors, FiveLoci, PinnedRecordId);

        historyRepository.Received(2).GetActiveRecord();
    }
}
