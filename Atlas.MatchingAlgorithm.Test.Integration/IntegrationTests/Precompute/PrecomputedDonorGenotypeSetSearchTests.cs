using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Services.Search.Precompute;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Services.Precompute;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using ContextFactory = Atlas.MatchingAlgorithm.Data.Context.ContextFactory;
using DonorType = Atlas.MatchingAlgorithm.Client.Models.Donors.DonorType;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;
using Injection = Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection.DependencyInjection;
using SearchAlgorithmContext = Atlas.MatchingAlgorithm.Data.Context.SearchAlgorithmContext;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Precompute;

/// <summary>
/// How search uses a donor's stored genotype set (ATL-221) when the row is good, missing, for another typing, or cannot be
/// decoded (ATL-493). The stored rows are real rows in the active matching database, and the batch runs through the real
/// path: the source resolver and reader, the match probability service, and the batch completer with the real writer.
/// Only the Match Prediction engine (the genotype matcher and the frequency set lookup) is substituted: this project has
/// no Match Prediction database.
/// </summary>
[TestFixture]
internal class PrecomputedDonorGenotypeSetSearchTests
{
    private const AllowedLociKey Key = AllowedLociKey.ABCDrb1Dqb1;
    private const int FrequencySetId = 456;
    private const string RegistryCode = "reg";
    private const string EthnicityCode = "eth";

    private ActiveDataRefreshRecord activeRecord;
    private StaticallyChosenTransientSqlConnectionStringProviderFactory connectionStringFactory;
    private ISubjectGenotypeSetRepository repository;
    private PrecomputedDonorGenotypeSetWriter writer;
    private IDonorGenotypeSetSourceResolver resolver;
    private IMatchProbabilityService matchProbabilityService;
    private IDonorGenotypeSetBatchCompleter completer;
    private IGenotypeMatcher genotypeMatcher;
    private Dictionary<string, double> usageMetrics;
    private SubjectGenotypeSet storedSet;
    private SubjectGenotypeSet liveSet;

    [SetUp]
    public async Task SetUp()
    {
        var historyRepository = Injection.Provider.GetService<IDataRefreshHistoryRepository>();
        activeRecord = historyRepository.GetActiveRecord();
        connectionStringFactory = Injection.Provider.GetService<StaticallyChosenTransientSqlConnectionStringProviderFactory>();
        var repositoryFactory = new SubjectGenotypeSetRepositoryFactory(connectionStringFactory);
        repository = repositoryFactory.GetForDatabase(activeRecord.Database);
        await TruncatePrecomputeTables();

        writer = new PrecomputedDonorGenotypeSetWriter(historyRepository, repositoryFactory);
        var logger = Substitute.For<IAtlasLogger>();
        logger.When(l => l.SendEvent(
                DonorGenotypeSetBatchCompleter.UsageEventName,
                Arg.Any<LogLevel>(),
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<Dictionary<string, double>>()))
            .Do(call => usageMetrics = call.ArgAt<Dictionary<string, double>>(3));
        resolver = new DonorGenotypeSetSourceResolver(
            new PrecomputedGenotypeSetSettings { Mode = PrecomputedGenotypeSetMode.DefaultPrecomputed },
            new PrecomputedDonorGenotypeSetReader(historyRepository, repositoryFactory),
            logger);
        completer = new DonorGenotypeSetBatchCompleter(writer, logger);

        storedSet = SetWithGenotypes(2, 0.5m);
        liveSet = SetWithGenotypes(3, 0.6m);

        var haplotypeFrequencyService = Substitute.For<IHaplotypeFrequencyService>();
        haplotypeFrequencyService.GetHaplotypeFrequencySets(default, default).ReturnsForAnyArgs(new HaplotypeFrequencySetResponse
        {
            PatientSet = new HaplotypeFrequencySet { Id = FrequencySetId },
            DonorSet = new HaplotypeFrequencySet { Id = FrequencySetId }
        });

        // Like the real matcher: uses the supplied donor set, or "computes" the live one.
        genotypeMatcher = Substitute.For<IGenotypeMatcher>();
        genotypeMatcher.MatchPatientDonorGenotypes(default).ReturnsForAnyArgs(call =>
        {
            var donorSet = call.Arg<GenotypeMatcherInput>().DonorGenotypeSet ?? liveSet;
            return new GenotypeMatcherResult
            {
                PatientResult = new GenotypeMatcherResult.SubjectResult(false, 1, 0.1m),
                DonorResult = new GenotypeMatcherResult.SubjectResult(donorSet.IsUnrepresented, donorSet.Genotypes.Count, donorSet.SumOfLikelihoods),
                GenotypeMatchDetails = new List<GenotypeMatchDetails>(),
                DonorGenotypeSet = donorSet
            };
        });

        var calculator = Substitute.For<IMatchProbabilityCalculator>();
        calculator.CalculateMatchProbability(default, default, default, default).ReturnsForAnyArgs(new MatchProbabilityResponse());

        // The genotype set service is only asked whether a donor with no row is unambiguous: the substitute says it is not.
        matchProbabilityService = new MatchProbabilityService(
            calculator,
            haplotypeFrequencyService,
            genotypeMatcher,
            Substitute.For<IGenotypeSetService>(),
            Substitute.For<IMatchPredictionLogger<MatchProbabilityLoggingContext>>(),
            new MatchProbabilityLoggingContext());
    }

    /// <param name="storedRow">
    /// The state of the donor's stored row when the batch runs, named by the source search should give for it.
    /// </param>
    /// <param name="isStoredBack">True when the live set is stored, so that the next search can use it.</param>
    [TestCase(DonorGenotypeSetSource.Precomputed, false)]
    [TestCase(DonorGenotypeSetSource.NoRow, true)]
    [TestCase(DonorGenotypeSetSource.TypingChanged, false)]
    [TestCase(DonorGenotypeSetSource.DecodeFailed, false)]
    public async Task MatchPredictionBatch_ForTheDonorsStoredRow_UsesItOrComputesLive_AndStoresOnlyWhenTheRowIsMissing(
        DonorGenotypeSetSource storedRow,
        bool isStoredBack)
    {
        var searchTyping = Typing("search");
        var donorId = await InsertDonor(searchTyping);
        await Store(donorId, searchTyping, storedSet);
        await SetUpTheStoredRow(storedRow, donorId);
        var rowBeforeTheBatch = await StoredRow(donorId);

        var result = await RunBatch(donorId, searchTyping);

        result.GenotypeSetSource.Should().Be(storedRow);
        var matcherInput = (GenotypeMatcherInput)genotypeMatcher.ReceivedCalls().Single().GetArguments().Single();
        if (storedRow == DonorGenotypeSetSource.Precomputed)
        {
            SubjectGenotypeSetPayload.Encode(matcherInput.DonorGenotypeSet).Should().Equal(SubjectGenotypeSetPayload.Encode(storedSet));
        }
        else
        {
            matcherInput.DonorGenotypeSet.Should().BeNull("the donor is computed live");
        }

        var usageMetric = storedRow == DonorGenotypeSetSource.Precomputed ? "PrecomputedCount" : storedRow.ToString();
        usageMetrics[usageMetric].Should().Be(1);
        usageMetrics["StoredCount"].Should().Be(isStoredBack ? 1 : 0);

        var rowAfterTheBatch = await StoredRow(donorId);
        if (isStoredBack)
        {
            rowAfterTheBatch.HlaTypingKey.Should().Be(TypingKey(searchTyping));
            rowAfterTheBatch.SubjectGenotypeSetData.Should().Equal(SubjectGenotypeSetPayload.Encode(liveSet));
        }
        else
        {
            rowAfterTheBatch.Should().BeEquivalentTo(rowBeforeTheBatch);
        }
    }

    /// <summary>
    /// Changes the donor's row, stored for the search's typing, into the state that gives <paramref name="storedRow"/>.
    /// </summary>
    private async Task SetUpTheStoredRow(DonorGenotypeSetSource storedRow, int donorId)
    {
        switch (storedRow)
        {
            case DonorGenotypeSetSource.Precomputed:
                break;
            case DonorGenotypeSetSource.NoRow:
                // A donor that was never precomputed: no link row and no value row. A value row of the same key would be
                // kept by the store back, as value rows are keyed by content.
                await TruncatePrecomputeTables();
                break;
            case DonorGenotypeSetSource.TypingChanged:
                // As an import after matching does: the donor gets a new typing, and the set of the new typing is stored.
                var newTyping = Typing("imported");
                await UpdateDonorTyping(donorId, newTyping);
                await Store(donorId, newTyping, SetWithGenotypes(4, 0.7m));
                break;
            case DonorGenotypeSetSource.DecodeFailed:
                await UpdateStoredPayload(donorId, [1, 2, 3]);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(storedRow), storedRow, null);
        }
    }

    /// <summary>One batch of one donor, as the match prediction algorithm runs it: resolve, calculate, complete.</summary>
    private async Task<MatchProbabilityResult> RunBatch(int donorId, PhenotypeInfo<string> searchTyping)
    {
        var donor = new DonorInput
        {
            DonorIds = [donorId],
            DonorHla = searchTyping.ToPhenotypeInfoTransfer(),
            DonorFrequencySetMetadata = new FrequencySetMetadata { RegistryCode = RegistryCode, EthnicityCode = EthnicityCode }
        };
        var input = new SingleDonorMatchProbabilityInput
        {
            SearchRequestId = Guid.NewGuid().ToString(),
            MatchingAlgorithmHlaNomenclatureVersion = activeRecord.HlaNomenclatureVersion,
            MatchingAlgorithmDataRefreshRecordId = activeRecord.Id,
            PatientHla = Typing("patient").ToPhenotypeInfoTransfer(),
            Donor = donor
        };

        var batchContext = await resolver.Resolve(input, [donor]);
        var result = await matchProbabilityService.CalculateMatchProbability(
            input,
            new SubjectGenotypeSet(false, new List<GenotypeAtDesiredResolutions>(), 0.1m),
            batchContext);
        await completer.Complete(
            input,
            batchContext,
            [new DonorGenotypeSetBatchOutcome(donor.DonorIds.Count, result.GenotypeSetSource, result.GenotypeSetToStore)],
            PatientGenotypeSetSource.NoKey);

        return result;
    }

    /// <summary>Stores the set for the donor through the writer that search stores live sets with.</summary>
    private async Task Store(int donorId, PhenotypeInfo<string> typing, SubjectGenotypeSet set)
    {
        var storeResult = await writer.Store(
            [new DonorGenotypeSetToStore([donorId], typing, RegistryCode, EthnicityCode, FrequencySetId, false, SubjectGenotypeSetPayload.Encode(set))],
            Key.ToLoci(),
            activeRecord.Id);
        storeResult.StoredCount.Should().Be(1);
    }

    private async Task<StoredDonorSubjectGenotypeSet> StoredRow(int donorId) =>
        (await repository.GetDonorSubjectGenotypeSets([donorId], Key)).GetValueOrDefault(donorId);

    private async Task<int> InsertDonor(PhenotypeInfo<string> typing)
    {
        var donorId = DonorIdGenerator.NextId();
        var donor = new Donor
        {
            DonorId = donorId,
            DonorType = DonorType.Adult,
            IsAvailableForSearch = true,
            ExternalDonorCode = DonorIdGenerator.NewExternalCode,
            RegistryCode = RegistryCode,
            EthnicityCode = EthnicityCode
        };
        SetTyping(donor, typing);

        await using var context = Context();
        context.Donors.Add(donor);
        await context.SaveChangesAsync();
        return donorId;
    }

    private async Task UpdateDonorTyping(int donorId, PhenotypeInfo<string> typing)
    {
        await using var context = Context();
        var donor = await context.Donors.SingleAsync(d => d.DonorId == donorId);
        SetTyping(donor, typing);
        await context.SaveChangesAsync();
    }

    /// <summary>Replaces the data of the value row that the donor's row points at.</summary>
    private async Task UpdateStoredPayload(int donorId, byte[] data)
    {
        await using var context = Context();
        var valueId = await context.DonorSubjectGenotypeSets
            .Where(row => row.DonorId == donorId && row.AllowedLociKey == Key)
            .Select(row => row.SubjectGenotypeSetValueId)
            .SingleAsync();
        var value = await context.SubjectGenotypeSetValues.SingleAsync(row => row.Id == valueId);
        value.SubjectGenotypeSetData = data;
        await context.SaveChangesAsync();
    }

    /// <summary>The precompute tables only: the donors of each test have ids and codes of their own.</summary>
    private async Task TruncatePrecomputeTables()
    {
        await using var context = Context();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE DonorSubjectGenotypeSets; TRUNCATE TABLE SubjectGenotypeSetValues;");
    }

    private SearchAlgorithmContext Context() =>
        new ContextFactory().Create(connectionStringFactory.GenerateConnectionStringProvider(activeRecord.Database).GetConnectionString());

    private static string TypingKey(PhenotypeInfo<string> typing) => SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(typing, Key);

    /// <summary>A typing with a distinct name at every match prediction locus.</summary>
    private static PhenotypeInfo<string> Typing(string prefix) =>
        new((locus, position) => locus == Locus.Dpb1 ? null : $"{locus.ToString().ToUpperInvariant()}*{prefix}-{position}");

    private static SubjectGenotypeSet SetWithGenotypes(int count, decimal sumOfLikelihoods) =>
        new(
            false,
            Enumerable.Range(1, count)
                .Select(i => new GenotypeAtDesiredResolutions(new ImputedGenotype(null, Typing($"g{i}"), 0.1m * i), Typing($"p{i}")))
                .ToList(),
            sumOfLikelihoods);

    private static void SetTyping(Donor donor, PhenotypeInfo<string> typing)
    {
        donor.A_1 = typing.A.Position1;
        donor.A_2 = typing.A.Position2;
        donor.B_1 = typing.B.Position1;
        donor.B_2 = typing.B.Position2;
        donor.C_1 = typing.C.Position1;
        donor.C_2 = typing.C.Position2;
        donor.DQB1_1 = typing.Dqb1.Position1;
        donor.DQB1_2 = typing.Dqb1.Position2;
        donor.DRB1_1 = typing.Drb1.Position1;
        donor.DRB1_2 = typing.Drb1.Position2;
    }
}
