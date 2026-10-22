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
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.CacheWarming;

[TestFixture]
internal class MatchPredictionCacheWarmerTests
{
    private IHlaMetadataDictionaryFactory hmdFactory;
    private IHaplotypeFrequencyLookupService frequencyLookupService;
    private IAtlasLogger logger;
    private Dictionary<string, IHlaMetadataCacheControl> cacheControls;
    private IMatchPredictionCacheWarmer warmer;

    [SetUp]
    public void SetUp()
    {
        hmdFactory = Substitute.For<IHlaMetadataDictionaryFactory>();
        frequencyLookupService = Substitute.For<IHaplotypeFrequencyLookupService>();
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

        warmer = new MatchPredictionCacheWarmer(hmdFactory, frequencyLookupService, logger);
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
