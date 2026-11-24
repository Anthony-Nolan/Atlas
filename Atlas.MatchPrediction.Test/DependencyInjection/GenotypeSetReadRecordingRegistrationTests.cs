using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Services.Precompute;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.DependencyInjection;

/// <summary>Recording for the ATL-347 measurement (ATL-571) is a no-op unless the host asks for it.</summary>
[TestFixture]
internal class GenotypeSetReadRecordingRegistrationTests
{
    private static readonly ServiceProviderOptions ValidatingOptions = new() { ValidateOnBuild = true, ValidateScopes = true };

    [Test]
    public void RegisterMatchPredictionAlgorithm_RegistersTheNoOpRecorder()
    {
        var services = WholeAlgorithm();

        using var provider = services.BuildServiceProvider(ValidatingOptions);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IGenotypeSetReadRecorder>().Should().BeOfType<NoOpGenotypeSetReadRecorder>();
    }

    [Test]
    public void RegisterGenotypeSetReadRecording_ReplacesTheNoOpWithTheBlobRecorder()
    {
        var services = WholeAlgorithm();
        services.RegisterGenotypeSetReadRecording(_ => new GenotypeSetReadRecordingSettings { Enabled = true });

        using var provider = services.BuildServiceProvider(ValidatingOptions);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IGenotypeSetReadRecorder>().Should().BeOfType<GenotypeSetReadRecorder>();
        scope.ServiceProvider.GetServices<IGenotypeSetReadRecorder>().Should().ContainSingle();
    }

    private static IServiceCollection WholeAlgorithm()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new HaplotypeFrequencySetCacheSettings()));

        // As the host does - see RegisterGenotypeSetPipeline's remarks.
        services.RegisterDistributedCacheTypes(_ => new DistributedCacheSettings());

        services.RegisterMatchPredictionAlgorithm(
            _ => new ApplicationInsightsSettings { LogLevel = "Info" },
            _ => new HlaMetadataDictionarySettings(),
            _ => new MacDictionarySettings(),
            _ => new NotificationsServiceBusSettings { NotificationsTopic = "from-host" },
            _ => new AzureStorageSettings { ConnectionString = "UseDevelopmentStorage=true" },
            _ => new GenotypeImputationSettings(),
            _ => "match-prediction-sql");

        return services;
    }
}
