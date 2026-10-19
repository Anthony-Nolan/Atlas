using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
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

/// <summary>The parity check: <see cref="GenotypeSetDebugRequest.CompareWithStored"/> (ATL-420).</summary>
[TestFixture]
internal partial class GenotypeSetDebugServiceTests
{
    [Test]
    public async Task CompareWithStored_ForADonorWhoseRowsArePrecomputed_ReadsThemAsSearchDoes_AndGivesMatchForEveryRow()
    {
        var donor = NewDonor();
        GivenDonors(donor);
        var live = GivenTheLiveSet();
        GivenStoredDonorRows((_, allowedLociKey) => PrecomputedRow(donor, allowedLociKey, live));

        var result = await service.Compute(new GenotypeSetDebugRequest { ExternalDonorCodes = [donor.ExternalDonorCode], CompareWithStored = true });

        ParityChecks(result).Should().HaveCount(4).And.OnlyContain(check =>
            check.Result == ParityResult.Match
            && check.DifferenceCount == 0
            && check.StoredHaplotypeFrequencySetId == searchFrequencySet.Id
            && check.SearchHaplotypeFrequencySetId == searchFrequencySet.Id);
        repositoryFactory.Received().GetSubjectGenotypeSetRepositoryForDatabase(activeRecord.Database);
        foreach (var allowedLociKey in AllowedLociKeyExtensions.All)
        {
            await genotypeSetRepository.Received(1).GetDonorSubjectGenotypeSets(
                Arg.Is<IReadOnlyCollection<int>>(donorIds => donorIds.SequenceEqual(new[] { donor.DonorId })),
                allowedLociKey);
        }
    }

    [Test]
    public async Task CompareWithStored_ForATyping_ReadsTheValueRowWithTheKeyOfTheLiveSet_AndGivesMatch()
    {
        // How a patient's row, which the warm step of search stores, is checked: a patient has no link row.
        var typing = Typing();
        var live = GivenTheLiveSet();
        var key = new SubjectGenotypeSetKey(Key(typing, AllowedLociKey.ABDrb1), searchFrequencySet.Id, AllowedLociKey.ABDrb1);
        genotypeSetRepository.GetSubjectGenotypeSetValue(key)
            .Returns(new StoredSubjectGenotypeSetValue(false, SubjectGenotypeSetPayload.Encode(live)));

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            HlaTyping = typing,
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            CompareWithStored = true
        });

        ParityChecks(result).Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Result = ParityResult.Match,
            StoredHlaTypingKey = key.HlaTypingKey,
            StoredHaplotypeFrequencySetId = searchFrequencySet.Id,
            SearchHaplotypeFrequencySetId = (int?)null,
            DifferenceCount = 0
        });
        result.Parity!.DonorsNotFound.Should().BeNull();
        await genotypeSetRepository.DidNotReceiveWithAnyArgs().GetDonorSubjectGenotypeSets(default, default);
    }

    [Test]
    public async Task CompareWithStored_ForATypingWithNoStoredRow_GivesNoStoredRow()
    {
        GivenTheLiveSet();

        var result = await service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing(), CompareWithStored = true });

        ParityChecks(result).Should().HaveCount(4).And.OnlyContain(check =>
            check.Result == ParityResult.NoStoredRow && check.StoredHlaTypingKey == null && check.DifferenceCount == null);
    }

    [Test]
    public async Task CompareWithStored_ForADonorWithNoLinkRows_GivesNoStoredRow()
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
        genotypeSetRepository.GetSubjectGenotypeSetValue(default).ReturnsForAnyArgs(new StoredSubjectGenotypeSetValue(true, null));

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
        genotypeSetRepository.GetSubjectGenotypeSetValue(default).ReturnsForAnyArgs(new StoredSubjectGenotypeSetValue(true, null));

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
        genotypeSetRepository.GetSubjectGenotypeSetValue(default)
            .ReturnsForAnyArgs(new StoredSubjectGenotypeSetValue(false, fixture.CreateMany<byte>(3).ToArray()));

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
        genotypeSetRepository.GetSubjectGenotypeSetValue(default).ReturnsForAnyArgs(new StoredSubjectGenotypeSetValue(false, null));

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

    [Test]
    public async Task CompareWithStored_ForADonorRowOfAnotherTyping_GivesTypingChangedWithBothKeys_AndStillCompares()
    {
        var donor = NewDonor();
        GivenDonors(donor);
        var live = GivenTheLiveSet();
        var otherTyping = Typing();
        GivenStoredDonorRows((_, allowedLociKey) => PrecomputedRow(donor, allowedLociKey, live, typingKey: Key(otherTyping, allowedLociKey)));

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = [donor.ExternalDonorCode],
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            CompareWithStored = true
        });

        var set = result.Subjects.Single().GenotypeSets.Single();
        set.HlaTypingKey.Should().Be(Key(donor.HlaNames, AllowedLociKey.ABDrb1));
        set.ParityCheck.Should().BeEquivalentTo(new
        {
            Result = ParityResult.TypingChanged,
            StoredHlaTypingKey = Key(otherTyping, AllowedLociKey.ABDrb1),
            DifferenceCount = 0
        });
    }

    [Test]
    public async Task CompareWithStored_ForADonorRowOfAnotherFrequencySet_GivesStaleFrequencySetWithBothIds_AndTheDifferences()
    {
        var donor = NewDonor();
        GivenDonors(donor);
        GivenTheLiveSet();
        var olderSetId = fixture.Create<int>();
        var olderSet = RepresentedSet();
        GivenStoredDonorRows((_, allowedLociKey) => PrecomputedRow(donor, allowedLociKey, olderSet, frequencySetId: olderSetId));

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = [donor.ExternalDonorCode],
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            CompareWithStored = true
        });

        var check = ParityChecks(result).Single();
        check.Result.Should().Be(ParityResult.StaleFrequencySet);
        check.StoredHaplotypeFrequencySetId.Should().Be(olderSetId);
        check.SearchHaplotypeFrequencySetId.Should().Be(searchFrequencySet.Id);
        check.DifferenceCount.Should().BePositive("the comparison still runs, to show the effect of the newer set");
        check.Differences.First().Should().BeEquivalentTo(new GenotypeSetDifference(
            GenotypeSetDifferenceKind.HaplotypeFrequencySet,
            null,
            null,
            Text(searchFrequencySet.Id),
            Text(olderSetId)));
    }

    [Test]
    public async Task CompareWithStored_ForADonorRowOfAnotherTypingAndFrequencySet_GivesTypingChanged_AsSearchChecksTheTypingFirst()
    {
        var donor = NewDonor();
        GivenDonors(donor);
        var live = GivenTheLiveSet();
        var otherTyping = Typing();
        var olderSetId = fixture.Create<int>();
        GivenStoredDonorRows((_, allowedLociKey) =>
            PrecomputedRow(donor, allowedLociKey, live, frequencySetId: olderSetId, typingKey: Key(otherTyping, allowedLociKey)));

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = [donor.ExternalDonorCode],
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            CompareWithStored = true
        });

        var check = ParityChecks(result).Single();
        check.Result.Should().Be(ParityResult.TypingChanged);
        check.StoredHaplotypeFrequencySetId.Should().Be(olderSetId);
    }

    [Test]
    public async Task CompareWithStored_ForAStaleRowThatCannotBeDecoded_GivesStaleFrequencySet_AndTheDecodeError()
    {
        var donor = NewDonor();
        GivenDonors(donor);
        GivenTheLiveSet();
        var olderSetId = fixture.Create<int>();
        GivenStoredDonorRows((_, allowedLociKey) => new StoredDonorSubjectGenotypeSet(
            donor.DonorId,
            Key(donor.HlaNames, allowedLociKey),
            olderSetId,
            false,
            fixture.CreateMany<byte>(3).ToArray()));

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = [donor.ExternalDonorCode],
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            CompareWithStored = true
        });

        var check = ParityChecks(result).Single();
        check.Result.Should().Be(ParityResult.StaleFrequencySet);
        check.DecodeError.Should().Contain(nameof(InvalidDataException));
        check.DifferenceCount.Should().BeNull();
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
        GivenStoredDonorRows((_, allowedLociKey) => PrecomputedRow(donor, allowedLociKey, live, frequencySetId: requested.Id));

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
    public async Task CompareWithStored_ForADonor_WithAFrequencySetIdThatItsRowDoesNotHave_GivesTheTwoFrequencySetsAsTheFirstDifference()
    {
        // For example, a save with an older set, and then a search that stored the row again with the set that it uses.
        var donor = NewDonor();
        GivenDonors(donor);
        var live = GivenTheLiveSet();
        var requested = fixture.Create<HaplotypeFrequencySet>();
        frequencySetReader.GetHaplotypeFrequencySet(requested.Id).Returns(requested);
        GivenStoredDonorRows((_, allowedLociKey) => PrecomputedRow(donor, allowedLociKey, live));

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = [donor.ExternalDonorCode],
            HaplotypeFrequencySetId = requested.Id,
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            CompareWithStored = true
        });

        var check = ParityChecks(result).Single();
        check.Result.Should().Be(ParityResult.Mismatch, "search would use the row, but the live set has another frequency set");
        check.StoredHaplotypeFrequencySetId.Should().Be(searchFrequencySet.Id);
        check.Differences.Should().ContainSingle().Which.Should().BeEquivalentTo(new GenotypeSetDifference(
            GenotypeSetDifferenceKind.HaplotypeFrequencySet,
            null,
            null,
            Text(requested.Id),
            Text(searchFrequencySet.Id)));
    }

    [Test]
    public async Task CompareWithStored_WhenTheLiveComputationOfOneSetFails_GivesErrorForThatSetOnly()
    {
        var donor = NewDonor();
        GivenDonors(donor);
        var live = GivenTheLiveSet();
        GivenStoredDonorRows((_, allowedLociKey) => PrecomputedRow(donor, allowedLociKey, live));
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
        GivenStoredDonorRows((donorId, allowedLociKey) => PrecomputedRow(donors.Single(donor => donor.DonorId == donorId), allowedLociKey, live));
        var message = fixture.Create<string>();
        genotypeSetRepository
            .GetDonorSubjectGenotypeSets(Arg.Any<IReadOnlyCollection<int>>(), AllowedLociKey.ABDrb1)
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
        GivenStoredDonorRows((donorId, allowedLociKey) => donorId == withRows.DonorId ? PrecomputedRow(withRows, allowedLociKey, live) : null);

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
            { ParityResult.TypingChanged, 0 },
            { ParityResult.NoStoredRow, 4 },
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

        await genotypeSetRepository.DidNotReceiveWithAnyArgs().GetDonorSubjectGenotypeSets(default, default);
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
    private void GivenStoredTypingRow(SubjectGenotypeSet stored) =>
        genotypeSetRepository.GetSubjectGenotypeSetValue(default).ReturnsForAnyArgs(new StoredSubjectGenotypeSetValue(
            stored.IsUnrepresented,
            stored.IsUnrepresented ? null : SubjectGenotypeSetPayload.Encode(stored)));

    /// <summary>The rows that the read of search gives: the row of each donor at each combination, or null for no row.</summary>
    private void GivenStoredDonorRows(Func<int, AllowedLociKey, StoredDonorSubjectGenotypeSet> rowOf) =>
        genotypeSetRepository.GetDonorSubjectGenotypeSets(default, default).ReturnsForAnyArgs(callInfo =>
            (callInfo.Arg<IReadOnlyCollection<int>>() ?? [])
            .Select(donorId => rowOf(donorId, callInfo.Arg<AllowedLociKey>()))
            .Where(row => row != null)
            .ToDictionary(row => row.DonorId));

    /// <summary>The row that the pre-computation stores for the donor and the set: by default, its typing and the set that search chooses.</summary>
    private StoredDonorSubjectGenotypeSet PrecomputedRow(
        DonorInfo donor,
        AllowedLociKey allowedLociKey,
        SubjectGenotypeSet set,
        int? frequencySetId = null,
        string typingKey = null) =>
        new(
            donor.DonorId,
            typingKey ?? Key(donor.HlaNames, allowedLociKey),
            frequencySetId ?? searchFrequencySet.Id,
            set.IsUnrepresented,
            set.IsUnrepresented ? null : SubjectGenotypeSetPayload.Encode(set));

    private static string Key(PhenotypeInfo<string> typing, AllowedLociKey allowedLociKey) =>
        SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(typing, allowedLociKey);

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static GenotypeAtDesiredResolutions WithLikelihood(GenotypeAtDesiredResolutions genotype, decimal likelihood) =>
        new(new ImputedGenotype(null, genotype.HaplotypeResolution, likelihood), genotype.StringMatchableResolution);

    private static IReadOnlyList<GenotypeSetParityCheck> ParityChecks(GenotypeSetDebugResult result) =>
        result.Subjects.SelectMany(subject => subject.GenotypeSets).Select(set => set.ParityCheck).ToList();
}
