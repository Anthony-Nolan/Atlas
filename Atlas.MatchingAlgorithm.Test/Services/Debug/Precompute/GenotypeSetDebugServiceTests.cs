using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Services.Debug.Precompute;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Services.Precompute;
using AutoFixture;
using AwesomeAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Debug.Precompute;

[TestFixture]
internal class GenotypeSetDebugServiceTests
{
    private Fixture fixture;
    private ActiveDataRefreshRecord activeRecord;
    private HaplotypeFrequencySet searchFrequencySet;
    private int nextValueId;

    private IDataRefreshHistoryRepository historyRepository;
    private IStaticallyChosenDatabaseRepositoryFactory repositoryFactory;
    private IDonorInspectionRepository donorRepository;
    private ISubjectGenotypeSetRepository genotypeSetRepository;
    private IHaplotypeFrequencyLookupService frequencyLookupService;
    private IHaplotypeFrequencyCache frequencyCache;
    private IHaplotypeFrequencySetReader frequencySetReader;
    private IGenotypeSetService genotypeSetService;
    private DebugSettings debugSettings;
    private FakeLogger<GenotypeSetDebugService> logger;

    private IGenotypeSetDebugService service;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        activeRecord = new ActiveDataRefreshRecord(fixture.Create<int>(), fixture.Create<TransientDatabase>(), fixture.Create<string>());
        searchFrequencySet = fixture.Create<HaplotypeFrequencySet>();
        nextValueId = fixture.Create<int>();

        historyRepository = Substitute.For<IDataRefreshHistoryRepository>();
        historyRepository.GetActiveRecord().Returns(activeRecord);

        donorRepository = Substitute.For<IDonorInspectionRepository>();
        donorRepository.GetDonorsByExternalDonorCodes(default).ReturnsForAnyArgs(new List<DonorInfo>());

        genotypeSetRepository = Substitute.For<ISubjectGenotypeSetRepository>();
        genotypeSetRepository.GetExistingValueIds(default).ReturnsForAnyArgs(new Dictionary<SubjectGenotypeSetKey, int>());
        // The values are null only when a test sets up the store again: that call is not a store.
        genotypeSetRepository.GetOrCreateValueIds(default).ReturnsForAnyArgs(callInfo =>
            callInfo.Arg<IReadOnlyCollection<SubjectGenotypeSetValueToStore>>()?.ToDictionary(value => value.Key, _ => nextValueId++)
            ?? new Dictionary<SubjectGenotypeSetKey, int>());

        repositoryFactory = Substitute.For<IStaticallyChosenDatabaseRepositoryFactory>();
        repositoryFactory.GetDonorInspectionRepositoryForDatabase(default).ReturnsForAnyArgs(donorRepository);
        repositoryFactory.GetSubjectGenotypeSetRepositoryForDatabase(default).ReturnsForAnyArgs(genotypeSetRepository);

        frequencyLookupService = Substitute.For<IHaplotypeFrequencyLookupService>();
        frequencyLookupService.GetSingleHaplotypeFrequencySet(default).ReturnsForAnyArgs(searchFrequencySet);
        frequencyCache = Substitute.For<IHaplotypeFrequencyCache>();
        frequencyCache.GetActiveHaplotypeFrequencySets().Returns(new Dictionary<(string, string), HaplotypeFrequencySet>());
        frequencySetReader = Substitute.For<IHaplotypeFrequencySetReader>();

        genotypeSetService = Substitute.For<IGenotypeSetService>();
        genotypeSetService.GetGenotypeSet(default, default).ReturnsForAnyArgs(_ => RepresentedSet());

        debugSettings = new DebugSettings { AllowPrecomputeWrites = true };
        logger = new FakeLogger<GenotypeSetDebugService>();

        service = new GenotypeSetDebugService(
            historyRepository,
            repositoryFactory,
            frequencyLookupService,
            frequencyCache,
            frequencySetReader,
            genotypeSetService,
            debugSettings,
            logger);
    }

    [Test]
    public async Task Compute_ForATyping_ComputesEachOfTheFourCombinations_AtTheVersionOfTheActiveDatabase()
    {
        var result = await service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing() });

        result.Database.Should().Be(activeRecord.Database);
        result.HlaNomenclatureVersion.Should().Be(activeRecord.HlaNomenclatureVersion);
        var subject = result.Subjects.Should().ContainSingle().Which;
        subject.Found.Should().BeTrue();
        subject.GenotypeSets.Select(set => set.AllowedLociKey).Should().BeEquivalentTo(AllowedLociKeyExtensions.All);
        await genotypeSetService.Received(4).GetGenotypeSet(
            Arg.Any<SubjectData>(),
            Arg.Is<MatchPredictionParameters>(parameters => parameters.MatchingAlgorithmHlaNomenclatureVersion == activeRecord.HlaNomenclatureVersion));
    }

    [Test]
    public async Task Compute_ForATyping_GivesTheKeyAndTheFrequencySetThatThePreComputationUses()
    {
        var typing = Typing();

        var result = await service.Compute(new GenotypeSetDebugRequest { HlaTyping = typing, AllowedLociKeys = [AllowedLociKey.ABDrb1] });

        var set = result.Subjects.Single().GenotypeSets.Should().ContainSingle().Which;
        set.HlaTypingKey.Should().Be(SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(typing, AllowedLociKey.ABDrb1));
        set.HaplotypeFrequencySet.Should().Be(new HaplotypeFrequencySetUsed(
            searchFrequencySet.Id,
            searchFrequencySet.Name,
            searchFrequencySet.RegistryCode,
            searchFrequencySet.EthnicityCode,
            searchFrequencySet.HlaNomenclatureVersion,
            true));
        await genotypeSetService.Received(1).GetGenotypeSet(
            Arg.Is<SubjectData>(subject => subject.HlaTyping.Equals(typing) && subject.SubjectFrequencySet.FrequencySet == searchFrequencySet),
            Arg.Is<MatchPredictionParameters>(parameters => parameters.AllowedLoci.SetEquals(AllowedLociKey.ABDrb1.ToLoci())));
    }

    [Test]
    public async Task Compute_ForATypingWithFrequencySetMetadata_ChoosesTheSetWithTheRulesOfSearch()
    {
        var metadata = fixture.Create<FrequencySetMetadata>();

        await service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing(), FrequencySetMetadata = metadata });

        await frequencyLookupService.Received().GetSingleHaplotypeFrequencySet(metadata);
    }

    [Test]
    public async Task Compute_ForATypingWithNoFrequencySetMetadata_ChoosesTheGlobalSet()
    {
        await service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing() });

        await frequencyLookupService.Received().GetSingleHaplotypeFrequencySet(
            Arg.Is<FrequencySetMetadata>(metadata => metadata.RegistryCode == null && metadata.EthnicityCode == null));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Compute_WithAFrequencySetId_UsesThatSet_AndSaysWhetherSearchWouldChooseIt(bool isActive)
    {
        var requested = fixture.Create<HaplotypeFrequencySet>();
        frequencySetReader.GetHaplotypeFrequencySet(requested.Id).Returns(requested);
        var activeForItsCodes = isActive ? requested : fixture.Create<HaplotypeFrequencySet>();
        frequencyCache.GetActiveHaplotypeFrequencySets().Returns(new Dictionary<(string, string), HaplotypeFrequencySet>
        {
            { (requested.RegistryCode, requested.EthnicityCode), activeForItsCodes }
        });

        var result = await service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing(), HaplotypeFrequencySetId = requested.Id });

        result.Subjects.Single().GenotypeSets.Should().OnlyContain(set =>
            set.HaplotypeFrequencySet!.Id == requested.Id && set.HaplotypeFrequencySet.IsActive == isActive);
        await frequencyLookupService.DidNotReceiveWithAnyArgs().GetSingleHaplotypeFrequencySet(default);
        await genotypeSetService.Received(4).GetGenotypeSet(
            Arg.Is<SubjectData>(subject => subject.SubjectFrequencySet.FrequencySet == requested),
            Arg.Any<MatchPredictionParameters>());
    }

    [Test]
    public async Task Compute_WithAFrequencySetIdThatDoesNotExist_ThrowsAValidationError_AndComputesNothing()
    {
        frequencySetReader.GetHaplotypeFrequencySet(default).ReturnsForAnyArgs((HaplotypeFrequencySet)null);

        var act = () => service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing(), HaplotypeFrequencySetId = fixture.Create<int>() });

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(GenotypeSetDebugRequest.HaplotypeFrequencySetId));
        await genotypeSetService.DidNotReceiveWithAnyArgs().GetGenotypeSet(default, default);
    }

    [Test]
    public async Task Compute_ForAnInvalidRequest_ThrowsAValidationError()
    {
        var act = () => service.Compute(new GenotypeSetDebugRequest());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task Compute_WhenNoDatabaseIsActive_ThrowsUnavailable()
    {
        historyRepository.GetActiveRecord().Returns((ActiveDataRefreshRecord)null);

        var act = () => service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing() });

        await act.Should().ThrowAsync<GenotypeSetDebugUnavailableException>();
    }

    [Test]
    public async Task Compute_ForDonors_ReadsTheirTypingAndCodesFromTheActiveDatabase_AndChoosesTheirSetFromTheirCodes()
    {
        var donor = NewDonor();
        GivenDonors(donor);

        var result = await service.Compute(new GenotypeSetDebugRequest { ExternalDonorCodes = [donor.ExternalDonorCode] });

        repositoryFactory.Received().GetDonorInspectionRepositoryForDatabase(activeRecord.Database);
        var subject = result.Subjects.Should().ContainSingle().Which;
        subject.Should().BeEquivalentTo(new
        {
            donor.ExternalDonorCode,
            donor.DonorId,
            Found = true,
            donor.IsAvailableForSearch,
            donor.RegistryCode,
            donor.EthnicityCode,
            HlaTyping = donor.HlaNames
        });
        subject.GenotypeSets.Should().HaveCount(4);
        await frequencyLookupService.Received().GetSingleHaplotypeFrequencySet(Arg.Is<FrequencySetMetadata>(metadata =>
            metadata.RegistryCode == donor.RegistryCode && metadata.EthnicityCode == donor.EthnicityCode));
        await genotypeSetService.Received(4).GetGenotypeSet(
            Arg.Is<SubjectData>(subjectData => subjectData.HlaTyping.Equals(donor.HlaNames)),
            Arg.Any<MatchPredictionParameters>());
    }

    [Test]
    public async Task Compute_ForADonorCodeThatNoDonorHas_GivesASubjectThatIsNotFound()
    {
        var code = fixture.Create<string>();

        var result = await service.Compute(new GenotypeSetDebugRequest { ExternalDonorCodes = [code] });

        var subject = result.Subjects.Should().ContainSingle().Which;
        subject.ExternalDonorCode.Should().Be(code);
        subject.Found.Should().BeFalse();
        subject.GenotypeSets.Should().BeEmpty();
        await genotypeSetService.DidNotReceiveWithAnyArgs().GetGenotypeSet(default, default);
    }

    [Test]
    public async Task Compute_FindsADonorWhoseCodeDiffersOnlyInCase()
    {
        // The database compares codes without regard to case, so it returns the donor for the request's code.
        var donor = NewDonor();
        GivenDonors(donor);

        var result = await service.Compute(new GenotypeSetDebugRequest { ExternalDonorCodes = [donor.ExternalDonorCode.ToLowerInvariant()] });

        result.Subjects.Should().ContainSingle().Which.Found.Should().BeTrue();
    }

    [Test]
    public async Task Compute_ForTwoDonorsWithOneTypingAndFrequencySet_ImputesEachCombinationOnce()
    {
        var donor = NewDonor();
        var sameTyping = NewDonor(donor.HlaNames);
        GivenDonors(donor, sameTyping);

        var result = await service.Compute(new GenotypeSetDebugRequest { ExternalDonorCodes = [donor.ExternalDonorCode, sameTyping.ExternalDonorCode] });

        await genotypeSetService.Received(4).GetGenotypeSet(Arg.Any<SubjectData>(), Arg.Any<MatchPredictionParameters>());
        result.Subjects.Should().HaveCount(2).And.OnlyContain(subject => subject.GenotypeSets.Count == 4);
    }

    [Test]
    public async Task Compute_ForAnEmptyHlaName_ImputesItAsNull()
    {
        // As the pre-computation does: the HLA Metadata Dictionary throws for an empty name.
        var typing = new PhenotypeInfoBuilder<string>(Typing()).WithDataAt(Locus.C, string.Empty, string.Empty).Build();
        var expected = new PhenotypeInfoBuilder<string>(typing).WithDataAt(Locus.C, null, null).Build();

        await service.Compute(new GenotypeSetDebugRequest { HlaTyping = typing, AllowedLociKeys = [AllowedLociKey.ABCDrb1Dqb1] });

        await genotypeSetService.Received(1).GetGenotypeSet(
            Arg.Is<SubjectData>(subject => subject.HlaTyping.Equals(expected)),
            Arg.Any<MatchPredictionParameters>());
    }

    [Test]
    public async Task Compute_WhenOneComputationFails_GivesTheErrorForThatSetOnly()
    {
        var message = fixture.Create<string>();
        genotypeSetService
            .GetGenotypeSet(Arg.Any<SubjectData>(), Arg.Is<MatchPredictionParameters>(parameters => parameters.AllowedLoci.SetEquals(AllowedLociKey.ABDrb1.ToLoci())))
            .ThrowsAsync(new InvalidOperationException(message));

        var result = await service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing() });

        var sets = result.Subjects.Single().GenotypeSets;
        sets.Single(set => set.AllowedLociKey == AllowedLociKey.ABDrb1).Error.Should().Contain(message);
        sets.Where(set => set.AllowedLociKey != AllowedLociKey.ABDrb1).Should().HaveCount(3).And.OnlyContain(set => set.Error == null);
    }

    [Test]
    public async Task Compute_WhenTheFrequencySetOfOneDonorCannotBeChosen_GivesTheErrorForThatDonorOnly()
    {
        var failing = NewDonor();
        var other = NewDonor();
        GivenDonors(failing, other);
        var message = fixture.Create<string>();
        frequencyLookupService
            .GetSingleHaplotypeFrequencySet(Arg.Is<FrequencySetMetadata>(metadata => metadata.RegistryCode == failing.RegistryCode))
            .ThrowsAsync(new Exception(message));

        var result = await service.Compute(new GenotypeSetDebugRequest { ExternalDonorCodes = [failing.ExternalDonorCode, other.ExternalDonorCode] });

        var failed = result.Subjects.Single(subject => subject.ExternalDonorCode == failing.ExternalDonorCode);
        failed.Error.Should().Contain(message);
        failed.GenotypeSets.Should().BeEmpty();
        result.Subjects.Single(subject => subject.ExternalDonorCode == other.ExternalDonorCode).GenotypeSets.Should().HaveCount(4);
    }

    [Test]
    public async Task Compute_ForARepresentedSet_GivesItsSummaryAndTheSizeOfThePayloadThatThePreComputationStores()
    {
        var computed = RepresentedSet();
        genotypeSetService.GetGenotypeSet(default, default).ReturnsForAnyArgs(computed);

        var result = await service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing(), AllowedLociKeys = [AllowedLociKey.ABDrb1] });

        result.Subjects.Single().GenotypeSets.Single().Should().BeEquivalentTo(new
        {
            IsUnrepresented = false,
            GenotypeCount = computed.Genotypes.Count,
            computed.SumOfLikelihoods,
            PayloadSizeBytes = SubjectGenotypeSetPayload.Encode(computed).Length
        });
    }

    [Test]
    public async Task Compute_ForAnUnrepresentedSet_GivesNoPayloadSize()
    {
        genotypeSetService.GetGenotypeSet(default, default).ReturnsForAnyArgs(new SubjectGenotypeSet(true, [], 0m));

        var result = await service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing(), AllowedLociKeys = [AllowedLociKey.ABDrb1] });

        var set = result.Subjects.Single().GenotypeSets.Single();
        set.IsUnrepresented.Should().BeTrue();
        set.GenotypeCount.Should().Be(0);
        set.PayloadSizeBytes.Should().BeNull();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Compute_KeepsTheGenotypesOnlyWhenTheRequestIncludesThem(bool includeGenotypes)
    {
        var computed = RepresentedSet();
        genotypeSetService.GetGenotypeSet(default, default).ReturnsForAnyArgs(computed);

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            HlaTyping = Typing(),
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            IncludeGenotypes = includeGenotypes
        });

        var genotypes = result.Subjects.Single().GenotypeSets.Single().Genotypes;
        if (includeGenotypes)
        {
            genotypes.Should().BeEquivalentTo(computed.Genotypes);
        }
        else
        {
            genotypes.Should().BeNull();
        }
    }

    [Test]
    public async Task Compute_WithoutSave_WritesNothing()
    {
        GivenDonors(NewDonor());

        await service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing() });

        repositoryFactory.DidNotReceiveWithAnyArgs().GetSubjectGenotypeSetRepositoryForDatabase(default);
    }

    [Test]
    public async Task Compute_WithSave_WhenWritesAreNotAllowed_ThrowsBeforeItComputesOrWrites()
    {
        debugSettings.AllowPrecomputeWrites = false;

        var act = () => service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing(), Save = true });

        await act.Should().ThrowAsync<PrecomputeWritesNotAllowedException>();
        await genotypeSetService.DidNotReceiveWithAnyArgs().GetGenotypeSet(default, default);
        repositoryFactory.DidNotReceiveWithAnyArgs().GetSubjectGenotypeSetRepositoryForDatabase(default);
    }

    [Test]
    public async Task Compute_WithSave_StoresTheComputedValuesInTheActiveDatabase_AsThePreComputationDoes()
    {
        var computed = RepresentedSet();
        genotypeSetService.GetGenotypeSet(default, default).ReturnsForAnyArgs(computed);
        IReadOnlyCollection<SubjectGenotypeSetValueToStore> stored = null;
        genotypeSetRepository
            .When(repository => repository.GetOrCreateValueIds(Arg.Any<IReadOnlyCollection<SubjectGenotypeSetValueToStore>>()))
            .Do(callInfo => stored = callInfo.Arg<IReadOnlyCollection<SubjectGenotypeSetValueToStore>>());
        var typing = Typing();

        var result = await service.Compute(new GenotypeSetDebugRequest { HlaTyping = typing, AllowedLociKeys = [AllowedLociKey.ABCDrb1], Save = true });

        repositoryFactory.Received().GetSubjectGenotypeSetRepositoryForDatabase(activeRecord.Database);
        repositoryFactory.DidNotReceive().GetSubjectGenotypeSetRepositoryForDatabase(activeRecord.Database.Other());
        var value = stored.Should().ContainSingle().Which;
        value.Key.Should().Be(new SubjectGenotypeSetKey(
            SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(typing, AllowedLociKey.ABCDrb1),
            searchFrequencySet.Id,
            AllowedLociKey.ABCDrb1));
        value.IsUnrepresented.Should().BeFalse();
        value.SubjectGenotypeSetData.Should().Equal(SubjectGenotypeSetPayload.Encode(computed));
        result.Subjects.Single().GenotypeSets.Single().Saved.Should().NotBeNull();
    }

    [Test]
    public async Task Compute_WithSave_ForAnUnrepresentedSet_StoresTheFlagAndNoPayload()
    {
        genotypeSetService.GetGenotypeSet(default, default).ReturnsForAnyArgs(new SubjectGenotypeSet(true, [], 0m));
        IReadOnlyCollection<SubjectGenotypeSetValueToStore> stored = null;
        genotypeSetRepository
            .When(repository => repository.GetOrCreateValueIds(Arg.Any<IReadOnlyCollection<SubjectGenotypeSetValueToStore>>()))
            .Do(callInfo => stored = callInfo.Arg<IReadOnlyCollection<SubjectGenotypeSetValueToStore>>());

        await service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing(), AllowedLociKeys = [AllowedLociKey.ABDrb1], Save = true });

        stored.Should().ContainSingle().Which.Should().Match<SubjectGenotypeSetValueToStore>(value =>
            value.IsUnrepresented && value.SubjectGenotypeSetData == null);
    }

    [Test]
    public async Task Compute_WithSave_SaysWhichValueRowsWereStoredBefore()
    {
        var storedBeforeId = fixture.Create<int>();
        genotypeSetRepository.GetExistingValueIds(default).ReturnsForAnyArgs(callInfo =>
            callInfo.Arg<IReadOnlyCollection<SubjectGenotypeSetKey>>()?
                .Where(key => key.AllowedLociKey == AllowedLociKey.ABDrb1)
                .ToDictionary(key => key, _ => storedBeforeId)
            ?? new Dictionary<SubjectGenotypeSetKey, int>());

        var result = await service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing(), Save = true });

        var sets = result.Subjects.Single().GenotypeSets;
        sets.Single(set => set.AllowedLociKey == AllowedLociKey.ABDrb1).Saved!.Outcome.Should().Be(SavedValueOutcome.AlreadyExisted);
        sets.Where(set => set.AllowedLociKey != AllowedLociKey.ABDrb1).Should().OnlyContain(set => set.Saved!.Outcome == SavedValueOutcome.Created);
    }

    [Test]
    public async Task Compute_WithSave_ForDonors_WritesTheLinkRowsOfEachDonorToItsValues()
    {
        var donors = new[] { NewDonor(), NewDonor() };
        GivenDonors(donors);
        IReadOnlyCollection<DonorSubjectGenotypeSetAssignment> assignments = null;
        genotypeSetRepository
            .When(repository => repository.UpsertDonorAssignments(Arg.Any<IReadOnlyCollection<DonorSubjectGenotypeSetAssignment>>()))
            .Do(callInfo => assignments = callInfo.Arg<IReadOnlyCollection<DonorSubjectGenotypeSetAssignment>>());

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = donors.Select(donor => donor.ExternalDonorCode).ToList(),
            Save = true
        });

        result.Subjects.Should().OnlyContain(subject => subject.LinkRowsWritten == true);
        var expected = result.Subjects.SelectMany(subject => subject.GenotypeSets.Select(set =>
            new DonorSubjectGenotypeSetAssignment(subject.DonorId!.Value, set.AllowedLociKey, set.Saved!.SubjectGenotypeSetValueId)));
        assignments.Should().BeEquivalentTo(expected);
        assignments.Should().HaveCount(8);
    }

    [Test]
    public async Task Compute_WithSave_ForADonorWithAValueThatFailed_StoresItsOtherValues_ButWritesNoLinkRowsForIt()
    {
        // As the pre-computation does: a donor with only some of its rows would be searched with a mix of both paths.
        var failing = NewDonor();
        var other = NewDonor();
        GivenDonors(failing, other);
        genotypeSetService
            .GetGenotypeSet(
                Arg.Is<SubjectData>(subject => subject.HlaTyping.Equals(failing.HlaNames)),
                Arg.Is<MatchPredictionParameters>(parameters => parameters.AllowedLoci.SetEquals(AllowedLociKey.ABDrb1.ToLoci())))
            .ThrowsAsync(new InvalidOperationException(fixture.Create<string>()));
        IReadOnlyCollection<DonorSubjectGenotypeSetAssignment> assignments = null;
        genotypeSetRepository
            .When(repository => repository.UpsertDonorAssignments(Arg.Any<IReadOnlyCollection<DonorSubjectGenotypeSetAssignment>>()))
            .Do(callInfo => assignments = callInfo.Arg<IReadOnlyCollection<DonorSubjectGenotypeSetAssignment>>());

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = [failing.ExternalDonorCode, other.ExternalDonorCode],
            Save = true
        });

        var failedSubject = result.Subjects.Single(subject => subject.ExternalDonorCode == failing.ExternalDonorCode);
        failedSubject.LinkRowsWritten.Should().BeFalse();
        failedSubject.GenotypeSets.Count(set => set.Saved != null).Should().Be(3);
        result.Subjects.Single(subject => subject.ExternalDonorCode == other.ExternalDonorCode).LinkRowsWritten.Should().BeTrue();
        assignments.Should().HaveCount(4).And.OnlyContain(assignment => assignment.DonorId == other.DonorId);
    }

    [Test]
    public async Task Compute_WithSave_ForATyping_WritesNoLinkRows()
    {
        var result = await service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing(), Save = true });

        await genotypeSetRepository.DidNotReceiveWithAnyArgs().UpsertDonorAssignments(default);
        result.Subjects.Single().LinkRowsWritten.Should().BeNull();
    }

    [Test]
    public async Task Compute_WithSave_WhenARefreshMakesTheDatabaseDormantDuringTheRequest_WritesNothing()
    {
        historyRepository.GetActiveRecord().Returns(activeRecord, activeRecord with { Id = activeRecord.Id + 1, Database = activeRecord.Database.Other() });

        var result = await service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing(), Save = true });

        await genotypeSetRepository.DidNotReceiveWithAnyArgs().GetOrCreateValueIds(default);
        result.Subjects.Single().Error.Should().NotBeNull();
        result.Subjects.Single().GenotypeSets.Should().OnlyContain(set => set.Saved == null);
    }

    [Test]
    public async Task Compute_WithSave_WhenTheStoreFails_GivesTheErrorAndLogsIt_ButStillGivesTheComputedSets()
    {
        var message = fixture.Create<string>();
        genotypeSetRepository.GetOrCreateValueIds(default).ThrowsAsyncForAnyArgs(new InvalidOperationException(message));

        var result = await service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing(), Save = true });

        var subject = result.Subjects.Single();
        subject.Error.Should().Contain(message);
        subject.GenotypeSets.Should().HaveCount(4).And.OnlyContain(set => set.Error == null && set.Saved == null);
        logger.Collector.GetSnapshot().Should().ContainSingle(log => log.Level == LogLevel.Error);
    }

    [Test]
    public async Task Compute_WithSave_LogsWhatItSaved()
    {
        var donor = NewDonor();
        GivenDonors(donor);

        await service.Compute(new GenotypeSetDebugRequest { ExternalDonorCodes = [donor.ExternalDonorCode], Save = true });

        var log = logger.Collector.GetSnapshot().Should().ContainSingle().Which;
        log.Level.Should().Be(LogLevel.Information);
        log.GetStructuredStateValue("Database").Should().Be(activeRecord.Database.ToString());
        log.GetStructuredStateValue("CreatedValueCount").Should().Be("4");
        log.GetStructuredStateValue("LinkedDonorCount").Should().Be("1");
        log.GetStructuredStateValue("ExternalDonorCodes").Should().Be(donor.ExternalDonorCode);
    }

    private void GivenDonors(params DonorInfo[] donors) =>
        donorRepository.GetDonorsByExternalDonorCodes(default).ReturnsForAnyArgs(donors);

    /// <summary>A donor with its own codes, and its own typing unless one is given.</summary>
    private DonorInfo NewDonor(PhenotypeInfo<string> typing = null) => new()
    {
        DonorId = fixture.Create<int>(),
        ExternalDonorCode = fixture.Create<string>().ToUpperInvariant(),
        RegistryCode = fixture.Create<string>(),
        EthnicityCode = fixture.Create<string>(),
        IsAvailableForSearch = fixture.Create<bool>(),
        HlaNames = typing ?? Typing()
    };

    private PhenotypeInfo<string> Typing()
    {
        var builder = new PhenotypeInfoBuilder<string>();
        foreach (var locus in new[] { Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1 })
        {
            builder = builder.WithDataAt(locus, fixture.Create<string>(), fixture.Create<string>());
        }

        return builder.Build();
    }

    private SubjectGenotypeSet RepresentedSet()
    {
        var genotypes = fixture.CreateMany<decimal>(3)
            .Select(likelihood => new GenotypeAtDesiredResolutions(new ImputedGenotype(null, Typing(), likelihood), Typing()))
            .ToList();

        return new SubjectGenotypeSet(false, genotypes, fixture.Create<decimal>());
    }
}
