using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Services.Debug.Precompute;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
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
using NUnit.Framework;
using ContextFactory = Atlas.MatchingAlgorithm.Data.Context.ContextFactory;
using DonorType = Atlas.MatchingAlgorithm.Client.Models.Donors.DonorType;
using Injection = Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection.DependencyInjection;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Precompute.Debug;

/// <summary>
/// The genotype set compute debug endpoint (ATL-454) against a real matching database. The Match Prediction pipeline
/// (imputation and the frequency set lookup) is substituted, as in <see cref="DonorImportPrecomputeTests"/>. Every read and
/// write of the matching databases is real.
/// </summary>
[TestFixture]
internal class GenotypeSetDebugServiceTests
{
    private Fixture fixture;
    private ActiveDataRefreshRecord activeRecord;
    private string activeConnectionString;
    private IStaticallyChosenDatabaseRepositoryFactory repositoryFactory;
    private HaplotypeFrequencySet frequencySet;
    private IGenotypeSetService genotypeSetService;
    private IHaplotypeFrequencySetReader frequencySetReader;

    private IGenotypeSetDebugService service;

    [SetUp]
    public async Task SetUp()
    {
        fixture = new Fixture();

        var historyRepository = Injection.Provider.GetService<IDataRefreshHistoryRepository>();
        activeRecord = historyRepository.GetActiveRecord();
        activeConnectionString = Injection.Provider.GetService<StaticallyChosenTransientSqlConnectionStringProviderFactory>()
            .GenerateConnectionStringProvider(activeRecord.Database)
            .GetConnectionString();

        // The precompute tables only: the donors of each test have ids and codes of their own.
        await using (var context = new ContextFactory().Create(activeConnectionString))
        {
            await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE DonorSubjectGenotypeSets; TRUNCATE TABLE SubjectGenotypeSetValues;");
        }

        repositoryFactory = Injection.Provider.GetService<IStaticallyChosenDatabaseRepositoryFactory>();

        frequencySet = fixture.Create<HaplotypeFrequencySet>();
        var frequencyLookupService = Substitute.For<IHaplotypeFrequencyLookupService>();
        frequencyLookupService.GetSingleHaplotypeFrequencySet(default).ReturnsForAnyArgs(frequencySet);
        var frequencyCache = Substitute.For<IHaplotypeFrequencyCache>();
        frequencyCache.GetActiveHaplotypeFrequencySets().Returns(new Dictionary<(string, string), HaplotypeFrequencySet>());
        frequencySetReader = Substitute.For<IHaplotypeFrequencySetReader>();

        // One set for each combination, the same at every call, as the engine gives for one typing and frequency set. One of
        // them is unrepresented.
        var setsByKey = AllowedLociKeyExtensions.All.ToDictionary(
            key => key,
            key => key == AllowedLociKey.ABCDrb1Dqb1 ? new SubjectGenotypeSet(true, [], 0m) : RepresentedSet());
        genotypeSetService = Substitute.For<IGenotypeSetService>();
        genotypeSetService.GetGenotypeSet(default, default).ReturnsForAnyArgs(callInfo =>
            setsByKey[AllowedLociKeyExtensions.ToAllowedLociKey(callInfo.Arg<MatchPredictionParameters>().AllowedLoci.ToHashSet())]);

        service = new GenotypeSetDebugService(
            historyRepository,
            repositoryFactory,
            frequencyLookupService,
            frequencyCache,
            frequencySetReader,
            genotypeSetService,
            new DebugSettings { AllowPrecomputeWrites = true },
            NullLogger<GenotypeSetDebugService>.Instance);
    }

    [Test]
    public async Task Compute_ForADonor_GivesTheKeyFrequencySetFlagAndSetThatThePreComputationStores()
    {
        var donor = await InsertDonor();
        var precomputeService = new SubjectGenotypeSetPrecomputeService(
            new SubjectGenotypeSetValueService(repositoryFactory, genotypeSetService),
            repositoryFactory);
        await precomputeService.Precompute(
            [new PrecomputeSubject(donor.DonorId, donor.Typing, frequencySet)],
            activeRecord.HlaNomenclatureVersion,
            activeRecord.Database);

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = [donor.ExternalDonorCode],
            IncludeGenotypes = true
        });

        var repository = repositoryFactory.GetSubjectGenotypeSetRepositoryForDatabase(activeRecord.Database);
        var sets = result.Subjects.Should().ContainSingle().Which.GenotypeSets;
        sets.Should().HaveCount(AllowedLociKeyExtensions.All.Count);
        foreach (var set in sets)
        {
            var stored = (await repository.GetDonorSubjectGenotypeSets([donor.DonorId], set.AllowedLociKey))[donor.DonorId];
            set.HlaTypingKey.Should().Be(stored.HlaTypingKey);
            set.HaplotypeFrequencySet!.Id.Should().Be(stored.HaplotypeFrequencySetId);
            set.IsUnrepresented.Should().Be(stored.IsUnrepresented);
            set.PayloadSizeBytes.Should().Be(stored.SubjectGenotypeSetData?.Length);

            if (!stored.IsUnrepresented)
            {
                var decoded = SubjectGenotypeSetPayload.Decode(stored.SubjectGenotypeSetData);
                set.SumOfLikelihoods.Should().Be(decoded.SumOfLikelihoods);
                set.Genotypes.Should().BeEquivalentTo(decoded.Genotypes);
            }
        }
    }

    [Test]
    public async Task Compute_WithSaveTwice_StoresOneValueRowPerKeyAndTheDonorsLinkRows_AndTheSecondCallFindsThem()
    {
        var donor = await InsertDonor();
        var request = new GenotypeSetDebugRequest { ExternalDonorCodes = [donor.ExternalDonorCode], Save = true };

        var first = await service.Compute(request);
        var second = await service.Compute(request);

        var firstSets = first.Subjects.Single().GenotypeSets;
        var secondSets = second.Subjects.Single().GenotypeSets;
        firstSets.Should().OnlyContain(set => set.Saved!.Outcome == SavedValueOutcome.Created);
        secondSets.Should().OnlyContain(set => set.Saved!.Outcome == SavedValueOutcome.AlreadyExisted);
        secondSets.Select(set => (set.AllowedLociKey, set.Saved!.SubjectGenotypeSetValueId))
            .Should().BeEquivalentTo(firstSets.Select(set => (set.AllowedLociKey, set.Saved!.SubjectGenotypeSetValueId)));
        first.Subjects.Single().LinkRowsWritten.Should().BeTrue();
        second.Subjects.Single().LinkRowsWritten.Should().BeTrue();

        (await ValueRowCount()).Should().Be(AllowedLociKeyExtensions.All.Count);
        (await LinkRows(donor.DonorId)).Should().BeEquivalentTo(
            firstSets.Select(set => (set.AllowedLociKey, set.Saved!.SubjectGenotypeSetValueId)));
    }

    /// <summary>How a tester stores a donor's rows against an older, inactive frequency set: search then sees a stale set.</summary>
    [Test]
    public async Task Compute_WithSaveAndAFrequencySetId_StoresTheDonorsRowsForThatSet()
    {
        var donor = await InsertDonor();
        var olderSet = fixture.Create<HaplotypeFrequencySet>();
        frequencySetReader.GetHaplotypeFrequencySet(olderSet.Id).Returns(olderSet);

        await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = [donor.ExternalDonorCode],
            HaplotypeFrequencySetId = olderSet.Id,
            Save = true
        });

        var repository = repositoryFactory.GetSubjectGenotypeSetRepositoryForDatabase(activeRecord.Database);
        foreach (var allowedLociKey in AllowedLociKeyExtensions.All)
        {
            var stored = await repository.GetDonorSubjectGenotypeSets([donor.DonorId], allowedLociKey);
            stored[donor.DonorId].HaplotypeFrequencySetId.Should().Be(olderSet.Id);
        }
    }

    private async Task<(int DonorId, string ExternalDonorCode, PhenotypeInfo<string> Typing)> InsertDonor()
    {
        var donorId = DonorIdGenerator.NextId();
        var externalDonorCode = fixture.Create<string>();
        var typing = Typing();

        await using var context = new ContextFactory().Create(activeConnectionString);
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

        return (donorId, externalDonorCode, typing);
    }

    private async Task<int> ValueRowCount()
    {
        await using var context = new ContextFactory().Create(activeConnectionString);
        return await context.SubjectGenotypeSetValues.CountAsync();
    }

    private async Task<List<(AllowedLociKey, int)>> LinkRows(int donorId)
    {
        await using var context = new ContextFactory().Create(activeConnectionString);
        var rows = await context.DonorSubjectGenotypeSets.Where(row => row.DonorId == donorId).ToListAsync();
        return rows.Select(row => (row.AllowedLociKey, row.SubjectGenotypeSetValueId)).ToList();
    }

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
}
