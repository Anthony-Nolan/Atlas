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
using AutoFixture;
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
/// How search uses a donor's stored genotype set (ATL-221), which it reads by the key of the donor input's typing,
/// frequency set and loci (ATL-424): when the value is good, missing, only for another frequency set, or cannot be
/// decoded; when the donor was re-typed or has no link row; and when the batch cannot use stored rows at all (ATL-493).
/// The stored rows are real rows in the active matching database, and the batch runs through the real path: the source
/// resolver and reader, the match probability service, and the batch completer with the real writer. Only the Match
/// Prediction engine (the genotype matcher, the frequency set lookups and the small g group check) is substituted: this
/// project has no Match Prediction database.
/// </summary>
/// <remarks>
/// <see cref="DonorGenotypeSetSource.UncoveredAllowedLoci"/> is not here: every set of loci the input validator accepts
/// is one of the four precomputed keys. <see cref="DonorGenotypeSetSource.ReadFailed"/> is not here either: it needs a
/// failing read, which the unit tests of the resolver cover.
/// </remarks>
[TestFixture]
internal class PrecomputedDonorGenotypeSetSearchTests
{
    private const AllowedLociKey Key = AllowedLociKey.ABCDrb1Dqb1;
    private const int DonorFrequencySetId = 456;
    private const int ReplacedFrequencySetId = 123;

    /// <summary>Not the donor's: a set stored with the patient's frequency set would never be used for the donor.</summary>
    private const int PatientFrequencySetId = 789;

    private const string RegistryCode = "reg";
    private const string EthnicityCode = "eth";

    /// <summary>The donor's stored row when the batch runs.</summary>
    internal enum RowState
    {
        Good,
        Unrepresented,
        None,
        NoneAsUnambiguous,
        OtherFrequencySet,
        OtherTyping,
        NoLinkRow,
        Undecodable
    }

    /// <summary>The transient database the request says matching used.</summary>
    internal enum MatchedDatabase
    {
        Active,
        Unknown,
        NoLongerActive
    }

    private Fixture fixture;
    private IDataRefreshHistoryRepository historyRepository;
    private ActiveDataRefreshRecord activeRecord;
    private StaticallyChosenTransientSqlConnectionStringProviderFactory connectionStringFactory;
    private SubjectGenotypeSetRepositoryFactory repositoryFactory;
    private ISubjectGenotypeSetRepository repository;
    private PrecomputedDonorGenotypeSetWriter writer;
    private IAtlasLogger logger;
    private IHaplotypeFrequencyLookupService frequencySetLookup;
    private IHaplotypeFrequencyService haplotypeFrequencyService;
    private IDonorGenotypeSetSourceResolver resolver;
    private IMatchProbabilityService matchProbabilityService;
    private IDonorGenotypeSetBatchCompleter completer;
    private IGenotypeMatcher genotypeMatcher;
    private IGenotypeSetService genotypeSetService;
    private Dictionary<string, double> usageMetrics;
    private SubjectGenotypeSet storedSet;
    private SubjectGenotypeSet liveSet;
    private Dictionary<(string TypingKey, int FrequencySetId), SubjectGenotypeSet> engineSets;

    [SetUp]
    public async Task SetUp()
    {
        fixture = new Fixture();
        engineSets = new Dictionary<(string TypingKey, int FrequencySetId), SubjectGenotypeSet>();
        historyRepository = Injection.Provider.GetService<IDataRefreshHistoryRepository>();
        activeRecord = historyRepository.GetActiveRecord();
        connectionStringFactory = Injection.Provider.GetService<StaticallyChosenTransientSqlConnectionStringProviderFactory>();
        repositoryFactory = new SubjectGenotypeSetRepositoryFactory(connectionStringFactory);
        repository = repositoryFactory.GetForDatabase(activeRecord.Database);
        await TruncatePrecomputeTables();

        writer = new PrecomputedDonorGenotypeSetWriter(historyRepository, repositoryFactory);
        // The fixture is one instance for all its tests: a batch that sends no usage event must not be checked against
        // the previous test's metrics.
        usageMetrics = null;
        logger = Substitute.For<IAtlasLogger>();
        logger.When(l => l.SendEvent(
                DonorGenotypeSetBatchCompleter.UsageEventName,
                Arg.Any<LogLevel>(),
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<Dictionary<string, double>>()))
            .Do(call => usageMetrics = call.ArgAt<Dictionary<string, double>>(3));

        // The set that the batch chooses for a donor input before it reads, as the live path chooses it below.
        frequencySetLookup = Substitute.For<IHaplotypeFrequencyLookupService>();
        frequencySetLookup.GetSingleHaplotypeFrequencySet(default).ReturnsForAnyArgs(new HaplotypeFrequencySet { Id = DonorFrequencySetId });
        resolver = Resolver(PrecomputedGenotypeSetMode.DefaultPrecomputed);
        completer = new DonorGenotypeSetBatchCompleter(writer, logger);

        storedSet = SetWithGenotypes(2, 0.5m);
        liveSet = SetWithGenotypes(3, 0.6m);

        haplotypeFrequencyService = Substitute.For<IHaplotypeFrequencyService>();
        haplotypeFrequencyService.GetHaplotypeFrequencySets(default, default).ReturnsForAnyArgs(new HaplotypeFrequencySetResponse
        {
            PatientSet = new HaplotypeFrequencySet { Id = PatientFrequencySetId },
            DonorSet = new HaplotypeFrequencySet { Id = DonorFrequencySetId }
        });

        // Like the real matcher: uses the supplied donor set, or "computes" the live one. The input is null only when a test
        // sets up the matcher again: that is not a call.
        genotypeMatcher = Substitute.For<IGenotypeMatcher>();
        genotypeMatcher.MatchPatientDonorGenotypes(default).ReturnsForAnyArgs(call =>
        {
            var donorSet = call.Arg<GenotypeMatcherInput>()?.DonorGenotypeSet ?? liveSet;
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

        // Only asked whether a donor with no row is unambiguous at small g group. False, as for most donors, unless a
        // test says otherwise.
        genotypeSetService = Substitute.For<IGenotypeSetService>();

        matchProbabilityService = new MatchProbabilityService(
            calculator,
            haplotypeFrequencyService,
            genotypeMatcher,
            genotypeSetService,
            Substitute.For<IMatchPredictionLogger<MatchProbabilityLoggingContext>>(),
            new MatchProbabilityLoggingContext());
    }

    /// <param name="storedRow">The state of the donor's stored row when the batch runs.</param>
    /// <param name="expectedSource">The source search should give for it.</param>
    /// <param name="isStoredBack">True when the live set is stored, so that the next search can use it.</param>
    [TestCase(RowState.Good, DonorGenotypeSetSource.Precomputed, false)]
    [TestCase(RowState.Unrepresented, DonorGenotypeSetSource.Precomputed, false)]
    [TestCase(RowState.None, DonorGenotypeSetSource.NoRow, true)]
    [TestCase(RowState.NoneAsUnambiguous, DonorGenotypeSetSource.NotStoredUnambiguous, false)]
    [TestCase(RowState.OtherFrequencySet, DonorGenotypeSetSource.NoRow, true)]
    [TestCase(RowState.OtherTyping, DonorGenotypeSetSource.Precomputed, false)]
    [TestCase(RowState.NoLinkRow, DonorGenotypeSetSource.Precomputed, false)]
    [TestCase(RowState.Undecodable, DonorGenotypeSetSource.DecodeFailed, false)]
    public async Task MatchPredictionBatch_ForTheDonorsStoredRow_UsesTheValueOfItsKeyOrComputesLive_AndStoresOnlyAMissingRow(
        RowState storedRow,
        DonorGenotypeSetSource expectedSource,
        bool isStoredBack)
    {
        var searchTyping = Typing("search");
        var donorId = await InsertDonor(searchTyping);
        await Store(donorId, searchTyping, storedSet);
        await SetUpTheStoredRow(storedRow, donorId);
        var rowBeforeTheBatch = await StoredRow(donorId);

        var result = await RunBatch(Input(searchTyping, donorId));

        result.GenotypeSetSource.Should().Be(expectedSource);
        var matcherInput = (GenotypeMatcherInput)genotypeMatcher.ReceivedCalls().Single().GetArguments().Single();
        switch (storedRow)
        {
            case RowState.Good:
            case RowState.OtherTyping:
            case RowState.NoLinkRow:
                SubjectGenotypeSetPayload.Encode(matcherInput.DonorGenotypeSet).Should().Equal(SubjectGenotypeSetPayload.Encode(storedSet));
                break;
            case RowState.Unrepresented:
                matcherInput.DonorGenotypeSet.IsUnrepresented.Should().BeTrue();
                matcherInput.DonorGenotypeSet.Genotypes.Should().BeEmpty();
                break;
            default:
                matcherInput.DonorGenotypeSet.Should().BeNull("the donor is computed live");
                break;
        }

        // Checked on the result too, not only in the database: the writer would skip a set stored for a changed
        // donor, and a row that is already there would hide one stored for a bad row.
        (result.GenotypeSetToStore != null).Should().Be(isStoredBack);

        var usageMetric = expectedSource == DonorGenotypeSetSource.Precomputed ? "PrecomputedCount" : expectedSource.ToString();
        usageMetrics[usageMetric].Should().Be(1);
        usageMetrics["StoredCount"].Should().Be(isStoredBack ? 1 : 0);
        usageMetrics["StoreSkippedDonorChanged"].Should().Be(0);
        usageMetrics["StoreSkippedDatabaseChanged"].Should().Be(0);

        var rowAfterTheBatch = await StoredRow(donorId);
        if (isStoredBack)
        {
            // All of the key search checks before it uses a row, so that the next search uses this one.
            rowAfterTheBatch.HlaTypingKey.Should().Be(TypingKey(searchTyping));
            rowAfterTheBatch.HaplotypeFrequencySetId.Should().Be(DonorFrequencySetId);
            rowAfterTheBatch.IsUnrepresented.Should().BeFalse();
            rowAfterTheBatch.SubjectGenotypeSetData.Should().Equal(SubjectGenotypeSetPayload.Encode(liveSet));
        }
        else
        {
            rowAfterTheBatch.Should().BeEquivalentTo(rowBeforeTheBatch);
        }
    }

    [TestCase(PrecomputedGenotypeSetMode.ForceLive, true, MatchedDatabase.Active, DonorGenotypeSetSource.PrecomputeDisabled)]
    [TestCase(PrecomputedGenotypeSetMode.DefaultLive, null, MatchedDatabase.Active, DonorGenotypeSetSource.PrecomputeDisabled)]
    [TestCase(PrecomputedGenotypeSetMode.DefaultPrecomputed, false, MatchedDatabase.Active, DonorGenotypeSetSource.PrecomputeDisabled)]
    [TestCase(PrecomputedGenotypeSetMode.DefaultPrecomputed, null, MatchedDatabase.Unknown, DonorGenotypeSetSource.ActiveDatabaseUnknown)]
    [TestCase(PrecomputedGenotypeSetMode.DefaultPrecomputed, null, MatchedDatabase.NoLongerActive, DonorGenotypeSetSource.ActiveDatabaseChanged)]
    public async Task MatchPredictionBatch_WhenTheBatchCannotUseStoredRows_ComputesTheDonorLive_AndStoresNothing(
        PrecomputedGenotypeSetMode mode,
        bool? usePrecomputedGenotypeSets,
        MatchedDatabase matchedDatabase,
        DonorGenotypeSetSource expectedSource)
    {
        resolver = Resolver(mode);
        var searchTyping = Typing("search");
        var donorId = await InsertDonor(searchTyping);
        await Store(donorId, searchTyping, storedSet);
        var rowBeforeTheBatch = await StoredRow(donorId);
        var input = Input(searchTyping, donorId);
        input.UsePrecomputedGenotypeSets = usePrecomputedGenotypeSets;
        input.MatchingAlgorithmDataRefreshRecordId = matchedDatabase switch
        {
            MatchedDatabase.Active => activeRecord.Id,
            MatchedDatabase.Unknown => null,
            MatchedDatabase.NoLongerActive => activeRecord.Id + 1,
            _ => throw new ArgumentOutOfRangeException(nameof(matchedDatabase), matchedDatabase, null)
        };

        var result = await RunBatch(input);

        result.GenotypeSetSource.Should().Be(expectedSource);
        var matcherInput = (GenotypeMatcherInput)genotypeMatcher.ReceivedCalls().Single().GetArguments().Single();
        matcherInput.DonorGenotypeSet.Should().BeNull("the good stored row must not be used");
        result.GenotypeSetToStore.Should().BeNull();
        usageMetrics[expectedSource.ToString()].Should().Be(1);
        usageMetrics["StoredCount"].Should().Be(0);
        (await StoredRow(donorId)).Should().BeEquivalentTo(rowBeforeTheBatch);
    }

    [Test]
    public async Task MatchPredictionBatch_WhenTheDonorIsUpdatedBeforeTheStore_SkipsTheDonor()
    {
        var searchTyping = Typing("search");
        var donorId = await InsertDonor(searchTyping);

        // As an import that runs while the batch computes the donor live.
        var result = await RunBatch(Input(searchTyping, donorId), beforeTheStore: () => UpdateDonorTyping(donorId, Typing("imported")));

        result.GenotypeSetSource.Should().Be(DonorGenotypeSetSource.NoRow);
        result.GenotypeSetToStore.Should().NotBeNull();
        usageMetrics["StoredCount"].Should().Be(0);
        usageMetrics["StoreSkippedDonorChanged"].Should().Be(1);
        (await StoredRow(donorId)).Should().BeNull();
    }

    [Test]
    public async Task MatchPredictionBatch_WhenTheActiveDatabaseChangesBeforeTheStore_StoresNothing()
    {
        var searchTyping = Typing("search");
        var donorId = await InsertDonor(searchTyping);

        // As a data refresh that completes while the batch computes the donor live.
        var historyAfterTheRefresh = Substitute.For<IDataRefreshHistoryRepository>();
        historyAfterTheRefresh.GetActiveRecord().Returns(activeRecord with { Id = activeRecord.Id + 1 });
        completer = new DonorGenotypeSetBatchCompleter(new PrecomputedDonorGenotypeSetWriter(historyAfterTheRefresh, repositoryFactory), logger);

        var result = await RunBatch(Input(searchTyping, donorId));

        result.GenotypeSetSource.Should().Be(DonorGenotypeSetSource.NoRow);
        result.GenotypeSetToStore.Should().NotBeNull();
        usageMetrics["StoredCount"].Should().Be(0);
        usageMetrics["StoreSkippedDatabaseChanged"].Should().Be(1);
        (await StoredRow(donorId)).Should().BeNull();
    }

    /// <summary>
    /// Donors with the same typing share one donor input. One that was updated after matching must not keep the search
    /// from storing the set for the other.
    /// </summary>
    [Test]
    public async Task MatchPredictionBatch_ForAnInputWithAnUpdatedDonorAndADonorWithNoRow_StoresForTheDonorWithNoRowOnly()
    {
        var searchTyping = Typing("search");
        var updatedDonorId = await InsertDonor(searchTyping);
        var donorIdWithNoRow = await InsertDonor(searchTyping);
        var newTyping = Typing("imported");
        await UpdateDonorTyping(updatedDonorId, newTyping);
        await Store(updatedDonorId, newTyping, storedSet);
        var updatedDonorRowBeforeTheBatch = await StoredRow(updatedDonorId);

        var result = await RunBatch(Input(searchTyping, updatedDonorId, donorIdWithNoRow));

        result.GenotypeSetSource.Should().Be(DonorGenotypeSetSource.NoRow);
        usageMetrics["StoredCount"].Should().Be(1);
        usageMetrics["StoreSkippedDonorChanged"].Should().Be(1);
        (await StoredRow(updatedDonorId)).Should().BeEquivalentTo(updatedDonorRowBeforeTheBatch);
        var storedRow = await StoredRow(donorIdWithNoRow);
        storedRow.HlaTypingKey.Should().Be(TypingKey(searchTyping));
        storedRow.HaplotypeFrequencySetId.Should().Be(DonorFrequencySetId);
        storedRow.SubjectGenotypeSetData.Should().Equal(SubjectGenotypeSetPayload.Encode(liveSet));
    }

    /// <summary>
    /// A new frequency set is activated for the donor's codes after the batch read its stored rows, and before the donor
    /// runs. The value of the set that the batch read with must not be used, and the donor is stored for the new set.
    /// </summary>
    [Test]
    public async Task MatchPredictionBatch_WhenANewFrequencySetIsActivatedDuringTheBatch_ComputesTheDonorLive_AndStoresItForTheNewSet()
    {
        var searchTyping = Typing("search");
        var donorId = await InsertDonor(searchTyping);
        await Store(donorId, searchTyping, storedSet);
        var newFrequencySetId = fixture.Create<int>();
        // The batch chose DonorFrequencySetId when it started; the donor's own run chooses the new set.
        haplotypeFrequencyService.GetHaplotypeFrequencySets(default, default).ReturnsForAnyArgs(new HaplotypeFrequencySetResponse
        {
            PatientSet = new HaplotypeFrequencySet { Id = PatientFrequencySetId },
            DonorSet = new HaplotypeFrequencySet { Id = newFrequencySetId }
        });

        var result = await RunBatch(Input(searchTyping, donorId));

        result.GenotypeSetSource.Should().Be(DonorGenotypeSetSource.StaleFrequencySet);
        var matcherInput = (GenotypeMatcherInput)genotypeMatcher.ReceivedCalls().Single().GetArguments().Single();
        matcherInput.DonorGenotypeSet.Should().BeNull("the donor is computed live");
        usageMetrics["StoredCount"].Should().Be(1);
        (await StoredRow(donorId)).HaplotypeFrequencySetId.Should().Be(newFrequencySetId);
    }

    /// <summary>
    /// Parity (ATL-424): a batch that reads stored sets gives each donor input the set that live imputation gives it. The
    /// inputs cover the cases where the read by key changes which stored row serves a donor - a donor re-typed after
    /// matching, a donor with no link row, and inputs of two frequency sets - next to a precomputed input of two donors and
    /// an input with no stored value. The engine gives one set for each typing and frequency set, and the stored sets are
    /// the sets it gave, as the pre-computation stores them.
    /// </summary>
    [Test]
    public async Task MatchPredictionBatch_ReadingStoredSets_GivesEveryDonorInputTheSetThatLiveImputationGivesIt()
    {
        GivenAnEngineThatGivesOneSetForEachTypingAndFrequencySet();
        var otherRegistryCode = fixture.Create<string>();
        var otherFrequencySetId = fixture.Create<int>();
        GivenTheFrequencySetOfRegistry(otherRegistryCode, otherFrequencySetId);

        // Two donors of one typing, both precomputed.
        var sharedTyping = Typing("shared");
        int[] sharedDonorIds = [await InsertDonor(sharedTyping), await InsertDonor(sharedTyping)];
        foreach (var donorId in sharedDonorIds)
        {
            await Store(donorId, sharedTyping, EngineSet(sharedTyping, DonorFrequencySetId));
        }

        // Re-typed after matching: its link row now points at the value of its new typing.
        var retypedSearchTyping = Typing("retyped-search");
        var retypedDonorId = await InsertDonor(retypedSearchTyping);
        await Store(retypedDonorId, retypedSearchTyping, EngineSet(retypedSearchTyping, DonorFrequencySetId));
        var retypedNewTyping = Typing("retyped-new");
        await UpdateDonorTyping(retypedDonorId, retypedNewTyping);
        await Store(retypedDonorId, retypedNewTyping, EngineSet(retypedNewTyping, DonorFrequencySetId));

        // A value with no link row.
        var unlinkedTyping = Typing("unlinked");
        var unlinkedDonorId = await InsertDonor(unlinkedTyping);
        await Store(unlinkedDonorId, unlinkedTyping, EngineSet(unlinkedTyping, DonorFrequencySetId));
        await repository.DeleteDonorAssignments([unlinkedDonorId]);

        // No stored value.
        var missingTyping = Typing("missing");
        var missingDonorId = await InsertDonor(missingTyping);

        // Another frequency set, chosen by the donor's registry.
        var otherSetTyping = Typing("other-set");
        var otherSetDonorId = await InsertDonor(otherSetTyping, otherRegistryCode);
        await Store(otherSetDonorId, otherSetTyping, EngineSet(otherSetTyping, otherFrequencySetId), otherFrequencySetId, otherRegistryCode);

        var results = await RunBatch([
            Input(sharedTyping, sharedDonorIds),
            Input(retypedSearchTyping, retypedDonorId),
            Input(unlinkedTyping, unlinkedDonorId),
            Input(missingTyping, missingDonorId),
            InputOfRegistry(otherSetTyping, otherRegistryCode, otherSetDonorId)
        ]);

        results.Select(result => result.GenotypeSetSource).Should().Equal(
            DonorGenotypeSetSource.Precomputed,
            DonorGenotypeSetSource.Precomputed,
            DonorGenotypeSetSource.Precomputed,
            DonorGenotypeSetSource.NoRow,
            DonorGenotypeSetSource.Precomputed);
        var matcherInputs = genotypeMatcher.ReceivedCalls().Select(call => (GenotypeMatcherInput)call.GetArguments().Single()).ToList();
        matcherInputs.Should().HaveCount(5);
        foreach (var matcherInput in matcherInputs)
        {
            var engineSet = EngineSet(matcherInput.DonorData.HlaTyping, matcherInput.DonorData.SubjectFrequencySet.FrequencySet.Id);
            SubjectGenotypeSetPayload.Encode(matcherInput.DonorGenotypeSet ?? engineSet).Should().Equal(SubjectGenotypeSetPayload.Encode(engineSet));
        }
    }

    /// <summary>
    /// Changes the donor's row, stored for the search's typing, into <paramref name="storedRow"/>.
    /// </summary>
    private async Task SetUpTheStoredRow(RowState storedRow, int donorId)
    {
        switch (storedRow)
        {
            case RowState.Good:
                break;
            case RowState.Unrepresented:
                await TruncatePrecomputeTables();
                await Store(donorId, Typing("search"), new SubjectGenotypeSet(true, new List<GenotypeAtDesiredResolutions>(), 0m));
                break;
            case RowState.None:
                // A donor that was never precomputed: no link row and no value row. A value row of the same key would be
                // kept by the store back, as value rows are keyed by content.
                await TruncatePrecomputeTables();
                break;
            case RowState.NoneAsUnambiguous:
                await TruncatePrecomputeTables();
                genotypeSetService.IsUnambiguousAtSmallGGroup(default, default).ReturnsForAnyArgs(true);
                break;
            case RowState.OtherFrequencySet:
                // Precomputed before the frequency set search now uses was activated. Search reads the key of the set it
                // uses, so it does not find this value.
                await TruncatePrecomputeTables();
                await Store(donorId, Typing("search"), storedSet, ReplacedFrequencySetId);
                break;
            case RowState.OtherTyping:
                // As an import after matching does: the donor gets a new typing, and the set of the new typing is stored. The
                // value of the search's typing stays, and search reads it by its key.
                var newTyping = Typing("imported");
                await UpdateDonorTyping(donorId, newTyping);
                await Store(donorId, newTyping, SetWithGenotypes(4, 0.7m));
                break;
            case RowState.NoLinkRow:
                // For example, a pre-computation that stored the value and failed before the link row. Search does not
                // need the link row.
                await repository.DeleteDonorAssignments([donorId]);
                break;
            case RowState.Undecodable:
                await UpdateStoredPayload(donorId, [1, 2, 3]);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(storedRow), storedRow, null);
        }
    }

    private IDonorGenotypeSetSourceResolver Resolver(PrecomputedGenotypeSetMode mode) =>
        new DonorGenotypeSetSourceResolver(
            new PrecomputedGenotypeSetSettings { Mode = mode },
            new PrecomputedDonorGenotypeSetReader(historyRepository, repositoryFactory),
            frequencySetLookup,
            logger);

    /// <summary>One donor input, with the ids that share its typing, matched on the active database.</summary>
    private SingleDonorMatchProbabilityInput Input(PhenotypeInfo<string> searchTyping, params int[] donorIds) =>
        InputOfRegistry(searchTyping, RegistryCode, donorIds);

    /// <summary>One donor input of the registry, which chooses its frequency set, with the ids that share its typing.</summary>
    private SingleDonorMatchProbabilityInput InputOfRegistry(PhenotypeInfo<string> searchTyping, string registryCode, params int[] donorIds) =>
        new()
        {
            SearchRequestId = Guid.NewGuid().ToString(),
            MatchingAlgorithmHlaNomenclatureVersion = activeRecord.HlaNomenclatureVersion,
            MatchingAlgorithmDataRefreshRecordId = activeRecord.Id,
            PatientHla = Typing("patient").ToPhenotypeInfoTransfer(),
            Donor = new DonorInput
            {
                DonorIds = donorIds.ToList(),
                DonorHla = searchTyping.ToPhenotypeInfoTransfer(),
                DonorFrequencySetMetadata = new FrequencySetMetadata { RegistryCode = registryCode, EthnicityCode = EthnicityCode }
            }
        };

    /// <summary>One batch of one donor input, as the match prediction algorithm runs it: resolve, calculate, complete.</summary>
    /// <param name="input">The batch's one donor input.</param>
    /// <param name="beforeTheStore">What happens in the database after the donor is calculated and before the batch stores it.</param>
    private async Task<MatchProbabilityResult> RunBatch(SingleDonorMatchProbabilityInput input, Func<Task> beforeTheStore = null) =>
        (await RunBatch([input], beforeTheStore)).Single();

    /// <summary>One batch, as the match prediction algorithm runs it: resolve, calculate, complete.</summary>
    /// <remarks>
    /// A copy of the steps in <c>MatchPredictionAlgorithm.RunBatch</c> and <c>ParallelMatchPredictionAlgorithm.RunBatch</c>,
    /// without the cache warm-up, the patient set provider and the result upload. A change to those steps does not fail
    /// these tests, so keep this copy the same as them.
    /// </remarks>
    /// <param name="inputs">The batch's donor inputs, of one search.</param>
    /// <param name="beforeTheStore">What happens in the database after the donors are calculated and before the batch stores them.</param>
    private async Task<IReadOnlyList<MatchProbabilityResult>> RunBatch(
        IReadOnlyList<SingleDonorMatchProbabilityInput> inputs,
        Func<Task> beforeTheStore = null)
    {
        var batchContext = await resolver.Resolve(inputs[0], inputs.Select(input => input.Donor).ToList());
        var results = new List<MatchProbabilityResult>(inputs.Count);
        foreach (var input in inputs)
        {
            results.Add(await matchProbabilityService.CalculateMatchProbability(
                input,
                new SubjectGenotypeSet(false, new List<GenotypeAtDesiredResolutions>(), 0.1m),
                batchContext));
        }

        if (beforeTheStore != null)
        {
            await beforeTheStore();
        }

        await completer.Complete(
            inputs[0],
            batchContext,
            inputs.Zip(results, (input, result) =>
                new DonorGenotypeSetBatchOutcome(input.Donor.DonorIds.Count, result.GenotypeSetSource, result.GenotypeSetToStore)).ToList(),
            PatientGenotypeSetSource.NoKey);
        usageMetrics.Should().NotBeNull("the completer sends one usage event per batch");

        return results;
    }

    /// <summary>Stores the set for the donor through the writer that search stores live sets with.</summary>
    private async Task Store(
        int donorId,
        PhenotypeInfo<string> typing,
        SubjectGenotypeSet set,
        int frequencySetId = DonorFrequencySetId,
        string registryCode = RegistryCode)
    {
        var storeResult = await writer.Store(
            [new DonorGenotypeSetToStore(
                [donorId],
                typing,
                registryCode,
                EthnicityCode,
                frequencySetId,
                set.IsUnrepresented,
                set.IsUnrepresented ? null : SubjectGenotypeSetPayload.Encode(set))],
            Key.ToLoci(),
            activeRecord.Id);
        storeResult.StoredCount.Should().Be(1);
    }

    /// <summary>The value row that the donor's link row points at; null when the donor has no link row.</summary>
    private async Task<SubjectGenotypeSetValue> StoredRow(int donorId)
    {
        await using var context = Context();
        var valueId = await context.DonorSubjectGenotypeSets
            .Where(row => row.DonorId == donorId && row.AllowedLociKey == Key)
            .Select(row => (int?)row.SubjectGenotypeSetValueId)
            .SingleOrDefaultAsync();

        return valueId == null ? null : await context.SubjectGenotypeSetValues.SingleAsync(row => row.Id == valueId);
    }

    private async Task<int> InsertDonor(PhenotypeInfo<string> typing, string registryCode = RegistryCode)
    {
        var donorId = DonorIdGenerator.NextId();
        var donor = new Donor
        {
            DonorId = donorId,
            DonorType = DonorType.Adult,
            IsAvailableForSearch = true,
            ExternalDonorCode = DonorIdGenerator.NewExternalCode,
            RegistryCode = registryCode,
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

    /// <summary>Like live imputation: one set for each typing and frequency set, the same at every call.</summary>
    private void GivenAnEngineThatGivesOneSetForEachTypingAndFrequencySet() =>
        genotypeMatcher.MatchPatientDonorGenotypes(default).ReturnsForAnyArgs(call =>
        {
            var input = call.Arg<GenotypeMatcherInput>();
            var donorSet = input.DonorGenotypeSet ?? EngineSet(input.DonorData.HlaTyping, input.DonorData.SubjectFrequencySet.FrequencySet.Id);
            return new GenotypeMatcherResult
            {
                PatientResult = new GenotypeMatcherResult.SubjectResult(false, 1, 0.1m),
                DonorResult = new GenotypeMatcherResult.SubjectResult(donorSet.IsUnrepresented, donorSet.Genotypes.Count, donorSet.SumOfLikelihoods),
                GenotypeMatchDetails = new List<GenotypeMatchDetails>(),
                DonorGenotypeSet = donorSet
            };
        });

    /// <summary>The set that the engine gives for the typing and frequency set: one of its own for each pair.</summary>
    private SubjectGenotypeSet EngineSet(PhenotypeInfo<string> typing, int frequencySetId)
    {
        var key = (TypingKey(typing), frequencySetId);
        if (!engineSets.TryGetValue(key, out var set))
        {
            set = SetWithGenotypes(engineSets.Count + 1, fixture.Create<decimal>());
            engineSets[key] = set;
        }

        return set;
    }

    /// <summary>The frequency set that the donors of the registry get: the same from the batch's lookup and the donor's own.</summary>
    private void GivenTheFrequencySetOfRegistry(string registryCode, int frequencySetId)
    {
        frequencySetLookup
            .GetSingleHaplotypeFrequencySet(Arg.Is<FrequencySetMetadata>(metadata => metadata != null && metadata.RegistryCode == registryCode))
            .Returns(new HaplotypeFrequencySet { Id = frequencySetId });
        haplotypeFrequencyService
            .GetHaplotypeFrequencySets(
                Arg.Is<FrequencySetMetadata>(metadata => metadata != null && metadata.RegistryCode == registryCode),
                Arg.Any<FrequencySetMetadata>())
            .Returns(new HaplotypeFrequencySetResponse
            {
                PatientSet = new HaplotypeFrequencySet { Id = PatientFrequencySetId },
                DonorSet = new HaplotypeFrequencySet { Id = frequencySetId }
            });
    }

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
