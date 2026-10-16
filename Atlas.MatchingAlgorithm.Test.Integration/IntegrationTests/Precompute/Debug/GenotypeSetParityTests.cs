using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Services.Debug.Precompute;
using Atlas.MatchingAlgorithm.Services.Search.Precompute;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Repositories;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Services.Precompute;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using ContextFactory = Atlas.MatchingAlgorithm.Data.Context.ContextFactory;
using DonorType = Atlas.MatchingAlgorithm.Client.Models.Donors.DonorType;
using Injection = Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection.DependencyInjection;
using SearchAlgorithmContext = Atlas.MatchingAlgorithm.Data.Context.SearchAlgorithmContext;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Precompute.Debug;

/// <summary>
/// The parity check of the genotype set compute debug endpoint (<c>CompareWithStored</c>, ATL-420) against real matching
/// databases: one test for each acceptance criterion of the ticket. The rows are stored by the real writers of the
/// pre-computation and of search. The Match Prediction pipeline (imputation and the frequency set lookup) is substituted,
/// as in <see cref="GenotypeSetDebugServiceTests"/>. The substitute of the engine gives one set for each input, so a row
/// gives <c>Match</c> only when its writer and the endpoint give the engine the same input.
/// </summary>
[TestFixture]
internal class GenotypeSetParityTests
{
    private Fixture fixture;
    private IDataRefreshHistoryRepository historyRepository;
    private ActiveDataRefreshRecord activeRecord;
    private StaticallyChosenTransientSqlConnectionStringProviderFactory connectionStringFactory;
    private IStaticallyChosenDatabaseRepositoryFactory repositoryFactory;
    private HaplotypeFrequencySet frequencySet;
    private IHaplotypeFrequencyLookupService frequencyLookupService;
    private IGenotypeSetService genotypeSetService;
    private Dictionary<EngineInput, SubjectGenotypeSet> setsByInput;
    private bool addedADormantRecord;
    private List<int> dormantDonorIds;

    private IGenotypeSetDebugService service;

    [SetUp]
    public async Task SetUp()
    {
        fixture = new Fixture();
        dormantDonorIds = [];

        historyRepository = Injection.Provider.GetService<IDataRefreshHistoryRepository>();
        activeRecord = historyRepository.GetActiveRecord();
        connectionStringFactory = Injection.Provider.GetService<StaticallyChosenTransientSqlConnectionStringProviderFactory>();
        repositoryFactory = Injection.Provider.GetService<IStaticallyChosenDatabaseRepositoryFactory>();
        await TruncatePrecomputeTables(activeRecord.Database);

        frequencySet = fixture.Create<HaplotypeFrequencySet>();
        frequencyLookupService = Substitute.For<IHaplotypeFrequencyLookupService>();
        frequencyLookupService.GetSingleHaplotypeFrequencySet(default).ReturnsForAnyArgs(frequencySet);
        var frequencyCache = Substitute.For<IHaplotypeFrequencyCache>();
        frequencyCache.GetActiveHaplotypeFrequencySets().Returns(new Dictionary<(string, string), HaplotypeFrequencySet>());

        // One set for each input of the engine, the same at every call: the typing, the frequency set, the nomenclature
        // version and the loci. The sets of one combination are unrepresented. The arguments are null only when a test sets
        // up the substitute again: that is not a call.
        setsByInput = new Dictionary<EngineInput, SubjectGenotypeSet>();
        genotypeSetService = Substitute.For<IGenotypeSetService>();
        genotypeSetService.GetGenotypeSet(default, default).ReturnsForAnyArgs(callInfo =>
            callInfo.Arg<SubjectData>() is { } subject && callInfo.Arg<MatchPredictionParameters>() is { } parameters
                ? SetFor(new EngineInput(
                    subject.HlaTyping,
                    subject.SubjectFrequencySet.FrequencySet.Id,
                    parameters.MatchingAlgorithmHlaNomenclatureVersion,
                    AllowedLociKeyExtensions.ToAllowedLociKey(parameters.AllowedLoci.ToHashSet())))
                : null);

        service = new GenotypeSetDebugService(
            historyRepository,
            repositoryFactory,
            frequencyLookupService,
            frequencyCache,
            Substitute.For<IHaplotypeFrequencySetReader>(),
            genotypeSetService,
            new DebugSettings(),
            NullLogger<GenotypeSetDebugService>.Instance);
    }

    /// <summary>Leaves the dormant database and the refresh records as the other tests expect them.</summary>
    [TearDown]
    public async Task TearDown()
    {
        if (dormantDonorIds.Count > 0)
        {
            var dormantDatabase = activeRecord.Database.Other();
            await TruncatePrecomputeTables(dormantDatabase);
            await using var context = Context(dormantDatabase);
            await context.Donors.Where(donor => dormantDonorIds.Contains(donor.DonorId)).ExecuteDeleteAsync();
        }

        if (addedADormantRecord)
        {
            await Injection.Provider.GetService<ITestDataRefreshHistoryRepository>().RemoveAllDataRefreshRecords();
            IntegrationTestSetUp.RunInitialDataRefresh();
            addedADormantRecord = false;
        }
    }

    /// <summary>Criterion 1: donors whose rows the pre-computation stored with the current set and version.</summary>
    [Test]
    public async Task CompareWithStored_ForDonorsStoredByThePreComputation_GivesMatchForEveryRow()
    {
        var donors = new[] { await InsertDonor(activeRecord.Database), await InsertDonor(activeRecord.Database) };
        await Precompute(donors, activeRecord.Database, activeRecord.HlaNomenclatureVersion);

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = donors.Select(donor => donor.ExternalDonorCode).ToList(),
            CompareWithStored = true
        });

        ParityChecks(result).Should().HaveCount(8).And.OnlyContain(check => check.Result == ParityResult.Match);
        result.Parity!.ResultCounts[ParityResult.Match].Should().Be(8);
        result.Parity.DonorsNotFound.Should().Be(0);
    }

    /// <summary>Criterion 2: a patient's row, stored as the warm step of search stores it.</summary>
    [Test]
    public async Task CompareWithStored_ForAPatientStoredByTheWarmStepOfSearch_GivesMatch()
    {
        var patientTyping = Typing();
        var searchRepositoryFactory = new SubjectGenotypeSetRepositoryFactory(connectionStringFactory);
        var reader = new PrecomputedDonorGenotypeSetReader(historyRepository, searchRepositoryFactory);
        var writer = new PrecomputedDonorGenotypeSetWriter(historyRepository, searchRepositoryFactory);
        foreach (var allowedLociKey in AllowedLociKeyExtensions.All)
        {
            var lookup = await reader.FindPatientGenotypeSet(patientTyping, frequencySet.Id, allowedLociKey.ToLoci(), activeRecord.Id);

            // The input of the warm step (GenotypeSetService.GetPatientGenotypeSet): the patient's typing and frequency set,
            // the nomenclature version that matching used, and the loci of the search.
            var set = SetFor(new EngineInput(patientTyping, frequencySet.Id, activeRecord.HlaNomenclatureVersion, allowedLociKey));
            var stored = await writer.StorePatientGenotypeSet(
                lookup.Key,
                set.IsUnrepresented,
                set.IsUnrepresented ? null : SubjectGenotypeSetPayload.Encode(set),
                activeRecord.Id);
            stored.Should().BeTrue();
        }

        var result = await service.Compute(new GenotypeSetDebugRequest { HlaTyping = patientTyping, CompareWithStored = true });

        ParityChecks(result).Should().HaveCount(4).And.OnlyContain(check => check.Result == ParityResult.Match);
    }

    /// <summary>Criterion 3: a stored payload with a different likelihood.</summary>
    [Test]
    public async Task CompareWithStored_ForAStoredPayloadWithAChangedLikelihood_GivesMismatchWithTheDifference()
    {
        var donor = await InsertDonor(activeRecord.Database);
        await Precompute([donor], activeRecord.Database, activeRecord.HlaNomenclatureVersion);
        var live = SetFor(new EngineInput(donor.Typing, frequencySet.Id, activeRecord.HlaNomenclatureVersion, AllowedLociKey.ABDrb1));
        var changed = live.Genotypes.First();
        var storedLikelihood = fixture.Create<decimal>();
        await UpdateStoredPayload(donor, AllowedLociKey.ABDrb1, _ => SubjectGenotypeSetPayload.Encode(new SubjectGenotypeSet(
            false,
            live.Genotypes.Select(genotype => genotype == changed ? WithLikelihood(genotype, storedLikelihood) : genotype).ToList(),
            live.SumOfLikelihoods)));

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = [donor.ExternalDonorCode],
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            CompareWithStored = true
        });

        var check = ParityChecks(result).Should().ContainSingle().Which;
        check.Result.Should().Be(ParityResult.Mismatch);
        check.DifferenceCount.Should().Be(1);
        check.Differences.Should().ContainSingle().Which.Should().BeEquivalentTo(new GenotypeSetDifference(
            GenotypeSetDifferenceKind.GenotypeLikelihood,
            changed.StringMatchableResolution,
            changed.HaplotypeResolution,
            changed.GenotypeLikelihood.ToString(CultureInfo.InvariantCulture),
            storedLikelihood.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// Criterion 4: a new frequency set is imported and activated for the donor's codes after the store. The lookup gives
    /// the new set, as search would: this project has no Match Prediction database to import into.
    /// </summary>
    [Test]
    public async Task CompareWithStored_AfterANewFrequencySetIsActivatedForTheDonor_GivesStaleFrequencySetWithBothIds()
    {
        var donor = await InsertDonor(activeRecord.Database);
        await Precompute([donor], activeRecord.Database, activeRecord.HlaNomenclatureVersion);
        var newSet = fixture.Create<HaplotypeFrequencySet>();
        frequencyLookupService.GetSingleHaplotypeFrequencySet(default).ReturnsForAnyArgs(newSet);

        var result = await service.Compute(new GenotypeSetDebugRequest { ExternalDonorCodes = [donor.ExternalDonorCode], CompareWithStored = true });

        ParityChecks(result).Should().HaveCount(4).And.OnlyContain(check =>
            check.Result == ParityResult.StaleFrequencySet
            && check.StoredHaplotypeFrequencySetId == frequencySet.Id
            && check.SearchHaplotypeFrequencySetId == newSet.Id);
    }

    /// <summary>Criterion 5: a link row that points at the value row of another typing.</summary>
    [Test]
    public async Task CompareWithStored_ForALinkRowThatPointsAtTheRowOfAnotherTyping_GivesTypingChangedWithBothKeys()
    {
        var donor = await InsertDonor(activeRecord.Database);
        var other = await InsertDonor(activeRecord.Database);
        await Precompute([donor, other], activeRecord.Database, activeRecord.HlaNomenclatureVersion);
        var repository = repositoryFactory.GetSubjectGenotypeSetRepositoryForDatabase(activeRecord.Database);
        var otherKey = new SubjectGenotypeSetKey(Key(other.Typing, AllowedLociKey.ABDrb1), frequencySet.Id, AllowedLociKey.ABDrb1);
        var otherValueId = (await repository.GetExistingValueIds([otherKey]))[otherKey];
        await repository.UpsertDonorAssignments([new DonorSubjectGenotypeSetAssignment(donor.DonorId, AllowedLociKey.ABDrb1, otherValueId)]);

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = [donor.ExternalDonorCode],
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            CompareWithStored = true
        });

        var set = result.Subjects.Single().GenotypeSets.Single();
        set.HlaTypingKey.Should().Be(Key(donor.Typing, AllowedLociKey.ABDrb1));
        set.ParityCheck!.Result.Should().Be(ParityResult.TypingChanged);
        set.ParityCheck.StoredHlaTypingKey.Should().Be(otherKey.HlaTypingKey);
    }

    /// <summary>Criterion 6: missing rows.</summary>
    [Test]
    public async Task CompareWithStored_ForADonorWhoseLinkRowsAreRemoved_GivesNoStoredRow()
    {
        var donor = await InsertDonor(activeRecord.Database);
        await Precompute([donor], activeRecord.Database, activeRecord.HlaNomenclatureVersion);
        await repositoryFactory.GetSubjectGenotypeSetRepositoryForDatabase(activeRecord.Database).DeleteDonorAssignments([donor.DonorId]);

        var result = await service.Compute(new GenotypeSetDebugRequest { ExternalDonorCodes = [donor.ExternalDonorCode], CompareWithStored = true });

        ParityChecks(result).Should().HaveCount(4).And.OnlyContain(check => check.Result == ParityResult.NoStoredRow);
    }

    /// <summary>Criterion 6: a damaged payload.</summary>
    [Test]
    public async Task CompareWithStored_ForADamagedPayload_GivesDecodeFailedWithTheError()
    {
        var donor = await InsertDonor(activeRecord.Database);
        await Precompute([donor], activeRecord.Database, activeRecord.HlaNomenclatureVersion);
        await UpdateStoredPayload(donor, AllowedLociKey.ABDrb1, _ => fixture.CreateMany<byte>(3).ToArray());

        var result = await service.Compute(new GenotypeSetDebugRequest { ExternalDonorCodes = [donor.ExternalDonorCode], CompareWithStored = true });

        var sets = result.Subjects.Single().GenotypeSets;
        var damaged = sets.Single(set => set.AllowedLociKey == AllowedLociKey.ABDrb1).ParityCheck;
        damaged!.Result.Should().Be(ParityResult.DecodeFailed);
        damaged.DecodeError.Should().NotBeNullOrEmpty();
        sets.Where(set => set.AllowedLociKey != AllowedLociKey.ABDrb1).Should().OnlyContain(set => set.ParityCheck!.Result == ParityResult.Match);
    }

    /// <summary>Criterion 6: an error in one row does not fail the request.</summary>
    [Test]
    public async Task CompareWithStored_WhenTheImputationOfOneDonorFails_GivesErrorForThatDonorsRowsOnly()
    {
        var failing = await InsertDonor(activeRecord.Database);
        var other = await InsertDonor(activeRecord.Database);
        await Precompute([failing, other], activeRecord.Database, activeRecord.HlaNomenclatureVersion);
        var message = fixture.Create<string>();
        genotypeSetService
            .GetGenotypeSet(Arg.Is<SubjectData>(subject => subject.HlaTyping.Equals(failing.Typing)), Arg.Any<MatchPredictionParameters>())
            .ThrowsAsync(new InvalidOperationException(message));

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = [failing.ExternalDonorCode, other.ExternalDonorCode],
            CompareWithStored = true
        });

        var failedChecks = result.Subjects.Single(subject => subject.DonorId == failing.DonorId).GenotypeSets.Select(set => set.ParityCheck);
        failedChecks.Should().HaveCount(4).And.OnlyContain(check => check!.Result == ParityResult.Error && check.Error!.Contains(message));
        var otherChecks = result.Subjects.Single(subject => subject.DonorId == other.DonorId).GenotypeSets.Select(set => set.ParityCheck);
        otherChecks.Should().HaveCount(4).And.OnlyContain(check => check!.Result == ParityResult.Match);
    }

    /// <summary>Criterion 7: rows that a refresh wrote to the dormant database, before the databases swap.</summary>
    [Test]
    public async Task CompareWithStored_WithTheDormantDatabase_ReadsItsRows_AtTheVersionOfTheRefreshThatWroteThem()
    {
        var dormantDatabase = activeRecord.Database.Other();
        var dormantVersion = await AddARefreshThatWroteTo(dormantDatabase);
        await TruncatePrecomputeTables(dormantDatabase);
        var donor = await InsertDonor(dormantDatabase);
        await Precompute([donor], dormantDatabase, dormantVersion);
        genotypeSetService.ClearReceivedCalls();

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = [donor.ExternalDonorCode],
            Database = TransientDatabaseRole.Dormant,
            CompareWithStored = true
        });

        result.Database.Should().Be(dormantDatabase);
        result.HlaNomenclatureVersion.Should().Be(dormantVersion);
        result.Subjects.Single().Found.Should().BeTrue("the donor is only in the dormant database");
        ParityChecks(result).Should().HaveCount(4).And.OnlyContain(check => check.Result == ParityResult.Match);
        await genotypeSetService.Received(4).GetGenotypeSet(
            Arg.Any<SubjectData>(),
            Arg.Is<MatchPredictionParameters>(parameters => parameters.MatchingAlgorithmHlaNomenclatureVersion == dormantVersion));
    }

    /// <summary>The rows that the pre-computation of the data refresh and of the donor import stores for the donors.</summary>
    private async Task Precompute(
        IEnumerable<(int DonorId, string ExternalDonorCode, PhenotypeInfo<string> Typing)> donors,
        TransientDatabase database,
        string hlaNomenclatureVersion)
    {
        var precomputeService = new SubjectGenotypeSetPrecomputeService(
            new SubjectGenotypeSetValueService(repositoryFactory, genotypeSetService),
            repositoryFactory);

        await precomputeService.Precompute(
            donors.Select(donor => new PrecomputeSubject(donor.DonorId, donor.Typing, frequencySet)).ToList(),
            hlaNomenclatureVersion,
            database);
    }

    /// <summary>
    /// A closed refresh of the database that did not succeed, so that the active database does not change, and that wrote
    /// to it. Returns its nomenclature version.
    /// </summary>
    private async Task<string> AddARefreshThatWroteTo(TransientDatabase database)
    {
        var record = DataRefreshRecordBuilder.New
            .WithDatabase(database)
            .WithStagesCompletedUpToAndIncluding(DataRefreshStage.DonorGenotypePrecomputation)
            .Build();
        record.HlaNomenclatureVersion = fixture.Create<string>();
        record.RefreshEndUtc = DateTime.UtcNow;

        addedADormantRecord = true;
        await Injection.Provider.GetService<ITestDataRefreshHistoryRepository>().Create(record);
        return record.HlaNomenclatureVersion;
    }

    /// <summary>Replaces the stored data of the donor's value row at the combination.</summary>
    private async Task UpdateStoredPayload(
        (int DonorId, string ExternalDonorCode, PhenotypeInfo<string> Typing) donor,
        AllowedLociKey allowedLociKey,
        Func<byte[], byte[]> update)
    {
        var typingKey = Key(donor.Typing, allowedLociKey);
        await using var context = Context(activeRecord.Database);
        var value = await context.SubjectGenotypeSetValues.SingleAsync(row =>
            row.HlaTypingKey == typingKey && row.HaplotypeFrequencySetId == frequencySet.Id && row.AllowedLociKey == allowedLociKey);
        value.SubjectGenotypeSetData = update(value.SubjectGenotypeSetData);
        await context.SaveChangesAsync();
    }

    private async Task<(int DonorId, string ExternalDonorCode, PhenotypeInfo<string> Typing)> InsertDonor(TransientDatabase database)
    {
        var donorId = DonorIdGenerator.NextId();
        var externalDonorCode = fixture.Create<string>();
        var typing = Typing();

        await using var context = Context(database);
        context.Donors.Add(new Donor
        {
            DonorId = donorId,
            DonorType = DonorType.Adult,
            IsAvailableForSearch = true,
            ExternalDonorCode = externalDonorCode,
            RegistryCode = fixture.Create<string>(),
            EthnicityCode = fixture.Create<string>(),
            A_1 = typing.A.Position1, A_2 = typing.A.Position2,
            B_1 = typing.B.Position1, B_2 = typing.B.Position2,
            C_1 = typing.C.Position1, C_2 = typing.C.Position2,
            DQB1_1 = typing.Dqb1.Position1, DQB1_2 = typing.Dqb1.Position2,
            DRB1_1 = typing.Drb1.Position1, DRB1_2 = typing.Drb1.Position2
        });
        await context.SaveChangesAsync();

        if (database != activeRecord.Database)
        {
            dormantDonorIds.Add(donorId);
        }

        return (donorId, externalDonorCode, typing);
    }

    /// <summary>The precompute tables only: the donors of each test have ids and codes of their own.</summary>
    private async Task TruncatePrecomputeTables(TransientDatabase database)
    {
        await using var context = Context(database);
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE DonorSubjectGenotypeSets; TRUNCATE TABLE SubjectGenotypeSetValues;");
    }

    private SearchAlgorithmContext Context(TransientDatabase database) =>
        new ContextFactory().Create(connectionStringFactory.GenerateConnectionStringProvider(database).GetConnectionString());

    private static string Key(PhenotypeInfo<string> typing, AllowedLociKey allowedLociKey) =>
        SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(typing, allowedLociKey);

    private static IReadOnlyList<GenotypeSetParityCheck> ParityChecks(GenotypeSetDebugResult result) =>
        result.Subjects.SelectMany(subject => subject.GenotypeSets).Select(set => set.ParityCheck).ToList();

    private static GenotypeAtDesiredResolutions WithLikelihood(GenotypeAtDesiredResolutions genotype, decimal likelihood) =>
        new(new ImputedGenotype(null, genotype.HaplotypeResolution, likelihood), genotype.StringMatchableResolution);

    /// <summary>A typing with names of its own at every match prediction locus.</summary>
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

    /// <summary>The set that the engine gives for the input: made at the first call, and the same at every call after it.</summary>
    private SubjectGenotypeSet SetFor(EngineInput input)
    {
        if (setsByInput.TryGetValue(input, out var set))
        {
            return set;
        }

        set = input.AllowedLociKey == AllowedLociKey.ABCDrb1Dqb1 ? new SubjectGenotypeSet(true, [], 0m) : RepresentedSet();
        setsByInput[input] = set;
        return set;
    }

    /// <summary>What the engine computes a set from. The typing is compared by value, so a null name and an empty name differ.</summary>
    private readonly record struct EngineInput(
        PhenotypeInfo<string> Typing,
        int FrequencySetId,
        string HlaNomenclatureVersion,
        AllowedLociKey AllowedLociKey);
}
