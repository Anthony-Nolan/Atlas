using System;
using System.Threading.Tasks;
using Atlas.Common.Notifications;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using Atlas.MatchPrediction.Test.TestHelpers;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.Import;

/// <summary>
/// The active sets of two processes that share one distributed cache, over the real database: an import in one process
/// reaches the other when the local entry of the other expires.
/// </summary>
[TestFixture]
internal class ActiveSetDistributedCacheTests
{
    private const int ActiveSetCacheExpiryMinutes = 1;

    private Fixture fixture;

    // The clock of the local caches, so that the test can move past the expiry of a local entry.
    private DateTimeOffset now;
    private ISystemClock clock;

    private static IServiceProvider Provider => DependencyInjection.DependencyInjection.Provider;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        // The clock follows the real time: LazyCache sets the expiry of an entry that GetOrAddAsync stores from the real
        // clock, and the local cache compares it with this clock.
        now = DateTimeOffset.UtcNow;
        clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(_ => now);
    }

    [Test]
    public async Task ImportFrequencySet_InOneProcess_ReachesAnotherProcessWhenItsLocalEntryExpires()
    {
        var store = DistributedCacheBuilder.NewSharedStore();
        var importingProcess = NewService(NewCache(store));
        var otherProcess = NewCache(store);
        // A registry of its own, so the other tests' sets do not matter here, and this test's sets do not matter there.
        var registryCode = fixture.Create<string>();

        var firstSetId = await Import(importingProcess, registryCode);
        var setsBeforeTheSecondImport = await otherProcess.GetActiveHaplotypeFrequencySets();
        var secondSetId = await Import(importingProcess, registryCode);
        var setsBeforeTheExpiry = await otherProcess.GetActiveHaplotypeFrequencySets();
        now = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(ActiveSetCacheExpiryMinutes) + TimeSpan.FromSeconds(5);
        var setsAfterTheExpiry = await otherProcess.GetActiveHaplotypeFrequencySets();

        setsBeforeTheSecondImport[(registryCode, null)].Id.Should().Be(firstSetId);
        setsBeforeTheExpiry[(registryCode, null)].Id.Should().Be(firstSetId);
        // The other process read the distributed cache, which held the sets of the first import until the second import
        // wrote its own: so a second import that did not write would leave the first set here.
        setsAfterTheExpiry[(registryCode, null)].Id.Should().Be(secondSetId);
    }

    /// <returns>The id of the set that the import activated.</returns>
    private static async Task<int> Import(IHaplotypeFrequencyService importingProcess, string registryCode)
    {
        using var file = FrequencySetFileBuilder.New([registryCode]).Build();
        await importingProcess.ImportFrequencySet(file);

        var activeSet = await Provider.GetRequiredService<IHaplotypeFrequencySetRepository>().GetActiveSet(registryCode, null);
        return activeSet.Id;
    }

    /// <summary>The cache of one process: a local cache of its own, over the shared <paramref name="store"/>.</summary>
    private HaplotypeFrequencyCache NewCache(IDistributedCache store) =>
        new(
            new HaplotypeFrequencySetCacheProvider(AppCacheBuilder.NewCacheOver(new MemoryCache(new MemoryCacheOptions { Clock = clock }))),
            DistributedCacheBuilder.NewProviderOver(store),
            Substitute.For<IFrequencySetResidencyTracker>(),
            Provider.GetRequiredService<IHaplotypeFrequenciesRepository>(),
            Provider.GetRequiredService<IHaplotypeFrequencySetRepository>(),
            Provider.GetRequiredService<IFrequencyConsolidator>(),
            Provider.GetRequiredService<IMatchPredictionLogger<MatchProbabilityLoggingContext>>(),
            Options.Create(new HaplotypeFrequencySetCacheSettings
            {
                ActiveSetCacheExpiryMinutes = ActiveSetCacheExpiryMinutes,
                MaxCachedFrequencySetMegabytes = 1,
                SetCacheExpiryMinutes = 1
            }));

    private static HaplotypeFrequencyService NewService(IHaplotypeFrequencyCache cache) =>
        new(
            Provider.GetRequiredService<IFrequencySetImporter>(),
            Substitute.For<INotificationSender>(),
            Provider.GetRequiredService<IMatchPredictionLogger<MatchProbabilityLoggingContext>>(),
            cache,
            new ImmediateTimersTimeProvider());
}
