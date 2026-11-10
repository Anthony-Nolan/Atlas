using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchPrediction.Services.CacheWarming;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.PrecomputeWorker.Test;

[TestFixture]
internal class DonorGenotypePrecomputationCacheWarmerTests
{
    private Fixture fixture = null!;
    private IDataRefreshHistoryRepository dataRefreshHistoryRepository = null!;
    private IMatchPredictionCacheWarmer matchPredictionCacheWarmer = null!;
    private ServiceProvider provider = null!;
    private FakeLogger<DonorGenotypePrecomputationCacheWarmer> logger = null!;
    private DonorGenotypePrecomputationCacheWarmer warmer = null!;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();
        matchPredictionCacheWarmer = Substitute.For<IMatchPredictionCacheWarmer>();

        var services = new ServiceCollection();
        services.AddScoped(_ => dataRefreshHistoryRepository);
        services.AddScoped(_ => matchPredictionCacheWarmer);
        provider = services.BuildServiceProvider();

        logger = new FakeLogger<DonorGenotypePrecomputationCacheWarmer>();
        warmer = new DonorGenotypePrecomputationCacheWarmer(provider.GetRequiredService<IServiceScopeFactory>(), logger);
    }

    [TearDown]
    public async Task TearDown()
    {
        await provider.DisposeAsync();
    }

    [Test]
    public async Task WarmUp_WarmsTheActiveFrequencySets_AndTheVersionOfEachOpenDataRefreshRecord()
    {
        var versions = fixture.CreateMany<string>(2).ToList();
        dataRefreshHistoryRepository.GetIncompleteRefreshJobHlaNomenclatureVersions().Returns(versions);

        await warmer.WarmUp();

        await matchPredictionCacheWarmer.Received(1).WarmUpForActiveFrequencySets(
            Arg.Is<IReadOnlyCollection<string>>(warmed => warmed.SequenceEqual(versions)));
    }

    [Test]
    public async Task WarmUp_WithNoOpenDataRefreshRecordVersion_WarmsNothing_AndLogsWhy()
    {
        // As when the worker reads the messages of a refresh that has ended: it skips every message.
        dataRefreshHistoryRepository.GetIncompleteRefreshJobHlaNomenclatureVersions().Returns(new List<string>());

        await warmer.WarmUp();

        await matchPredictionCacheWarmer.DidNotReceiveWithAnyArgs().WarmUpForActiveFrequencySets(default!);
        logger.Collector.GetSnapshot().Should().ContainSingle().Which.Level.Should().Be(LogLevel.Information);
    }

    [Test]
    public async Task WarmUp_WhenTheDataRefreshRecordsCannotBeRead_WarmsNothing_DoesNotThrow_AndLogsAWarning()
    {
        var failure = new InvalidOperationException(fixture.Create<string>());
        dataRefreshHistoryRepository.GetIncompleteRefreshJobHlaNomenclatureVersions().ThrowsAsync(failure);

        var act = () => warmer.WarmUp();

        await act.Should().NotThrowAsync();
        await matchPredictionCacheWarmer.DidNotReceiveWithAnyArgs().WarmUpForActiveFrequencySets(default!);
        logger.Collector.GetSnapshot().Should().ContainSingle(log => log.Level == LogLevel.Warning)
            .Which.Exception.Should().BeSameAs(failure);
    }
}
