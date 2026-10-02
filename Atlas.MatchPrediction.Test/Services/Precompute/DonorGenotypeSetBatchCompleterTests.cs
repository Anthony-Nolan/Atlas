using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.Precompute;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.Precompute;

[TestFixture]
internal class DonorGenotypeSetBatchCompleterTests
{
    private const int DataRefreshRecordId = 42;
    private const string SearchRequestId = "search-id";

    private IPrecomputedDonorGenotypeSetWriter writer;
    private IAtlasLogger logger;
    private IDonorGenotypeSetBatchCompleter completer;

    private Dictionary<string, string> loggedProps;
    private Dictionary<string, double> loggedMetrics;

    [SetUp]
    public void SetUp()
    {
        writer = Substitute.For<IPrecomputedDonorGenotypeSetWriter>();
        logger = Substitute.For<IAtlasLogger>();
        completer = new DonorGenotypeSetBatchCompleter(writer, logger);

        writer.Store(default, default, default).ReturnsForAnyArgs(new DonorGenotypeSetStoreResult(3, 1, 0));

        logger.When(l => l.SendEvent(DonorGenotypeSetBatchCompleter.UsageEventName, Arg.Any<LogLevel>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<Dictionary<string, double>>()))
            .Do(call =>
            {
                loggedProps = call.ArgAt<Dictionary<string, string>>(2);
                loggedMetrics = call.ArgAt<Dictionary<string, double>>(3);
            });
    }

    [Test]
    public async Task Complete_SendsOneEventWithACountForEveryReason()
    {
        var outcomes = new List<DonorGenotypeSetBatchOutcome>
        {
            new(2, DonorGenotypeSetSource.Precomputed, null),
            new(1, DonorGenotypeSetSource.Precomputed, null),
            new(1, DonorGenotypeSetSource.NoRow, ToStore(10)),
            new(3, DonorGenotypeSetSource.StaleFrequencySet, ToStore(11, 12, 13)),
            new(1, DonorGenotypeSetSource.DecodeFailed, ToStore(14)),
        };

        await completer.Complete(Request(), EnabledContext(), outcomes);

        logger.Received(1).SendEvent(DonorGenotypeSetBatchCompleter.UsageEventName, LogLevel.Info, Arg.Any<Dictionary<string, string>>(), Arg.Any<Dictionary<string, double>>());
        loggedProps["SearchRequestId"].Should().Be(SearchRequestId);
        loggedProps["AllowedLociKey"].Should().Be("ABCDrb1Dqb1");
        loggedProps["UsePrecomputedGenotypeSets"].Should().Be("True");
        loggedProps["UsePrecomputedGenotypeSetsSource"].Should().Be("FeatureFlag");
        loggedProps["MatchingAlgorithmDataRefreshRecordId"].Should().Be("42");

        loggedMetrics["DonorCount"].Should().Be(5);
        loggedMetrics["DonorIdCount"].Should().Be(8);
        loggedMetrics["PrecomputedCount"].Should().Be(2);
        loggedMetrics["NoRow"].Should().Be(1);
        loggedMetrics["StaleFrequencySet"].Should().Be(1);
        loggedMetrics["DecodeFailed"].Should().Be(1);
        loggedMetrics["UncoveredAllowedLoci"].Should().Be(0);
        loggedMetrics["ActiveDatabaseChanged"].Should().Be(0);
        loggedMetrics["ActiveDatabaseUnknown"].Should().Be(0);
        loggedMetrics["ReadFailed"].Should().Be(0);
        loggedMetrics["PrecomputeDisabled"].Should().Be(0);
        loggedMetrics["StoredCount"].Should().Be(3);
        loggedMetrics["StoreSkippedHlaChanged"].Should().Be(1);
        loggedMetrics["StoreSkippedDatabaseChanged"].Should().Be(0);
    }

    [Test]
    public async Task Complete_StoresEverySetToStoreOnceAgainstThePinnedRecord()
    {
        var outcomes = new List<DonorGenotypeSetBatchOutcome>
        {
            new(1, DonorGenotypeSetSource.Precomputed, null),
            new(1, DonorGenotypeSetSource.NoRow, ToStore(10)),
            new(2, DonorGenotypeSetSource.StaleFrequencySet, ToStore(11, 12)),
        };

        await completer.Complete(Request(), EnabledContext(), outcomes);

        await writer.Received(1).Store(
            Arg.Is<IReadOnlyCollection<DonorGenotypeSetToStore>>(sets => sets.SelectMany(s => s.DonorIds).Order().SequenceEqual(new[] { 10, 11, 12 })),
            Arg.Is<IReadOnlySet<Locus>>(loci => loci.Count == 5),
            DataRefreshRecordId);
    }

    [Test]
    public async Task Complete_WithNothingToStore_DoesNotCallTheWriter()
    {
        await completer.Complete(Request(), EnabledContext(), [new(1, DonorGenotypeSetSource.Precomputed, null)]);

        await writer.DidNotReceiveWithAnyArgs().Store(default, default, default);
    }

    [Test]
    public async Task Complete_WhenTheBatchCannotStore_DoesNotCallTheWriter()
    {
        var context = DonorGenotypeSetBatchContext.Enabled(
            UsePrecomputedGenotypeSetsSource.Request,
            DataRefreshRecordId,
            PrecomputedDonorGenotypeSetLookup.Unavailable(DonorGenotypeSetSource.ActiveDatabaseChanged));

        await completer.Complete(Request(), context, [new(1, DonorGenotypeSetSource.ActiveDatabaseChanged, ToStore(1))]);

        await writer.DidNotReceiveWithAnyArgs().Store(default, default, default);
        loggedMetrics["ActiveDatabaseChanged"].Should().Be(1);
    }

    [Test]
    public async Task Complete_WhenTheStoreFails_DoesNotThrowAndLogsAWarningAndTheUsageEvent()
    {
        writer.Store(default, default, default).ThrowsAsyncForAnyArgs(new Exception("deadlock victim"));

        await completer.Invoking(c => c.Complete(Request(), EnabledContext(), [new(1, DonorGenotypeSetSource.NoRow, ToStore(1))]))
            .Should().NotThrowAsync();

        logger.Received(1).SendEvent(
            DonorGenotypeSetBatchCompleter.StoreFailedEventName,
            LogLevel.Warn,
            Arg.Is<Dictionary<string, string>>(p => p["ExceptionMessage"] == "deadlock victim" && p["DonorIdCount"] == "1"),
            Arg.Any<Dictionary<string, double>>());
        loggedMetrics["StoredCount"].Should().Be(0);
        loggedMetrics["NoRow"].Should().Be(1);
    }

    [Test]
    public async Task Complete_WhenDisabled_CountsEveryDonorAsPrecomputeDisabled()
    {
        var context = DonorGenotypeSetBatchContext.Disabled(UsePrecomputedGenotypeSetsSource.Request, DataRefreshRecordId);

        await completer.Complete(Request(), context, [new(1, DonorGenotypeSetSource.PrecomputeDisabled, null), new(4, DonorGenotypeSetSource.PrecomputeDisabled, null)]);

        loggedProps["UsePrecomputedGenotypeSets"].Should().Be("False");
        loggedProps["UsePrecomputedGenotypeSetsSource"].Should().Be("Request");
        loggedMetrics["PrecomputeDisabled"].Should().Be(2);
        loggedMetrics["DonorIdCount"].Should().Be(5);
    }

    private static IdentifiedMatchProbabilityRequest Request() => new() { SearchRequestId = SearchRequestId };

    private static DonorGenotypeSetBatchContext EnabledContext() =>
        DonorGenotypeSetBatchContext.Enabled(
            UsePrecomputedGenotypeSetsSource.FeatureFlag,
            DataRefreshRecordId,
            new PrecomputedDonorGenotypeSetLookup("ABCDrb1Dqb1", null, new Dictionary<int, PrecomputedDonorGenotypeSetRow>()));

    private static DonorGenotypeSetToStore ToStore(params int[] donorIds) =>
        new(donorIds, new PhenotypeInfo<string>("hla"), 7, false, [1, 2, 3]);
}
