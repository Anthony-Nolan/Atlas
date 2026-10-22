using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Services.Precompute;
using AwesomeAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.Precompute;

[TestFixture]
internal class GenotypeSetTimingEventsTests
{
    private const string SearchRequestId = "search-id";

    [TestCase(0, "G0to1")]
    [TestCase(1, "G0to1")]
    [TestCase(2, "G2to10")]
    [TestCase(10, "G2to10")]
    [TestCase(11, "G11to100")]
    [TestCase(500, "G101to500")]
    [TestCase(501, "G501to1000")]
    [TestCase(1000, "G501to1000")]
    [TestCase(1001, "G1001Plus")]
    public void BucketName_PutsAGenotypeCountInItsBucket(int genotypeCount, string expectedBucket)
    {
        GenotypeSetTimingEvents.BucketName(genotypeCount).Should().Be(expectedBucket);
    }

    [Test]
    public void Metrics_SumsLiveAndPrecomputedDonorsSeparatelyAndByBucket()
    {
        var timings = Timings(
            EnabledContext(),
            [
                Precomputed(genotypeCount: 1, lookupMs: 0.5, storedBytes: 200, totalMs: 2),
                Precomputed(genotypeCount: 50, lookupMs: 1.5, storedBytes: 4_000, totalMs: 6),
                Live(DonorGenotypeSetSource.NoRow, genotypeCount: 40, liveMs: 3, encodeMs: 0.25, totalMs: 9),
                new DonorGenotypeSetTimingSample(DonorGenotypeSetSource.NoRow, 3, null),
            ]);

        var metrics = GenotypeSetTimingEvents.Metrics(timings);

        metrics["DonorCount"].Should().Be(4);
        metrics["MaxDegreeOfParallelism"].Should().Be(10);
        metrics["BatchMs"].Should().Be(100);
        metrics["PatientMs"].Should().Be(20);
        metrics["PatientGenotypeCount"].Should().Be(33);
        metrics["ReadMs"].Should().Be(30);
        metrics["ReadRowCount"].Should().Be(2);
        metrics["ReadBytes"].Should().Be(7 + 11 + 5);
        metrics["CompleteMs"].Should().Be(40);
        metrics["DonorTotalMs"].Should().Be(17);
        metrics["LookupMs"].Should().Be(2);
        metrics["LiveCount"].Should().Be(1);
        metrics["LiveMs"].Should().Be(3);
        metrics["LiveGenotypes"].Should().Be(40);
        metrics["PrecomputedCount"].Should().Be(2);
        metrics["PrecomputedMs"].Should().Be(2);
        metrics["PrecomputedGenotypes"].Should().Be(51);
        metrics["PrecomputedBytes"].Should().Be(4_200);
        metrics["EncodeCount"].Should().Be(1);
        metrics["EncodeMs"].Should().Be(0.25);

        metrics["PrecomputedG0to1Count"].Should().Be(1);
        metrics["PrecomputedG0to1Ms"].Should().Be(0.5);
        metrics["PrecomputedG0to1Bytes"].Should().Be(200);
        metrics["PrecomputedG11to100Count"].Should().Be(1);
        metrics["PrecomputedG11to100Bytes"].Should().Be(4_000);
        metrics["LiveG11to100Count"].Should().Be(1);
        metrics["LiveG11to100Ms"].Should().Be(3);
        metrics["LiveG1001PlusCount"].Should().Be(0);
    }

    [Test]
    public void Metrics_SumTheLiveImputationStagesOverTheLiveDonors()
    {
        var timings = Timings(
            EnabledContext(),
            [
                LiveWithStages(survivors: 10, fallbacks: 2),
                LiveWithStages(survivors: 5, fallbacks: 0),
                Live(DonorGenotypeSetSource.NoRow, 5, 1, 0, 2),
                Precomputed(genotypeCount: 1, lookupMs: 0.5, storedBytes: 200, totalMs: 2),
            ]);

        var metrics = GenotypeSetTimingEvents.Metrics(timings);

        metrics["StagedLiveCount"].Should().Be(2);
        metrics["Survivors"].Should().Be(15);
        metrics["ConsolidationFallbacks"].Should().Be(2);
        metrics["StagePairingMs"].Should().BeGreaterThan(0);
        metrics["StageTruncationMs"].Should().Be(0);
        foreach (var stage in Enum.GetValues<LiveImputationStage>())
        {
            metrics.Should().ContainKey($"Stage{stage}Ms");
        }
    }

    [Test]
    public void Metrics_ReportTheCacheWarmUpTime_AndZeroWhenTheBatchSkippedIt()
    {
        var skipped = Timings(EnabledContext(), [Live(DonorGenotypeSetSource.NoRow, 5, 1, 0, 2)]);
        var warmed = skipped with { WarmUpTime = TimeSpan.FromMilliseconds(1500) };

        GenotypeSetTimingEvents.Metrics(skipped)["WarmUpMs"].Should().Be(0);
        GenotypeSetTimingEvents.Metrics(warmed)["WarmUpMs"].Should().Be(1500);
    }

    [Test]
    public void Properties_WhenDisabled_SaysWhyAndWhichLociWereExcluded()
    {
        var context = DonorGenotypeSetBatchContext.Disabled(UsePrecomputedGenotypeSetsSource.Request, PrecomputedGenotypeSetMode.DefaultLive, 42);
        var timings = Timings(context, [Live(DonorGenotypeSetSource.PrecomputeDisabled, 5, 1, 0, 2)]) with
        {
            Request = new IdentifiedMatchProbabilityRequest { SearchRequestId = SearchRequestId, ExcludedLoci = [Locus.Dqb1, Locus.C] },
        };

        var properties = GenotypeSetTimingEvents.Properties(timings);
        var metrics = GenotypeSetTimingEvents.Metrics(timings);

        properties["SearchRequestId"].Should().Be(SearchRequestId);
        properties["MatchPredictionPath"].Should().Be("Parallel");
        properties["BatchId"].Should().Be("3");
        properties["ExcludedLoci"].Should().Be("C,Dqb1");
        properties["AllowedLociKey"].Should().BeEmpty();
        properties["UsePrecomputedGenotypeSets"].Should().Be("False");
        properties["UsePrecomputedGenotypeSetsSource"].Should().Be("Request");
        properties["PrecomputeMode"].Should().Be("DefaultLive");
        properties["BatchFallbackReason"].Should().Be("PrecomputeDisabled");
        properties["MatchingAlgorithmDataRefreshRecordId"].Should().Be("42");
        properties["PatientGenotypeSetSource"].Should().Be("NoRow");
        metrics["ReadMs"].Should().Be(0);
        metrics["ReadRowCount"].Should().Be(0);
        metrics["LiveCount"].Should().Be(1);
    }

    [Test]
    public void Send_SendsTheEventSoThatSamplingKeepsIt()
    {
        var channel = new CapturingChannel();
        var configuration = new TelemetryConfiguration
        {
            TelemetryChannel = channel,
            ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000",
        };
        // Keeps about 1 in 100 items that do not opt out.
        configuration.DefaultTelemetrySink.TelemetryProcessorChainBuilder.UseSampling(1).Build();
        var events = new GenotypeSetTimingEvents(new TelemetryClient(configuration));

        for (var i = 0; i < 200; i++)
        {
            events.Send(Timings(EnabledContext(), [Live(DonorGenotypeSetSource.NoRow, 5, 1, 0, 2)]));
        }

        channel.Items.Should().HaveCount(200);
        var sent = channel.Items.Cast<EventTelemetry>().First();
        sent.Name.Should().Be(GenotypeSetTimingEvents.EventName);
        sent.Properties["SearchRequestId"].Should().Be(SearchRequestId);
        sent.Metrics["LiveCount"].Should().Be(1);
    }

    [Test]
    public void Send_WhenTheTimingsCannotBeRead_DoesNotThrow()
    {
        var events = new GenotypeSetTimingEvents(new TelemetryClient(new TelemetryConfiguration { TelemetryChannel = new CapturingChannel() }));

        var act = () => events.Send(Timings(EnabledContext(), null));

        act.Should().NotThrow();
    }

    private static GenotypeSetBatchTimings Timings(DonorGenotypeSetBatchContext context, IReadOnlyCollection<DonorGenotypeSetTimingSample> donors) =>
        new(
            new IdentifiedMatchProbabilityRequest { SearchRequestId = SearchRequestId },
            context,
            "Parallel",
            10,
            3,
            PatientGenotypeSetSource.NoRow,
            33,
            TimeSpan.FromMilliseconds(20),
            donors,
            TimeSpan.FromMilliseconds(40),
            TimeSpan.FromMilliseconds(100));

    private static DonorGenotypeSetBatchContext EnabledContext() =>
        DonorGenotypeSetBatchContext.Enabled(
            UsePrecomputedGenotypeSetsSource.FeatureFlag,
            PrecomputedGenotypeSetMode.DefaultPrecomputed,
            42,
            new PrecomputedDonorGenotypeSetLookup(
                "ABCDrb1Dqb1",
                null,
                new Dictionary<int, PrecomputedDonorGenotypeSetRow>
                {
                    { 1, new PrecomputedDonorGenotypeSetRow(7, false, new byte[7], true) },
                    { 2, new PrecomputedDonorGenotypeSetRow(7, false, new byte[11], true) },
                },
                new PrecomputedPatientGenotypeSetRow(false, new byte[5])),
            readTime: TimeSpan.FromMilliseconds(30));

    private static DonorGenotypeSetTimingSample Precomputed(int genotypeCount, double lookupMs, int storedBytes, double totalMs) =>
        new(
            DonorGenotypeSetSource.Precomputed,
            genotypeCount,
            new DonorGenotypeSetTiming(TimeSpan.FromMilliseconds(lookupMs), null, storedBytes, TimeSpan.Zero, TimeSpan.FromMilliseconds(totalMs)));

    private static DonorGenotypeSetTimingSample Live(DonorGenotypeSetSource source, int genotypeCount, double liveMs, double encodeMs, double totalMs) =>
        new(
            source,
            genotypeCount,
            new DonorGenotypeSetTiming(
                TimeSpan.Zero, TimeSpan.FromMilliseconds(liveMs), null, TimeSpan.FromMilliseconds(encodeMs), TimeSpan.FromMilliseconds(totalMs)));

    private static DonorGenotypeSetTimingSample LiveWithStages(int survivors, int fallbacks)
    {
        using var collection = LiveImputationStageTimings.Begin();
        using (LiveImputationStageTimings.Time(LiveImputationStage.Pairing))
        {
            Thread.Sleep(2);
        }

        LiveImputationStageTimings.CountSurvivors(survivors);
        for (var i = 0; i < fallbacks; i++)
        {
            LiveImputationStageTimings.CountConsolidationFallback();
        }

        return new DonorGenotypeSetTimingSample(
            DonorGenotypeSetSource.NoRow,
            50,
            new DonorGenotypeSetTiming(
                TimeSpan.Zero, TimeSpan.FromMilliseconds(5), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(6), collection.Timings));
    }

    private sealed class CapturingChannel : ITelemetryChannel
    {
        public List<ITelemetry> Items { get; } = [];
        public bool? DeveloperMode { get; set; }
        public string EndpointAddress { get; set; }
        public void Send(ITelemetry item) => Items.Add(item);
        public void Flush() { }
        public void Dispose() { }
    }
}
