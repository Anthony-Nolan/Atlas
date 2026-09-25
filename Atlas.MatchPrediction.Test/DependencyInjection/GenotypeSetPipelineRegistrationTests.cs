using System;
using System.Linq;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.DependencyInjection;

/// <summary>
/// <see cref="ServiceConfiguration.RegisterGenotypeSetPipeline"/> must be enough, on its own, for a host that
/// precomputes donor genotype sets - and must not bring in what only search and frequency set import use.
/// </summary>
[TestFixture]
internal class GenotypeSetPipelineRegistrationTests
{
    private static readonly ServiceProviderOptions ValidatingOptions = new() { ValidateOnBuild = true, ValidateScopes = true };

    [Test]
    public void RegisterGenotypeSetPipeline_AllowsServiceProviderValidationOnBuild()
    {
        var services = PipelineOnly();

        var buildProvider = () => services.BuildServiceProvider(ValidatingOptions);

        buildProvider.Should().NotThrow();
    }

    [Test]
    public void RegisterGenotypeSetPipeline_RegistersTheGenotypeSetServiceAndTheFrequencySetLookup()
    {
        var services = PipelineOnly();

        services.Should().Contain(d => d.ServiceType == typeof(IGenotypeSetService));
        services.Should().Contain(d => d.ServiceType == typeof(IHaplotypeFrequencyLookupService));
    }

    [Test]
    public void RegisterGenotypeSetPipeline_RegistersNothingThatNeedsServiceBusOrBlobStorage()
    {
        var services = PipelineOnly();

        services.Should().NotContain(d => d.ServiceType == typeof(INotificationSender));
        services.Should().NotContain(d => d.ServiceType == typeof(NotificationsServiceBusSettings));
        services.Should().NotContain(d => d.ServiceType == typeof(AzureStorageSettings));
        services.Should().NotContain(d => d.ServiceType == typeof(IHaplotypeFrequencyService));
        services.Should().NotContain(d => d.ServiceType == typeof(IFrequencySetImporter));
    }

    [Test]
    public void RegisterMatchPredictionAlgorithm_AllowsServiceProviderValidationOnBuild()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new HaplotypeFrequencySetCacheSettings()));
        RegisterWholeAlgorithm(services);

        var buildProvider = () => services.BuildServiceProvider(ValidatingOptions);

        buildProvider.Should().NotThrow();
    }

    [Test]
    public void RegisterMatchPredictionAlgorithm_StillRegistersTheFrequencySetImport()
    {
        var services = new ServiceCollection();
        RegisterWholeAlgorithm(services);

        services.Should().Contain(d => d.ServiceType == typeof(IHaplotypeFrequencyService));
        services.Should().Contain(d => d.ServiceType == typeof(IFrequencySetImporter));
        services.Should().Contain(d => d.ServiceType == typeof(INotificationSender));
    }

    [TestCase(typeof(IGenotypeSetService))]
    [TestCase(typeof(IHaplotypeFrequencyLookupService))]
    [TestCase(typeof(IHaplotypeFrequencySetCacheProvider))]
    [TestCase(typeof(IFrequencySetResidencyTracker))]
    public void RegisterGenotypeSetPipeline_WithRegisterMatchPredictionAlgorithmToo_RegistersEachPipelineServiceOnce(Type serviceType)
    {
        // A host that precomputes and also runs match prediction calls both. A second singleton cache provider or
        // residency tracker would split the frequency set cache in two.
        var services = PipelineOnly();
        RegisterWholeAlgorithm(services);

        services.Count(d => d.ServiceType == serviceType).Should().Be(1);
    }

    private static ServiceCollection PipelineOnly()
    {
        var services = new ServiceCollection();
        // The one input the host provides itself - see RegisterGenotypeSetPipeline's remarks.
        services.AddSingleton(Options.Create(new HaplotypeFrequencySetCacheSettings()));

        services.RegisterGenotypeSetPipeline(
            _ => new ApplicationInsightsSettings { LogLevel = "Info" },
            _ => new HlaMetadataDictionarySettings(),
            _ => new MacDictionarySettings(),
            _ => new GenotypeImputationSettings(),
            _ => "match-prediction-sql");

        return services;
    }

    private static void RegisterWholeAlgorithm(IServiceCollection services)
    {
        services.RegisterMatchPredictionAlgorithm(
            _ => new ApplicationInsightsSettings { LogLevel = "Info" },
            _ => new HlaMetadataDictionarySettings(),
            _ => new MacDictionarySettings(),
            _ => new NotificationsServiceBusSettings(),
            _ => new AzureStorageSettings(),
            _ => new GenotypeImputationSettings(),
            _ => "match-prediction-sql");
    }
}
