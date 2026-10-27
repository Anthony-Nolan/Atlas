using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Services.Debug.Precompute;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using GenotypeImputationSettings = Atlas.MatchPrediction.ExternalInterface.Settings.GenotypeImputationSettings;
using HaplotypeFrequencySetCacheSettings = Atlas.MatchPrediction.ExternalInterface.Settings.HaplotypeFrequencySetCacheSettings;

namespace Atlas.MatchingAlgorithm.Test.DependencyInjection;

/// <summary>
/// The Data Refresh app's registration, as its Startup makes it: <see cref="ServiceConfiguration.RegisterDataRefresh"/>,
/// <see cref="ServiceConfiguration.RegisterDonorImportGenotypeSetPrecompute"/>, then
/// <see cref="ServiceConfiguration.RegisterGenotypeSetDebug"/>.
/// </summary>
/// <remarks>
/// Resolving the service builds the HLA Metadata Dictionary, which connects to storage, so these only check the
/// registrations. <c>FunctionAppDependencyInjectionTests</c> builds the app itself and resolves its function classes.
/// </remarks>
[TestFixture]
public class GenotypeSetDebugRegistrationTests
{
    /// <summary>The clients are built with these, but never connect: nothing here is called.</summary>
    private const string SqlConnectionString = "Data Source=(local);Initial Catalog=Atlas;Integrated Security=True;";

    private const string ServiceBusConnectionString =
        "Endpoint=sb://atlas-test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA==";

    private const string DevelopmentStorage = "UseDevelopmentStorage=true";

    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
    }

    [Test]
    public void RegisterGenotypeSetDebug_RegistersTheService()
    {
        var services = DataRefreshApp();

        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(IGenotypeSetDebugService))
            .Which.Should().Match<ServiceDescriptor>(descriptor =>
                descriptor.ImplementationType == typeof(GenotypeSetDebugService) && descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    [TestCase(typeof(IGenotypeSetService))]
    [TestCase(typeof(IHaplotypeFrequencyLookupService))]
    [TestCase(typeof(IHaplotypeFrequencyCache))]
    [TestCase(typeof(IHaplotypeFrequencySetReader))]
    [TestCase(typeof(DebugSettings))]
    public void RegisterGenotypeSetDebug_RegistersTheServiceDependencies(Type serviceType)
    {
        var services = DataRefreshApp();

        services.Should().Contain(descriptor => descriptor.ServiceType == serviceType);
    }

    [Test]
    public void RegisterGenotypeSetDebug_GivesTheServiceTheSettingsOfTheHost()
    {
        var settings = new DebugSettings { AllowPrecomputeWrites = fixture.Create<bool>() };
        var services = DataRefreshApp(settings);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<DebugSettings>().Should().BeSameAs(settings);
    }

    private ServiceCollection DataRefreshApp(DebugSettings debugSettings = null)
    {
        var services = new ServiceCollection();

        // Registered by the host itself.
        services.AddSingleton(Options.Create(new HaplotypeFrequencySetCacheSettings()));
        services.AddSingleton(Options.Create(new NotificationsServiceBusSettings { ConnectionString = ServiceBusConnectionString }));
        services.AddSingleton(Options.Create(new ApplicationInsightsSettings { LogLevel = nameof(LogLevel.Info) }));

        services.RegisterDataRefresh(
            _ => new AzureAuthenticationSettings(),
            _ => new AzureDatabaseManagementSettings(),
            _ => fixture.Create<DataRefreshSettings>(),
            _ => fixture.Create<DonorGenotypePrecomputationSettings>(),
            _ => new DonorGenotypePrecomputationSwitchSettings(),
            _ => new ApplicationInsightsSettings { LogLevel = nameof(LogLevel.Info) },
            _ => new AzureStorageSettings { ConnectionString = DevelopmentStorage },
            _ => new HlaMetadataDictionarySettings { AzureStorageConnectionString = DevelopmentStorage },
            _ => new MacDictionarySettings { AzureStorageConnectionString = DevelopmentStorage, TableName = fixture.Create<string>() },
            _ => new MessagingServiceBusSettings { ConnectionString = ServiceBusConnectionString },
            _ => new NotificationsServiceBusSettings(),
            _ => new DonorManagementSettings(),
            _ => SqlConnectionString,
            _ => SqlConnectionString,
            _ => SqlConnectionString,
            _ => SqlConnectionString);

        services.RegisterDonorImportGenotypeSetPrecompute(
            _ => new ApplicationInsightsSettings { LogLevel = nameof(LogLevel.Info) },
            _ => new HlaMetadataDictionarySettings { AzureStorageConnectionString = DevelopmentStorage },
            _ => new MacDictionarySettings { AzureStorageConnectionString = DevelopmentStorage, TableName = fixture.Create<string>() },
            _ => new GenotypeImputationSettings(),
            _ => new DonorGenotypePrecomputationSwitchSettings(),
            _ => SqlConnectionString);

        services.RegisterGenotypeSetDebug(
            _ => new ApplicationInsightsSettings { LogLevel = nameof(LogLevel.Info) },
            _ => new HlaMetadataDictionarySettings { AzureStorageConnectionString = DevelopmentStorage },
            _ => new MacDictionarySettings { AzureStorageConnectionString = DevelopmentStorage, TableName = fixture.Create<string>() },
            _ => new GenotypeImputationSettings(),
            _ => debugSettings ?? new DebugSettings(),
            _ => SqlConnectionString);

        return services;
    }
}
