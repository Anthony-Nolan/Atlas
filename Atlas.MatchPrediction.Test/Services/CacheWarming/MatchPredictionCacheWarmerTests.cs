using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
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
    private IHaplotypeFrequencyCache frequencyCache;
    private IAtlasLogger logger;
    private Dictionary<string, IHlaMetadataCacheControl> cacheControls;
    private IMatchPredictionCacheWarmer warmer;

    [SetUp]
    public void SetUp()
    {
        hmdFactory = Substitute.For<IHlaMetadataDictionaryFactory>();
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

        GivenActiveSetVersions("3480", "3480", "3660");

        warmer = new MatchPredictionCacheWarmer(hmdFactory, frequencyCache, logger);
    }

    [Test]
    public async Task WarmUp_WarmsTheMatchPredictionTablesOnceForEachDistinctVersion()
    {
        await warmer.WarmUp("3670");

        cacheControls.Keys.Should().BeEquivalentTo("3480", "3660", "3670");
        foreach (var cacheControl in cacheControls.Values)
        {
            await cacheControl.Received(1).PreWarmMatchPredictionCaches();
            await cacheControl.DidNotReceive().PreWarmAllCaches();
        }
    }

    [Test]
    public async Task WarmUp_WhenTheMatchingVersionIsAFrequencySetVersion_WarmsItOnce()
    {
        await warmer.WarmUp("3660");

        cacheControls.Keys.Should().BeEquivalentTo("3480", "3660");
        await cacheControls["3660"].Received(1).PreWarmMatchPredictionCaches();
    }

    [Test]
    public async Task WarmUp_WithNoMatchingVersion_WarmsOnlyTheFrequencySetVersions()
    {
        await warmer.WarmUp(null);

        cacheControls.Keys.Should().BeEquivalentTo("3480", "3660");
    }

    [Test]
    public async Task WarmUp_WhenAWarmFails_DoesNotThrowAndLogsAWarning()
    {
        await warmer.WarmUp("3660");
        cacheControls["3660"].PreWarmMatchPredictionCaches().ThrowsAsync(new Exception("storage is down"));

        var act = () => warmer.WarmUp("3660");

        await act.Should().NotThrowAsync();
        logger.Received(1).SendException(
            Arg.Is<Exception>(e => e.Message == "storage is down"),
            LogLevel.Warn,
            Arg.Is<Dictionary<string, string>>(p =>
                p["EventName"] == MatchPredictionCacheWarmer.FailedEventName && p["HlaNomenclatureVersions"] == "3480,3660"));
    }

    [Test]
    public async Task WarmUp_WhenReadingTheActiveSetsFails_DoesNotThrow()
    {
        frequencyCache.GetActiveHaplotypeFrequencySets().ThrowsAsync(new Exception("sql is down"));

        var act = () => warmer.WarmUp("3660");

        await act.Should().NotThrowAsync();
        logger.ReceivedWithAnyArgs(1).SendException(default, default, default);
    }

    [Test]
    public async Task WarmUp_WhenEverythingIsAlreadyCached_DoesNotLogAnEvent()
    {
        await warmer.WarmUp("3660");

        logger.DidNotReceiveWithAnyArgs().SendEvent(default);
    }

    [Test]
    public async Task WarmUp_WhenTheWarmIsSlow_LogsOneEventWithItsDuration()
    {
        await warmer.WarmUp("3660");
        logger.ClearReceivedCalls();
        cacheControls["3660"].PreWarmMatchPredictionCaches()
            .Returns(Task.Delay(MatchPredictionCacheWarmer.LoggedWarmUpThreshold + TimeSpan.FromMilliseconds(100)));

        await warmer.WarmUp("3660");

        logger.Received(1).SendEvent(
            MatchPredictionCacheWarmer.WarmedEventName,
            LogLevel.Info,
            Arg.Is<Dictionary<string, string>>(p => p["HlaNomenclatureVersions"] == "3480,3660"),
            Arg.Is<Dictionary<string, double>>(m => m["WarmUpMs"] >= MatchPredictionCacheWarmer.LoggedWarmUpThreshold.TotalMilliseconds));
    }

    private void GivenActiveSetVersions(params string[] versions)
    {
        var sets = new Dictionary<(string RegistryCode, string EthnicityCode), HaplotypeFrequencySet>();
        for (var i = 0; i < versions.Length; i++)
        {
            sets[($"registry-{i}", $"ethnicity-{i}")] = new HaplotypeFrequencySet { Id = i, HlaNomenclatureVersion = versions[i] };
        }

        frequencyCache.GetActiveHaplotypeFrequencySets().Returns(sets);
    }
}
