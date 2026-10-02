using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.Search.Precompute;
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
    private static readonly IReadOnlyCollection<int> DonorIds = [1, 2];

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
            { 1, new StoredDonorSubjectGenotypeSet(1, 7, false, [1, 2, 3]) }
        });

        historyRepository.GetActiveRecord().Returns(new ActiveDataRefreshRecord(PinnedRecordId, TransientDatabase.DatabaseB, "3330"));

        reader = new PrecomputedDonorGenotypeSetReader(historyRepository, repositoryFactory);
    }

    [Test]
    public async Task GetDonorGenotypeSets_WhenPinnedRecordIsStillActive_ReadsTheRowsFromItsDatabase()
    {
        var lookup = await reader.GetDonorGenotypeSets(DonorIds, FiveLoci, PinnedRecordId);

        repositoryFactory.Received(1).GetForDatabase(TransientDatabase.DatabaseB);
        await repository.Received(1).GetDonorSubjectGenotypeSets(DonorIds, AllowedLociKey.ABCDrb1Dqb1);
        lookup.BatchFallbackReason.Should().BeNull();
        lookup.AllowedLociKey.Should().Be("ABCDrb1Dqb1");
        lookup.Rows.Should().ContainKey(1);
        lookup.Rows[1].HaplotypeFrequencySetId.Should().Be(7);
        lookup.Rows[1].SubjectGenotypeSetData.Should().Equal(1, 2, 3);
    }

    [Test]
    public async Task GetDonorGenotypeSets_WhenAnotherRecordIsNowActive_ReturnsActiveDatabaseChangedAndNeverTouchesEitherDatabase()
    {
        historyRepository.GetActiveRecord().Returns(new ActiveDataRefreshRecord(PinnedRecordId + 1, TransientDatabase.DatabaseA, "3440"));

        var lookup = await reader.GetDonorGenotypeSets(DonorIds, FiveLoci, PinnedRecordId);

        lookup.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.ActiveDatabaseChanged);
        lookup.Rows.Should().BeEmpty();
        repositoryFactory.DidNotReceiveWithAnyArgs().GetForDatabase(default);
    }

    [Test]
    public async Task GetDonorGenotypeSets_WhenNoRefreshIsActive_ReturnsActiveDatabaseChanged()
    {
        historyRepository.GetActiveRecord().Returns((ActiveDataRefreshRecord) null);

        var lookup = await reader.GetDonorGenotypeSets(DonorIds, FiveLoci, PinnedRecordId);

        lookup.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.ActiveDatabaseChanged);
        repositoryFactory.DidNotReceiveWithAnyArgs().GetForDatabase(default);
    }

    [Test]
    public async Task GetDonorGenotypeSets_WithNoPinnedRecord_ReturnsActiveDatabaseUnknown()
    {
        var lookup = await reader.GetDonorGenotypeSets(DonorIds, FiveLoci, null);

        lookup.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.ActiveDatabaseUnknown);
        historyRepository.DidNotReceive().GetActiveRecord();
        repositoryFactory.DidNotReceiveWithAnyArgs().GetForDatabase(default);
    }

    [Test]
    public async Task GetDonorGenotypeSets_ForLociThatAreNotOneOfTheFourKeys_ReturnsUncoveredAllowedLoci()
    {
        var lookup = await reader.GetDonorGenotypeSets(DonorIds, new HashSet<Locus> { Locus.A, Locus.B, Locus.C }, PinnedRecordId);

        lookup.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.UncoveredAllowedLoci);
        lookup.AllowedLociKey.Should().BeNull();
        repositoryFactory.DidNotReceiveWithAnyArgs().GetForDatabase(default);
    }

    [Test]
    public async Task GetDonorGenotypeSets_ReadsTheActiveRecordFreshOnEveryCall()
    {
        await reader.GetDonorGenotypeSets(DonorIds, FiveLoci, PinnedRecordId);
        await reader.GetDonorGenotypeSets(DonorIds, FiveLoci, PinnedRecordId);

        historyRepository.Received(2).GetActiveRecord();
    }
}
