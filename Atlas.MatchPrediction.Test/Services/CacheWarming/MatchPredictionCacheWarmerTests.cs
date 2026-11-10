using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.CacheWarming;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.CacheWarming;

[TestFixture]
internal class MatchPredictionCacheWarmerTests
{
    private Fixture fixture;
    private IHlaMetadataDictionaryFactory hmdFactory;
    private IHaplotypeFrequencyLookupService frequencyLookupService;
    private IHaplotypeFrequencyCache frequencyCache;
    private IAtlasLogger logger;
    private Dictionary<string, IHlaMetadataCacheControl> cacheControls;
    private IMatchPredictionCacheWarmer warmer;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        hmdFactory = Substitute.For<IHlaMetadataDictionaryFactory>();
        frequencyLookupService = Substitute.For<IHaplotypeFrequencyLookupService>();
        frequencyCache = Substitute.For<IHaplotypeFrequencyCache>();
        logger = Substitute.For<IAtlasLogger>();

        cacheControls = new Dictionary<string, IHlaMetadataCacheControl>();
        hmdFactory.BuildCacheControl(default).ReturnsForAnyArgs(call =>
        {
            var version = call.Arg<string>();
            if (!cacheControls.TryGetValue(version, out var cacheControl))
            {
                cacheControl = Substitute.For<IHlaMetadataCacheControl>();
                cacheControls[version] = cacheControl;
            }

            return cacheControl;
        });

        // The registry picks the set's version; any registry not listed gets the global set, at 3480.
        var setVersionsByRegistry = new Dictionary<string, string> { { "R1", "3480" }, { "R2", "3500" }, { "R3", "3660" } };
        frequencyLookupService.GetSingleHaplotypeFrequencySet(default).ReturnsForAnyArgs(call =>
        {
            var registry = call.Arg<FrequencySetMetadata>()?.RegistryCode ?? "";
            return new HaplotypeFrequencySet { HlaNomenclatureVersion = setVersionsByRegistry.GetValueOrDefault(registry, "3480") };
        });

        warmer = new MatchPredictionCacheWarmer(hmdFactory, frequencyLookupService, frequencyCache, logger);
    }

    [Test]
    public async Task WarmUp_WarmsTheMatchPredictionTablesOnceForEachDistinctVersionTheBatchUses()
    {
        await warmer.WarmUp(Batch("3670", patientRegistry: "R1", donorRegistries: ["R1", "R2", "R2"]));

        cacheControls.Keys.Should().BeEquivalentTo("3480", "3500", "3670");
        foreach (var cacheControl in cacheControls.Values)
        {
            await cacheControl.Received(1).PreWarmMatchPredictionCaches();
            await cacheControl.DidNotReceive().PreWarmAllCaches();
        }
    }

    [Test]
    public async Task WarmUp_DoesNotWarmTheVersionsOfSetsTheBatchDoesNotUse()
    {
        await warmer.WarmUp(Batch("3480", patientRegistry: "R1", donorRegistries: ["R1"]));

        cacheControls.Keys.Should().BeEquivalentTo("3480");
    }

    [Test]
    public async Task WarmUp_LooksUpEachDistinctRegistryAndEthnicityOnce()
    {
        await warmer.WarmUp(Batch("3660", patientRegistry: "R1", donorRegistries: ["R1", "R2", "R2", "R2"]));

        await frequencyLookupService.ReceivedWithAnyArgs(2).GetSingleHaplotypeFrequencySet(default);
    }

    [Test]
    public async Task WarmUp_WithNoMatchingVersionOrSetMetadata_WarmsTheGlobalSetVersion()
    {
        var batch = new MultipleDonorMatchProbabilityInput(new IdentifiedMatchProbabilityRequest())
        {
            Donors = [new DonorInput { DonorIds = [1] }]
        };

        await warmer.WarmUp(batch);

        cacheControls.Keys.Should().BeEquivalentTo("3480");
    }

    [Test]
    public async Task WarmUp_WhenAWarmFails_DoesNotThrowAndLogsAWarning()
    {
        var batch = Batch("3660", patientRegistry: "R1", donorRegistries: ["R1"]);
        await warmer.WarmUp(batch);
        cacheControls["3660"].PreWarmMatchPredictionCaches().ThrowsAsync(new Exception("storage is down"));

        var act = () => warmer.WarmUp(batch);

        await act.Should().NotThrowAsync();
        logger.Received(1).SendException(
            Arg.Is<Exception>(e => e.Message == "storage is down"),
            LogLevel.Warn,
            Arg.Is<Dictionary<string, string>>(p =>
                p["EventName"] == MatchPredictionCacheWarmer.FailedEventName && p["HlaNomenclatureVersions"] == "3480,3660"));
    }

    [Test]
    public async Task WarmUp_WhenNoFrequencySetCanBeFound_DoesNotThrow()
    {
        frequencyLookupService.GetSingleHaplotypeFrequencySet(default).ThrowsAsyncForAnyArgs(new Exception("No Global Haplotype frequency set was found"));

        var act = () => warmer.WarmUp(Batch("3660", patientRegistry: "R1", donorRegistries: ["R1"]));

        await act.Should().NotThrowAsync();
        logger.ReceivedWithAnyArgs(1).SendException(default, default, default);
    }

    [Test]
    public async Task WarmUp_WhenEverythingIsAlreadyCached_DoesNotLogAnEvent()
    {
        await warmer.WarmUp(Batch("3660", patientRegistry: "R1", donorRegistries: ["R1"]));

        logger.DidNotReceiveWithAnyArgs().SendEvent(default);
    }

    [Test]
    public async Task WarmUp_WhenTheWarmIsSlow_LogsOneEventWithItsDuration()
    {
        var batch = Batch("3660", patientRegistry: "R1", donorRegistries: ["R1"]);
        await warmer.WarmUp(batch);
        logger.ClearReceivedCalls();
        cacheControls["3660"].PreWarmMatchPredictionCaches()
            .Returns(Task.Delay(MatchPredictionCacheWarmer.LoggedWarmUpThreshold + TimeSpan.FromMilliseconds(100)));

        await warmer.WarmUp(batch);

        logger.Received(1).SendEvent(
            MatchPredictionCacheWarmer.WarmedEventName,
            LogLevel.Info,
            Arg.Is<Dictionary<string, string>>(p => p["HlaNomenclatureVersions"] == "3480,3660"),
            Arg.Is<Dictionary<string, double>>(m => m["WarmUpMs"] >= MatchPredictionCacheWarmer.LoggedWarmUpThreshold.TotalMilliseconds));
    }

    [Test]
    public async Task WarmUpForActiveFrequencySets_WarmsTheMatchPredictionTablesOnceForEachActiveSetVersionAndEachMatchingVersion()
    {
        var setVersion = fixture.Create<string>();
        var otherSetVersion = fixture.Create<string>();
        var matchingVersion = fixture.Create<string>();
        // A version can be the version of more than one set, and also a matching version.
        ActiveSetsAt(setVersion, otherSetVersion, setVersion);

        await warmer.WarmUpForActiveFrequencySets([matchingVersion, setVersion]);

        cacheControls.Keys.Should().BeEquivalentTo(setVersion, otherSetVersion, matchingVersion);
        foreach (var cacheControl in cacheControls.Values)
        {
            await cacheControl.Received(1).PreWarmMatchPredictionCaches();
            await cacheControl.DidNotReceive().PreWarmAllCaches();
        }
    }

    [Test]
    public async Task WarmUpForActiveFrequencySets_WithNoActiveSets_WarmsTheMatchingVersions()
    {
        var matchingVersions = fixture.CreateMany<string>(2).ToList();
        ActiveSetsAt();

        await warmer.WarmUpForActiveFrequencySets(matchingVersions);

        cacheControls.Keys.Should().BeEquivalentTo(matchingVersions);
    }

    [Test]
    public async Task WarmUpForActiveFrequencySets_DoesNotWarmAnEmptyVersion()
    {
        var setVersion = fixture.Create<string>();
        ActiveSetsAt(setVersion, null, "");

        await warmer.WarmUpForActiveFrequencySets([""]);

        cacheControls.Keys.Should().BeEquivalentTo(setVersion);
    }

    [Test]
    public async Task WarmUpForActiveFrequencySets_ReadsTheActiveSetsOnce_AndLooksUpNoSetByRegistry()
    {
        ActiveSetsAt(fixture.Create<string>(), fixture.Create<string>());

        await warmer.WarmUpForActiveFrequencySets([fixture.Create<string>()]);

        await frequencyCache.Received(1).GetActiveHaplotypeFrequencySets();
        await frequencyLookupService.DidNotReceiveWithAnyArgs().GetSingleHaplotypeFrequencySet(default);
    }

    [Test]
    public async Task WarmUpForActiveFrequencySets_WhenTheActiveSetsCannotBeRead_WarmsNothing_DoesNotThrow_AndLogsAWarning()
    {
        var failure = new Exception(fixture.Create<string>());
        frequencyCache.GetActiveHaplotypeFrequencySets().ThrowsAsync(failure);

        var act = () => warmer.WarmUpForActiveFrequencySets([fixture.Create<string>()]);

        await act.Should().NotThrowAsync();
        cacheControls.Should().BeEmpty();
        logger.Received(1).SendException(
            Arg.Is(failure),
            LogLevel.Warn,
            Arg.Is<Dictionary<string, string>>(p => p["EventName"] == MatchPredictionCacheWarmer.FailedEventName));
    }

    [Test]
    public async Task WarmUpForActiveFrequencySets_WhenAWarmFails_DoesNotThrow_AndLogsAWarningWithTheVersions()
    {
        var setVersion = fixture.Create<string>();
        var matchingVersion = fixture.Create<string>();
        ActiveSetsAt(setVersion);
        hmdFactory.BuildCacheControl(setVersion).PreWarmMatchPredictionCaches().ThrowsAsync(new Exception(fixture.Create<string>()));

        var act = () => warmer.WarmUpForActiveFrequencySets([matchingVersion]);

        await act.Should().NotThrowAsync();
        var expectedVersions = string.Join(",", new[] { setVersion, matchingVersion }.Order());
        logger.Received(1).SendException(
            Arg.Any<Exception>(),
            LogLevel.Warn,
            Arg.Is<Dictionary<string, string>>(p =>
                p["EventName"] == MatchPredictionCacheWarmer.FailedEventName && p["HlaNomenclatureVersions"] == expectedVersions));
    }

    /// <summary>The active sets: one set at each version, each for a registry and ethnicity of its own.</summary>
    private void ActiveSetsAt(params string[] versions)
    {
        var activeSets = versions.ToDictionary(
            _ => (fixture.Create<string>(), fixture.Create<string>()),
            version => new HaplotypeFrequencySet { HlaNomenclatureVersion = version });

        frequencyCache.GetActiveHaplotypeFrequencySets().Returns(activeSets);
    }

    private static MultipleDonorMatchProbabilityInput Batch(string matchingVersion, string patientRegistry, string[] donorRegistries)
    {
        var request = new IdentifiedMatchProbabilityRequest
        {
            MatchingAlgorithmHlaNomenclatureVersion = matchingVersion,
            PatientFrequencySetMetadata = new FrequencySetMetadata { RegistryCode = patientRegistry }
        };

        return new MultipleDonorMatchProbabilityInput(request)
        {
            Donors = donorRegistries
                .Select((registry, i) => new DonorInput
                {
                    DonorIds = [i],
                    DonorFrequencySetMetadata = new FrequencySetMetadata { RegistryCode = registry }
                })
                .ToList()
        };
    }
}
