using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.Notifications;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies;

[TestFixture]
internal class HaplotypeFrequencyServiceTests
{
    private IFrequencySetImporter frequencySetImporter;
    private IHaplotypeFrequencySetRepository frequencySetRepository;
    private IHaplotypeFrequenciesRepository frequencyRepository;
    private INotificationSender notificationSender;
    private IMatchPredictionLogger<MatchProbabilityLoggingContext> logger;
    private IFrequencyConsolidator frequencyConsolidator;
    private IPersistentCacheProvider persistentCacheProvider;
    private IHaplotypeFrequencyCache haplotypeFrequencyCache;

    private Fixture fixture;

    private HaplotypeFrequencyService sut;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        frequencySetImporter = Substitute.For<IFrequencySetImporter>();
        frequencySetRepository = Substitute.For<IHaplotypeFrequencySetRepository>();
        frequencyRepository = Substitute.For<IHaplotypeFrequenciesRepository>();
        notificationSender = Substitute.For<INotificationSender>();
        logger = Substitute.For<IMatchPredictionLogger<MatchProbabilityLoggingContext>>();
        frequencyConsolidator = Substitute.For<IFrequencyConsolidator>();
        persistentCacheProvider = AppCacheBuilder.NewPersistentCacheProvider();
        var cacheSettings = fixture.Build<HaplotypeFrequencySetCacheSettings>()
            .With(x => x.ActiveSetCacheExpiryMinutes, 5)
            .Create();
        haplotypeFrequencyCache = Substitute.For<IHaplotypeFrequencyCache>();

        sut = new HaplotypeFrequencyService(
            frequencySetImporter,
            frequencySetRepository,
            frequencyRepository,
            notificationSender,
            logger,
            persistentCacheProvider,
            frequencyConsolidator,
            Options.Create(cacheSettings),
            haplotypeFrequencyCache
        );
    }

    [Test]
    public async Task GetSingleHaplotypeFrequencySet_MultipleCalls_UsesCachedActiveSets()
    {
        var registryCode = fixture.Create<string>();
        var ethnicityCode = fixture.Create<string>();

        var activeSet = fixture.Build<Data.Models.HaplotypeFrequencySet>()
            .With(x => x.RegistryCode, registryCode)
            .With(x => x.EthnicityCode, ethnicityCode)
            .With(x => x.Active, true)
            .Create();

        frequencySetRepository.GetAllActiveSets().Returns([activeSet]);

        var metadata = fixture.Build<FrequencySetMetadata>()
            .With(x => x.RegistryCode, registryCode)
            .With(x => x.EthnicityCode, ethnicityCode)
            .Create();

        var firstResult = await sut.GetSingleHaplotypeFrequencySet(metadata);
        var secondResult = await sut.GetSingleHaplotypeFrequencySet(metadata);

        firstResult.Id.Should().Be(activeSet.Id);
        secondResult.Id.Should().Be(activeSet.Id);
        await frequencySetRepository.Received(1).GetAllActiveSets();
    }

    [Test]
    public async Task ImportFrequencySet_SuccessfulImport_InvalidatesActiveSetCache()
    {
        var registryCode = fixture.Create<string>();
        var ethnicityCode = fixture.Create<string>();

        frequencySetImporter.Import(null, null).ReturnsForAnyArgs(Task.CompletedTask);

        var firstActiveSet = fixture.Build<Data.Models.HaplotypeFrequencySet>()
            .With(x => x.RegistryCode, registryCode)
            .With(x => x.EthnicityCode, ethnicityCode)
            .With(x => x.Active, true)
            .Create();
        var secondActiveSet = fixture.Build<Data.Models.HaplotypeFrequencySet>()
            .With(x => x.RegistryCode, registryCode)
            .With(x => x.EthnicityCode, ethnicityCode)
            .With(x => x.Active, true)
            .Create();

        frequencySetRepository.GetAllActiveSets().Returns([firstActiveSet], [secondActiveSet]);

        var metadata = fixture.Build<FrequencySetMetadata>()
            .With(x => x.RegistryCode, registryCode)
            .With(x => x.EthnicityCode, ethnicityCode)
            .Create();

        var file = fixture.Build<FrequencySetFile>()
            .Without(f => f.Contents)
            .Create();

        var resultBeforeImport = await sut.GetSingleHaplotypeFrequencySet(metadata);
        await sut.ImportFrequencySet(file, fixture.Create<FrequencySetImportBehaviour>());
        var resultAfterImport = await sut.GetSingleHaplotypeFrequencySet(metadata);

        resultBeforeImport.Id.Should().Be(firstActiveSet.Id);
        resultAfterImport.Id.Should().Be(secondActiveSet.Id);
        await frequencySetRepository.Received(2).GetAllActiveSets();
    }
}
