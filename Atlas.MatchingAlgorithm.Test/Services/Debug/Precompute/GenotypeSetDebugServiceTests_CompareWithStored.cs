using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Services.Debug.Precompute;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.Precompute;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Debug.Precompute;

/// <summary>
/// The parity check: <see cref="GenotypeSetDebugRequest.CompareWithStored"/> (ATL-420). It reads each row by the key of
/// the live set, for a donor as for a typing, as search does (ATL-424).
/// </summary>
[TestFixture]
internal partial class GenotypeSetDebugServiceTests
{
    [Test]
    public async Task CompareWithStored_ForAPrecomputedDonor_ReadsEachRowByTheKeyOfTheLiveSet_AndGivesMatchForEveryRow()
    {
        var donor = NewDonor();
        GivenDonors(donor);
        var live = GivenTheLiveSet();
        GivenStoredRows(_ => Value(live));

        var result = await service.Compute(new GenotypeSetDebugRequest { ExternalDonorCodes = [donor.ExternalDonorCode], CompareWithStored = true });

        ParityChecks(result).Should().HaveCount(4).And.OnlyContain(check =>
            check.Result == ParityResult.Match
            && check.DifferenceCount == 0
            && check.StoredHaplotypeFrequencySetId == searchFrequencySet.Id
            && check.SearchHaplotypeFrequencySetId == searchFrequencySet.Id);
        repositoryFactory.Received().GetSubjectGenotypeSetRepositoryForDatabase(activeRecord.Database);
        foreach (var allowedLociKey in AllowedLociKeyExtensions.All)
        {
            var key = new SubjectGenotypeSetKey(Key(donor.HlaNames, allowedLociKey), searchFrequencySet.Id, allowedLociKey);
            await genotypeSetRepository.Received(1).GetSubjectGenotypeSetValues(
                Arg.Is<IReadOnlyCollection<SubjectGenotypeSetKey>>(keys => keys.SequenceEqual(new[] { key })));
        }
    }

    [Test]
    public async Task CompareWithStored_ForATyping_ReadsTheValueRowWithTheKeyOfTheLiveSet_AndGivesMatch()
    {
        // How a patient's row, which the warm step of search stores, is checked: a patient has no link row.
        var typing = Typing();
        var live = GivenTheLiveSet();
        var key = new SubjectGenotypeSetKey(Key(typing, AllowedLociKey.ABDrb1), searchFrequencySet.Id, AllowedLociKey.ABDrb1);
        GivenStoredRows(storedKey => storedKey == key ? Value(live) : null);

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            HlaTyping = typing,
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            CompareWithStored = true
        });

        ParityChecks(result).Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Result = ParityResult.Match,
            StoredHaplotypeFrequencySetId = searchFrequencySet.Id,
            SearchHaplotypeFrequencySetId = (int?)null,
            DifferenceCount = 0
        });
        result.Parity!.DonorsNotFound.Should().BeNull();
    }

    [Test]
    public async Task CompareWithStored_ForATypingWithNoStoredRow_GivesNoStoredRow()
    {
        GivenTheLiveSet();

        var result = await service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing(), CompareWithStored = true });

        ParityChecks(result).Should().HaveCount(4).And.OnlyContain(check =>
            check.Result == ParityResult.NoStoredRow && check.StoredHaplotypeFrequencySetId == null && check.DifferenceCount == null);
    }

    [Test]
    public async Task CompareWithStored_ForAnUnambiguousTypingWithNoStoredRow_GivesNotStoredUnambiguous()
    {
        // ATL-478: the pre-computation does not store such a set, so no row is the expected state, not a miss.
        GivenTheLiveSet();
        genotypeSetService.IsUnambiguousAtSmallGGroup(default, default).ReturnsForAnyArgs(true);

        var result = await service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing(), CompareWithStored = true });

        ParityChecks(result).Should().HaveCount(4).And.OnlyContain(check => check.Result == ParityResult.NotStoredUnambiguous);
        result.Subjects.Single().GenotypeSets.Should().OnlyContain(set => set.IsUnambiguousAtSmallGGroup);
    }

    [Test]
    public async Task CompareWithStored_ForAnUnambiguousTypingWithAStoredRow_StillComparesIt()
    {
        // A row stored before ATL-478, or by the debug save, is read by search, so it is compared as any other row.
        var live = GivenTheLiveSet();
        GivenStoredTypingRow(live);
        genotypeSetService.IsUnambiguousAtSmallGGroup(default, default).ReturnsForAnyArgs(true);

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            HlaTyping = Typing(),
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            CompareWithStored = true
        });

        ParityChecks(result).Should().ContainSingle().Which.Result.Should().Be(ParityResult.Match);
    }

    [Test]
    public async Task CompareWithStored_ForADonorWithNoStoredRows_GivesNoStoredRow()
    {
        var donor = NewDonor();
        GivenDonors(donor);
        GivenTheLiveSet();

        var result = await service.Compute(new GenotypeSetDebugRequest { ExternalDonorCodes = [donor.ExternalDonorCode], CompareWithStored = true });

        ParityChecks(result).Should().HaveCount(4).And.OnlyContain(check =>
            check.Result == ParityResult.NoStoredRow && check.SearchHaplotypeFrequencySetId == searchFrequencySet.Id);
    }

    [Test]
    public async Task CompareWithStored_WithAStoredSetThatDiffers_GivesMismatchWithTheDifference()
    {
        var typing = Typing();
        var live = GivenTheLiveSet();
        var changed = live.Genotypes.First();
        var storedLikelihood = fixture.Create<decimal>();
        var stored = new SubjectGenotypeSet(
            false,
            live.Genotypes.Select(genotype => genotype == changed ? WithLikelihood(genotype, storedLikelihood) : genotype).ToList(),
            live.SumOfLikelihoods);
        GivenStoredTypingRow(stored);

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            HlaTyping = typing,
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            CompareWithStored = true
        });

        var check = ParityChecks(result).Should().ContainSingle().Which;
        check.Result.Should().Be(ParityResult.Mismatch);
        check.DifferenceCount.Should().Be(1);
        check.Differences.Should().ContainSingle().Which.Should().Match<GenotypeSetDifference>(difference =>
            difference.Kind == GenotypeSetDifferenceKind.GenotypeLikelihood
            && difference.StringMatchableResolution.Equals(changed.StringMatchableResolution)
            && difference.Stored == storedLikelihood.ToString(CultureInfo.InvariantCulture));
    }

    [Test]
    public async Task CompareWithStored_GivesAtMostMaxDifferencesPerRow_AndCountsThemAll()
    {
        var live = GivenTheLiveSet();
        var stored = new SubjectGenotypeSet(
            false,
            live.Genotypes.Select(genotype => WithLikelihood(genotype, fixture.Create<decimal>())).ToList(),
            fixture.Create<decimal>());
        GivenStoredTypingRow(stored);

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            HlaTyping = Typing(),
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            CompareWithStored = true,
            MaxDifferencesPerRow = 2
        });

        var check = ParityChecks(result).Single();
        check.DifferenceCount.Should().Be(live.Genotypes.Count + 1, "each likelihood and the sum differ");
        check.Differences.Should().HaveCount(2);
    }

    [Test]
    public async Task CompareWithStored_ForAnUnrepresentedRow_ComparesTheEmptySet_AsSearchUsesIt()
    {
        genotypeSetService.GetGenotypeSet(default, default).ReturnsForAnyArgs(new SubjectGenotypeSet(true, [], 0m));
        GivenStoredRows(_ => new StoredSubjectGenotypeSetValue(true, null));

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            HlaTyping = Typing(),
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            CompareWithStored = true
        });

        ParityChecks(result).Single().Result.Should().Be(ParityResult.Match);
    }

    [Test]
    public async Task CompareWithStored_ForAnUnrepresentedRowOfARepresentedLiveSet_GivesMismatch()
    {
        GivenTheLiveSet();
        GivenStoredRows(_ => new StoredSubjectGenotypeSetValue(true, null));

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            HlaTyping = Typing(),
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            CompareWithStored = true
        });

        var check = ParityChecks(result).Single();
        check.Result.Should().Be(ParityResult.Mismatch);
        check.Differences.First().Should().Match<GenotypeSetDifference>(difference =>
            difference.Kind == GenotypeSetDifferenceKind.IsUnrepresented && difference.Live == "false" && difference.Stored == "true");
    }

    [Test]
    public async Task CompareWithStored_ForADamagedPayload_GivesDecodeFailedWithTheError()
    {
        GivenTheLiveSet();
        GivenStoredRows(_ => new StoredSubjectGenotypeSetValue(false, fixture.CreateMany<byte>(3).ToArray()));

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            HlaTyping = Typing(),
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            CompareWithStored = true
        });

        var check = ParityChecks(result).Single();
        check.Result.Should().Be(ParityResult.DecodeFailed);
        check.DecodeError.Should().Contain(nameof(InvalidDataException));
        check.DifferenceCount.Should().BeNull();
    }

    [Test]
    public async Task CompareWithStored_ForARowThatIsNotUnrepresentedButHasNoData_GivesDecodeFailed()
    {
        // Search cannot use such a row either.
        GivenTheLiveSet();
        GivenStoredRows(_ => new StoredSubjectGenotypeSetValue(false, null));

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            HlaTyping = Typing(),
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            CompareWithStored = true
        });

        var check = ParityChecks(result).Single();
        check.Result.Should().Be(ParityResult.DecodeFailed);
        check.DecodeError.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// For example, the row was stored before a new frequency set was activated for the donor's codes. Search reads the
    /// key of the set it uses now, and does not find the row either.
    /// </summary>
    [Test]
    public async Task CompareWithStored_ForADonorWithARowOfAnOlderFrequencySetOnly_GivesNoStoredRow()
    {
        var donor = NewDonor();
        GivenDonors(donor);
        var live = GivenTheLiveSet();
        var olderSetId = fixture.Create<int>();
        GivenStoredRows(key => key.HaplotypeFrequencySetId == olderSetId ? Value(live) : null);

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = [donor.ExternalDonorCode],
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            CompareWithStored = true
        });

        var check = ParityChecks(result).Single();
        check.Result.Should().Be(ParityResult.NoStoredRow);
        check.SearchHaplotypeFrequencySetId.Should().Be(searchFrequencySet.Id);
    }

    [Test]
    public async Task CompareWithStored_ForADonor_WithAFrequencySetId_ChecksTheRowAgainstTheSetThatSearchWouldUse()
    {
        var donor = NewDonor();
        GivenDonors(donor);
        var live = GivenTheLiveSet();
        var requested = fixture.Create<HaplotypeFrequencySet>();
        frequencySetReader.GetHaplotypeFrequencySet(requested.Id).Returns(requested);
        // For example, a save with this set stored the rows.
        GivenStoredRows(key => key.HaplotypeFrequencySetId == requested.Id ? Value(live) : null);

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = [donor.ExternalDonorCode],
            HaplotypeFrequencySetId = requested.Id,
            CompareWithStored = true
        });

        ParityChecks(result).Should().HaveCount(4).And.OnlyContain(check =>
            check.Result == ParityResult.StaleFrequencySet
            && check.StoredHaplotypeFrequencySetId == requested.Id
            && check.SearchHaplotypeFrequencySetId == searchFrequencySet.Id
            && check.DifferenceCount == 0);
        await frequencyLookupService.Received().GetSingleHaplotypeFrequencySet(Arg.Is<FrequencySetMetadata>(metadata =>
            metadata.RegistryCode == donor.RegistryCode && metadata.EthnicityCode == donor.EthnicityCode));
    }

    [Test]
    public async Task CompareWithStored_ForADonor_WithAFrequencySetIdAndARowThatCannotBeDecoded_GivesStaleFrequencySet_AndTheDecodeError()
    {
        var donor = NewDonor();
        GivenDonors(donor);
        GivenTheLiveSet();
        var requested = fixture.Create<HaplotypeFrequencySet>();
        frequencySetReader.GetHaplotypeFrequencySet(requested.Id).Returns(requested);
        GivenStoredRows(_ => new StoredSubjectGenotypeSetValue(false, fixture.CreateMany<byte>(3).ToArray()));

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = [donor.ExternalDonorCode],
            HaplotypeFrequencySetId = requested.Id,
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            CompareWithStored = true
        });

        var check = ParityChecks(result).Single();
        check.Result.Should().Be(ParityResult.StaleFrequencySet);
        check.DecodeError.Should().Contain(nameof(InvalidDataException));
        check.DifferenceCount.Should().BeNull();
    }

    [Test]
    public async Task CompareWithStored_ForADonor_WithAFrequencySetIdThatNoRowHas_GivesNoStoredRow()
    {
        // For example, search stored the row with the set that it uses, and the request names an older set.
        var donor = NewDonor();
        GivenDonors(donor);
        var live = GivenTheLiveSet();
        var requested = fixture.Create<HaplotypeFrequencySet>();
        frequencySetReader.GetHaplotypeFrequencySet(requested.Id).Returns(requested);
        GivenStoredRows(key => key.HaplotypeFrequencySetId == searchFrequencySet.Id ? Value(live) : null);

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = [donor.ExternalDonorCode],
            HaplotypeFrequencySetId = requested.Id,
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            CompareWithStored = true
        });

        var check = ParityChecks(result).Single();
        check.Result.Should().Be(ParityResult.NoStoredRow);
        check.SearchHaplotypeFrequencySetId.Should().Be(searchFrequencySet.Id);
    }

    [Test]
    public async Task CompareWithStored_WhenTheLiveComputationOfOneSetFails_GivesErrorForThatSetOnly()
    {
        var donor = NewDonor();
        GivenDonors(donor);
        var live = GivenTheLiveSet();
        GivenStoredRows(_ => Value(live));
        var message = fixture.Create<string>();
        genotypeSetService
            .GetGenotypeSet(
                Arg.Any<SubjectData>(),
                Arg.Is<MatchPredictionParameters>(parameters => parameters.AllowedLoci.SetEquals(AllowedLociKey.ABDrb1.ToLoci())))
            .ThrowsAsync(new InvalidOperationException(message));

        var result = await service.Compute(new GenotypeSetDebugRequest { ExternalDonorCodes = [donor.ExternalDonorCode], CompareWithStored = true });

        var sets = result.Subjects.Single().GenotypeSets;
        sets.Single(set => set.AllowedLociKey == AllowedLociKey.ABDrb1).ParityCheck.Should().Match<GenotypeSetParityCheck>(check =>
            check.Result == ParityResult.Error && check.Error.Contains(message));
        sets.Where(set => set.AllowedLociKey != AllowedLociKey.ABDrb1).Should().HaveCount(3)
            .And.OnlyContain(set => set.ParityCheck!.Result == ParityResult.Match);
    }

    [Test]
    public async Task CompareWithStored_WhenTheReadOfOneCombinationFails_GivesErrorForItsRowsOnly()
    {
        // For example, the first read of a paused serverless database fails.
        var donors = new[] { NewDonor(), NewDonor() };
        GivenDonors(donors);
        var live = GivenTheLiveSet();
        GivenStoredRows(_ => Value(live));
        var message = fixture.Create<string>();
        genotypeSetRepository
            .GetSubjectGenotypeSetValues(Arg.Is<IReadOnlyCollection<SubjectGenotypeSetKey>>(keys =>
                keys.Any(key => key.AllowedLociKey == AllowedLociKey.ABDrb1)))
            .ThrowsAsync(new InvalidOperationException(message));

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = donors.Select(donor => donor.ExternalDonorCode).ToList(),
            CompareWithStored = true
        });

        var sets = result.Subjects.SelectMany(subject => subject.GenotypeSets).ToList();
        sets.Where(set => set.AllowedLociKey == AllowedLociKey.ABDrb1).Should().HaveCount(2)
            .And.OnlyContain(set => set.ParityCheck!.Result == ParityResult.Error && set.ParityCheck.Error!.Contains(message));
        sets.Where(set => set.AllowedLociKey != AllowedLociKey.ABDrb1).Should().HaveCount(6)
            .And.OnlyContain(set => set.ParityCheck!.Result == ParityResult.Match);
    }

    [Test]
    public async Task Compute_WhenTheDonorsCannotBeRead_GivesTheErrorForEachCode_AndCountsThemAsSubjectErrors()
    {
        var codes = fixture.CreateMany<string>(2).ToList();
        var message = fixture.Create<string>();
        donorRepository.GetDonorsByExternalDonorCodes(default).ThrowsAsyncForAnyArgs(new InvalidOperationException(message));

        var result = await service.Compute(new GenotypeSetDebugRequest { ExternalDonorCodes = codes, CompareWithStored = true });

        result.Subjects.Select(subject => subject.ExternalDonorCode).Should().BeEquivalentTo(codes);
        result.Subjects.Should().OnlyContain(subject => !subject.Found && subject.Error!.Contains(message) && subject.GenotypeSets.Count == 0);
        result.Parity!.DonorsNotFound.Should().Be(0);
        result.Parity.SubjectErrors.Should().Be(codes.Count);
        await genotypeSetService.DidNotReceiveWithAnyArgs().GetGenotypeSet(default, default);
    }

    [Test]
    public async Task CompareWithStored_CountsEachResult_AndTheDonorCodesThatNoDonorHas()
    {
        var withRows = NewDonor();
        var withoutRows = NewDonor();
        GivenDonors(withRows, withoutRows);
        var live = GivenTheLiveSet();
        GivenStoredRows(key => key.HlaTypingKey == Key(withRows.HlaNames, key.AllowedLociKey) ? Value(live) : null);

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = [withRows.ExternalDonorCode, withoutRows.ExternalDonorCode, fixture.Create<string>()],
            CompareWithStored = true
        });

        result.Parity!.ResultCounts.Should().BeEquivalentTo(new Dictionary<ParityResult, int>
        {
            { ParityResult.Match, 4 },
            { ParityResult.Mismatch, 0 },
            { ParityResult.StaleFrequencySet, 0 },
            { ParityResult.NoStoredRow, 4 },
            { ParityResult.NotStoredUnambiguous, 0 },
            { ParityResult.DecodeFailed, 0 },
            { ParityResult.Error, 0 }
        });
        result.Parity.DonorsNotFound.Should().Be(1);
        result.Parity.SubjectErrors.Should().Be(0);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CompareWithStored_GivesTheGenotypesOnlyWhenTheRequestIncludesThem(bool includeGenotypes)
    {
        var live = GivenTheLiveSet();
        GivenStoredTypingRow(live);

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            HlaTyping = Typing(),
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            CompareWithStored = true,
            IncludeGenotypes = includeGenotypes
        });

        var set = result.Subjects.Single().GenotypeSets.Single();
        set.ParityCheck!.Result.Should().Be(ParityResult.Match);
        (set.Genotypes != null).Should().Be(includeGenotypes);
    }

    [Test]
    public async Task Compute_WithoutCompareWithStored_ReadsNoStoredRows_AndGivesNoParityCheck()
    {
        var donor = NewDonor();
        GivenDonors(donor);

        var result = await service.Compute(new GenotypeSetDebugRequest { ExternalDonorCodes = [donor.ExternalDonorCode] });

        await genotypeSetRepository.DidNotReceiveWithAnyArgs().GetSubjectGenotypeSetValues(default);
        result.Parity.Should().BeNull();
        ParityChecks(result).Should().HaveCount(4).And.OnlyContain(check => check == null);
    }

    /// <summary>The set that the engine gives for every typing and combination.</summary>
    private SubjectGenotypeSet GivenTheLiveSet()
    {
        var live = RepresentedSet();
        genotypeSetService.GetGenotypeSet(default, default).ReturnsForAnyArgs(live);
        return live;
    }

    /// <summary>The value row that the read by key gives, for every key.</summary>
    private void GivenStoredTypingRow(SubjectGenotypeSet stored) => GivenStoredRows(_ => Value(stored));

    /// <summary>The value rows that the read by key gives: the row of each key, or null for no row.</summary>
    private void GivenStoredRows(Func<SubjectGenotypeSetKey, StoredSubjectGenotypeSetValue> rowOf) =>
        genotypeSetRepository.GetSubjectGenotypeSetValues(default).ReturnsForAnyArgs(callInfo =>
            (callInfo.Arg<IReadOnlyCollection<SubjectGenotypeSetKey>>() ?? [])
            .Select(key => (Key: key, Row: rowOf(key)))
            .Where(stored => stored.Row != null)
            .ToDictionary(stored => stored.Key, stored => stored.Row));

    /// <summary>The value row that the pre-computation stores for the set.</summary>
    private static StoredSubjectGenotypeSetValue Value(SubjectGenotypeSet set) =>
        new(set.IsUnrepresented, set.IsUnrepresented ? null : SubjectGenotypeSetPayload.Encode(set));

    private static string Key(PhenotypeInfo<string> typing, AllowedLociKey allowedLociKey) =>
        SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(typing, allowedLociKey);

    private static GenotypeAtDesiredResolutions WithLikelihood(GenotypeAtDesiredResolutions genotype, decimal likelihood) =>
        new(new ImputedGenotype(null, genotype.HaplotypeResolution, likelihood), genotype.StringMatchableResolution);

    private static IReadOnlyList<GenotypeSetParityCheck> ParityChecks(GenotypeSetDebugResult result) =>
        result.Subjects.SelectMany(subject => subject.GenotypeSets).Select(set => set.ParityCheck).ToList();
}
