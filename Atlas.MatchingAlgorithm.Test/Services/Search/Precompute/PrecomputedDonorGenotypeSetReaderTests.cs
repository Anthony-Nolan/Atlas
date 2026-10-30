using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Services.Search.Precompute;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.Precompute;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Precompute;

[TestFixture]
internal class PrecomputedDonorGenotypeSetReaderTests
{
    private const int PinnedRecordId = 42;
    private const int FrequencySetId = 7;
    private static readonly IReadOnlySet<Locus> FiveLoci = new HashSet<Locus> { Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1 };
    private static readonly PhenotypeInfo<string> SearchTyping = new((locus, position) => $"{locus}*search-{position}");

    /// <summary>One donor input of two donors, with the typing matching read and the frequency set the search uses.</summary>
    private static readonly IReadOnlyCollection<DonorGenotypeSetToRead> Donors =
        [new(new DonorInput { DonorIds = [1, 2], DonorHla = SearchTyping.ToPhenotypeInfoTransfer() }, FrequencySetId)];

    private static readonly SubjectGenotypeSetKey SearchKey = new(
        SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(SearchTyping, AllowedLociKey.ABCDrb1Dqb1),
        FrequencySetId,
        AllowedLociKey.ABCDrb1Dqb1);

    private Fixture fixture;
    private IDataRefreshHistoryRepository historyRepository;
    private ISubjectGenotypeSetRepositoryFactory repositoryFactory;
    private ISubjectGenotypeSetRepository repository;
    private IPrecomputedDonorGenotypeSetReader reader;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        historyRepository = Substitute.For<IDataRefreshHistoryRepository>();
        repositoryFactory = Substitute.For<ISubjectGenotypeSetRepositoryFactory>();
        repository = Substitute.For<ISubjectGenotypeSetRepository>();
        repositoryFactory.GetForDatabase(default).ReturnsForAnyArgs(repository);
        GivenStoredValues(new Dictionary<SubjectGenotypeSetKey, StoredSubjectGenotypeSetValue>
        {
            { SearchKey, new StoredSubjectGenotypeSetValue(false, [1, 2, 3]) }
        });

        historyRepository.GetActiveRecord().Returns(new ActiveDataRefreshRecord(PinnedRecordId, TransientDatabase.DatabaseB, "3330"));

        reader = new PrecomputedDonorGenotypeSetReader(historyRepository, repositoryFactory);
    }

    [Test]
    public async Task GetDonorGenotypeSets_WhenPinnedRecordIsStillActive_ReadsTheValueOfTheInputsKeyFromItsDatabase()
    {
        var lookup = await reader.GetDonorGenotypeSets(Donors, FiveLoci, PinnedRecordId, null);

        repositoryFactory.Received(1).GetForDatabase(TransientDatabase.DatabaseB);
        await repository.Received(1).GetSubjectGenotypeSetValues(
            Arg.Is<IReadOnlyCollection<SubjectGenotypeSetKey>>(keys => keys.SequenceEqual(new[] { SearchKey })));
        lookup.BatchFallbackReason.Should().BeNull();
        lookup.AllowedLociKey.Should().Be("ABCDrb1Dqb1");
        lookup.Rows.Should().ContainKey(1);
        lookup.Rows[1].HaplotypeFrequencySetId.Should().Be(FrequencySetId);
        lookup.Rows[1].SubjectGenotypeSetData.Should().Equal(1, 2, 3);
    }

    /// <summary>A donor input groups the donors with one typing and frequency set metadata, so they share one value.</summary>
    [Test]
    public async Task GetDonorGenotypeSets_ForADonorInputWithSeveralDonorIds_ReadsItsValueOnce_AndGivesItToEveryId()
    {
        var donorIds = fixture.CreateMany<int>(3).ToList();
        var payload = fixture.CreateMany<byte>().ToArray();
        GivenStoredValues(new Dictionary<SubjectGenotypeSetKey, StoredSubjectGenotypeSetValue>
        {
            { SearchKey, new StoredSubjectGenotypeSetValue(false, payload) }
        });

        var lookup = await reader.GetDonorGenotypeSets(
            [new DonorGenotypeSetToRead(new DonorInput { DonorIds = donorIds, DonorHla = SearchTyping.ToPhenotypeInfoTransfer() }, FrequencySetId)],
            FiveLoci,
            PinnedRecordId,
            null);

        await repository.Received(1).GetSubjectGenotypeSetValues(
            Arg.Is<IReadOnlyCollection<SubjectGenotypeSetKey>>(keys => keys.SequenceEqual(new[] { SearchKey })));
        lookup.Rows.Keys.Should().BeEquivalentTo(donorIds);
        lookup.Rows.Values.Should().OnlyContain(row => row.SubjectGenotypeSetData == payload);
    }

    /// <summary>For example, two inputs whose metadata differ, but choose the same frequency set.</summary>
    [Test]
    public async Task GetDonorGenotypeSets_ForTwoDonorInputsWithOneKey_ReadsTheKeyOnce()
    {
        var first = InputWithSearchTyping(fixture.Create<int>(), FrequencySetId);
        var second = InputWithSearchTyping(fixture.Create<int>(), FrequencySetId);

        var lookup = await reader.GetDonorGenotypeSets([first, second], FiveLoci, PinnedRecordId, null);

        await repository.Received(1).GetSubjectGenotypeSetValues(
            Arg.Is<IReadOnlyCollection<SubjectGenotypeSetKey>>(keys => keys.SequenceEqual(new[] { SearchKey })));
        lookup.Rows.Should().HaveCount(2);
    }

    /// <summary>The key holds the frequency set, so a value of another set is never used for the input.</summary>
    [Test]
    public async Task GetDonorGenotypeSets_ReadsEachDonorInputWithItsOwnFrequencySet()
    {
        var otherSetId = fixture.Create<int>();
        var otherKey = SearchKey with { HaplotypeFrequencySetId = otherSetId };
        var first = InputWithSearchTyping(fixture.Create<int>(), FrequencySetId);
        var second = InputWithSearchTyping(fixture.Create<int>(), otherSetId);
        var firstPayload = fixture.CreateMany<byte>().ToArray();
        var secondPayload = fixture.CreateMany<byte>().ToArray();
        GivenStoredValues(new Dictionary<SubjectGenotypeSetKey, StoredSubjectGenotypeSetValue>
        {
            { SearchKey, new StoredSubjectGenotypeSetValue(false, firstPayload) },
            { otherKey, new StoredSubjectGenotypeSetValue(false, secondPayload) }
        });

        var lookup = await reader.GetDonorGenotypeSets([first, second], FiveLoci, PinnedRecordId, null);

        await repository.Received(1).GetSubjectGenotypeSetValues(
            Arg.Is<IReadOnlyCollection<SubjectGenotypeSetKey>>(keys => keys.Count == 2 && keys.Contains(SearchKey) && keys.Contains(otherKey)));
        lookup.Rows[first.Donor.DonorIds.Single()].Should()
            .BeEquivalentTo(new { HaplotypeFrequencySetId = FrequencySetId, SubjectGenotypeSetData = firstPayload });
        lookup.Rows[second.Donor.DonorIds.Single()].Should()
            .BeEquivalentTo(new { HaplotypeFrequencySetId = otherSetId, SubjectGenotypeSetData = secondPayload });
    }

    [Test]
    public async Task GetDonorGenotypeSets_ForADonorInputWithNoStoredValue_HasNoRowForItsIds()
    {
        GivenStoredValues(new Dictionary<SubjectGenotypeSetKey, StoredSubjectGenotypeSetValue>());

        var lookup = await reader.GetDonorGenotypeSets(Donors, FiveLoci, PinnedRecordId, null);

        lookup.BatchFallbackReason.Should().BeNull();
        lookup.Rows.Should().BeEmpty();
    }

    /// <summary>The key treats null and empty names as the same typing, as the pre-computation does when it writes it.</summary>
    [Test]
    public async Task GetDonorGenotypeSets_WhenTheSearchTypingHasEmptyNamesWhereTheStoredTypingHasNull_StillMatches()
    {
        var withNull = SearchTyping.SetPosition(Locus.C, LocusPosition.Two, null);
        var withEmpty = SearchTyping.SetPosition(Locus.C, LocusPosition.Two, string.Empty);
        var storedKey = new SubjectGenotypeSetKey(
            SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(withNull, AllowedLociKey.ABCDrb1Dqb1), FrequencySetId, AllowedLociKey.ABCDrb1Dqb1);
        GivenStoredValues(new Dictionary<SubjectGenotypeSetKey, StoredSubjectGenotypeSetValue>
        {
            { storedKey, new StoredSubjectGenotypeSetValue(false, [1]) }
        });

        var lookup = await reader.GetDonorGenotypeSets(
            [new DonorGenotypeSetToRead(new DonorInput { DonorIds = [1], DonorHla = withEmpty.ToPhenotypeInfoTransfer() }, FrequencySetId)],
            FiveLoci,
            PinnedRecordId,
            null);

        lookup.Rows.Should().ContainKey(1);
    }

    [Test]
    public async Task GetDonorGenotypeSets_WhenAnotherRecordIsNowActive_ReturnsActiveDatabaseChangedAndNeverTouchesEitherDatabase()
    {
        historyRepository.GetActiveRecord().Returns(new ActiveDataRefreshRecord(PinnedRecordId + 1, TransientDatabase.DatabaseA, "3440"));

        var lookup = await reader.GetDonorGenotypeSets(Donors, FiveLoci, PinnedRecordId, null);

        lookup.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.ActiveDatabaseChanged);
        lookup.Rows.Should().BeEmpty();
        repositoryFactory.DidNotReceiveWithAnyArgs().GetForDatabase(default);
    }

    [Test]
    public async Task GetDonorGenotypeSets_WhenNoRefreshIsActive_ReturnsActiveDatabaseChanged()
    {
        historyRepository.GetActiveRecord().Returns((ActiveDataRefreshRecord) null);

        var lookup = await reader.GetDonorGenotypeSets(Donors, FiveLoci, PinnedRecordId, null);

        lookup.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.ActiveDatabaseChanged);
        repositoryFactory.DidNotReceiveWithAnyArgs().GetForDatabase(default);
    }

    [Test]
    public async Task GetDonorGenotypeSets_WithNoPinnedRecord_ReturnsActiveDatabaseUnknown()
    {
        var lookup = await reader.GetDonorGenotypeSets(Donors, FiveLoci, null, null);

        lookup.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.ActiveDatabaseUnknown);
        historyRepository.DidNotReceive().GetActiveRecord();
        repositoryFactory.DidNotReceiveWithAnyArgs().GetForDatabase(default);
    }

    [Test]
    public async Task GetDonorGenotypeSets_ForLociThatAreNotOneOfTheFourKeys_ReturnsUncoveredAllowedLoci()
    {
        var lookup = await reader.GetDonorGenotypeSets(Donors, new HashSet<Locus> { Locus.A, Locus.B, Locus.C }, PinnedRecordId, null);

        lookup.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.UncoveredAllowedLoci);
        lookup.AllowedLociKey.Should().BeNull();
        repositoryFactory.DidNotReceiveWithAnyArgs().GetForDatabase(default);
    }

    [Test]
    public async Task GetDonorGenotypeSets_ReadsTheActiveRecordFreshOnEveryCall()
    {
        await reader.GetDonorGenotypeSets(Donors, FiveLoci, PinnedRecordId, null);
        await reader.GetDonorGenotypeSets(Donors, FiveLoci, PinnedRecordId, null);

        historyRepository.Received(2).GetActiveRecord();
    }

    [Test]
    public async Task GetDonorGenotypeSets_WithAPatientKey_ReadsThePatientRowInTheSameReadAsTheDonors()
    {
        var patientKey = new PatientGenotypeSetKey("patient-key", 9, "ABCDrb1Dqb1");
        var patientValueKey = new SubjectGenotypeSetKey("patient-key", 9, AllowedLociKey.ABCDrb1Dqb1);
        GivenStoredValues(new Dictionary<SubjectGenotypeSetKey, StoredSubjectGenotypeSetValue>
        {
            { SearchKey, new StoredSubjectGenotypeSetValue(false, [1, 2, 3]) },
            { patientValueKey, new StoredSubjectGenotypeSetValue(false, [4, 5]) }
        });

        var lookup = await reader.GetDonorGenotypeSets(Donors, FiveLoci, PinnedRecordId, patientKey);

        lookup.PatientRow.IsUnrepresented.Should().BeFalse();
        lookup.PatientRow.SubjectGenotypeSetData.Should().Equal(4, 5);
        repositoryFactory.Received().GetForDatabase(TransientDatabase.DatabaseB);
        await repository.Received(1).GetSubjectGenotypeSetValues(Arg.Is<IReadOnlyCollection<SubjectGenotypeSetKey>>(keys =>
            keys.Count == 2 && keys.Contains(SearchKey) && keys.Contains(patientValueKey)));
    }

    /// <summary>A patient and a donor with the same typing and frequency set share one stored row (ATL-426).</summary>
    [Test]
    public async Task GetDonorGenotypeSets_WhenThePatientKeyIsTheKeyOfADonorInput_ReadsItOnce_ForBoth()
    {
        var patientKey = new PatientGenotypeSetKey(SearchKey.HlaTypingKey, SearchKey.HaplotypeFrequencySetId, SearchKey.AllowedLociKey.ToString());

        var lookup = await reader.GetDonorGenotypeSets(Donors, FiveLoci, PinnedRecordId, patientKey);

        await repository.Received(1).GetSubjectGenotypeSetValues(
            Arg.Is<IReadOnlyCollection<SubjectGenotypeSetKey>>(keys => keys.SequenceEqual(new[] { SearchKey })));
        lookup.PatientRow.SubjectGenotypeSetData.Should().Equal(1, 2, 3);
        lookup.Rows[1].SubjectGenotypeSetData.Should().Equal(1, 2, 3);
    }

    [Test]
    public async Task GetDonorGenotypeSets_WhenThePatientKeyHasNoRow_HasNoPatientRow()
    {
        var lookup = await reader.GetDonorGenotypeSets(Donors, FiveLoci, PinnedRecordId, new PatientGenotypeSetKey("patient-key", 9, "ABCDrb1Dqb1"));

        lookup.PatientRow.Should().BeNull();
    }

    [Test]
    public async Task GetDonorGenotypeSets_WithoutAPatientKey_ReadsOnlyTheDonorKeys()
    {
        var lookup = await reader.GetDonorGenotypeSets(Donors, FiveLoci, PinnedRecordId, null);

        lookup.PatientRow.Should().BeNull();
        await repository.Received(1).GetSubjectGenotypeSetValues(
            Arg.Is<IReadOnlyCollection<SubjectGenotypeSetKey>>(keys => keys.SequenceEqual(new[] { SearchKey })));
    }

    [Test]
    public async Task GetDonorGenotypeSets_WithAPatientKeyForOtherLoci_DoesNotReadAPatientRow()
    {
        var lookup = await reader.GetDonorGenotypeSets(Donors, FiveLoci, PinnedRecordId, new PatientGenotypeSetKey("patient-key", 9, "ABDrb1"));

        lookup.PatientRow.Should().BeNull();
        await repository.Received(1).GetSubjectGenotypeSetValues(
            Arg.Is<IReadOnlyCollection<SubjectGenotypeSetKey>>(keys => keys.SequenceEqual(new[] { SearchKey })));
    }

    [Test]
    public async Task GetDonorGenotypeSets_WhenThePinnedRecordIsNoLongerActive_DoesNotReadThePatientRow()
    {
        historyRepository.GetActiveRecord().Returns(new ActiveDataRefreshRecord(PinnedRecordId + 1, TransientDatabase.DatabaseA, "3330"));

        var lookup = await reader.GetDonorGenotypeSets(Donors, FiveLoci, PinnedRecordId, new PatientGenotypeSetKey("patient-key", 9, "ABCDrb1Dqb1"));

        lookup.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.ActiveDatabaseChanged);
        lookup.PatientRow.Should().BeNull();
        await repository.DidNotReceiveWithAnyArgs().GetSubjectGenotypeSetValues(default);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task FindPatientGenotypeSet_BuildsTheKeyFromTheTypingFrequencySetAndLociAndChecksTheStore(bool exists)
    {
        var expectedKey = new SubjectGenotypeSetKey(SearchKey.HlaTypingKey, 9, AllowedLociKey.ABCDrb1Dqb1);
        repository.GetExistingValueIds(default).ReturnsForAnyArgs(exists
            ? new Dictionary<SubjectGenotypeSetKey, int> { { expectedKey, 100 } }
            : new Dictionary<SubjectGenotypeSetKey, int>());

        var lookup = await reader.FindPatientGenotypeSet(SearchTyping, 9, FiveLoci, PinnedRecordId);

        lookup.UnavailableReason.Should().BeNull();
        lookup.Key.Should().Be(new PatientGenotypeSetKey(SearchKey.HlaTypingKey, 9, "ABCDrb1Dqb1"));
        lookup.Exists.Should().Be(exists);
        repositoryFactory.Received().GetForDatabase(TransientDatabase.DatabaseB);
        await repository.Received(1).GetExistingValueIds(Arg.Is<IReadOnlyCollection<SubjectGenotypeSetKey>>(keys => keys.Single() == expectedKey));
    }

    /// <summary>A patient's key must equal a donor's for the same typing, so that one can reuse the other's row.</summary>
    [Test]
    public async Task FindPatientGenotypeSet_TreatsEmptyAndNullNamesAlike()
    {
        var withEmpty = SearchTyping.SetPosition(Locus.C, LocusPosition.Two, string.Empty);
        var withNull = SearchTyping.SetPosition(Locus.C, LocusPosition.Two, null);
        repository.GetExistingValueIds(default).ReturnsForAnyArgs(new Dictionary<SubjectGenotypeSetKey, int>());

        var fromEmpty = await reader.FindPatientGenotypeSet(withEmpty, 9, FiveLoci, PinnedRecordId);
        var fromNull = await reader.FindPatientGenotypeSet(withNull, 9, FiveLoci, PinnedRecordId);

        fromEmpty.Key.Should().Be(fromNull.Key);
    }

    [Test]
    public async Task FindPatientGenotypeSet_WhenThePinnedRecordIsNoLongerActive_ReturnsActiveDatabaseChanged()
    {
        historyRepository.GetActiveRecord().Returns(new ActiveDataRefreshRecord(PinnedRecordId + 1, TransientDatabase.DatabaseA, "3330"));

        var lookup = await reader.FindPatientGenotypeSet(SearchTyping, 9, FiveLoci, PinnedRecordId);

        lookup.UnavailableReason.Should().Be(DonorGenotypeSetSource.ActiveDatabaseChanged);
        lookup.Key.Should().BeNull();
        repositoryFactory.DidNotReceiveWithAnyArgs().GetForDatabase(default);
    }

    [Test]
    public async Task FindPatientGenotypeSet_WithNoPinnedRecord_ReturnsActiveDatabaseUnknown()
    {
        var lookup = await reader.FindPatientGenotypeSet(SearchTyping, 9, FiveLoci, null);

        lookup.UnavailableReason.Should().Be(DonorGenotypeSetSource.ActiveDatabaseUnknown);
        historyRepository.DidNotReceive().GetActiveRecord();
    }

    [Test]
    public async Task FindPatientGenotypeSet_ForLociThatAreNotOneOfTheFourKeys_ReturnsUncoveredAllowedLoci()
    {
        var lookup = await reader.FindPatientGenotypeSet(SearchTyping, 9, new HashSet<Locus> { Locus.A, Locus.B, Locus.C }, PinnedRecordId);

        lookup.UnavailableReason.Should().Be(DonorGenotypeSetSource.UncoveredAllowedLoci);
    }

    private void GivenStoredValues(Dictionary<SubjectGenotypeSetKey, StoredSubjectGenotypeSetValue> values) =>
        repository.GetSubjectGenotypeSetValues(default).ReturnsForAnyArgs(values);

    /// <summary>A donor input with one id, the search's typing and metadata of its own, that chose the given frequency set.</summary>
    private DonorGenotypeSetToRead InputWithSearchTyping(int donorId, int frequencySetId) =>
        new(
            new DonorInput
            {
                DonorIds = [donorId],
                DonorHla = SearchTyping.ToPhenotypeInfoTransfer(),
                DonorFrequencySetMetadata = fixture.Create<FrequencySetMetadata>()
            },
            frequencySetId);
}
