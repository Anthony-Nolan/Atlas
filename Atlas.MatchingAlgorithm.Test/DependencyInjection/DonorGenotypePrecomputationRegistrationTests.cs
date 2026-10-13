using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using GenotypeImputationSettings = Atlas.MatchPrediction.ExternalInterface.Settings.GenotypeImputationSettings;

namespace Atlas.MatchingAlgorithm.Test.DependencyInjection;

/// <summary>
/// The Data Refresh app's registration, <see cref="ServiceConfiguration.RegisterDataRefresh"/>, gives the donor genotype
/// precomputation stage, its timers and its dead-letter trigger what they need. With
/// <see cref="ServiceConfiguration.RegisterDonorImportGenotypeSetPrecompute"/>, the queued donor updates of the last stage
/// precompute their donors.
/// </summary>
/// <remarks>
/// Each service is resolved, rather than the whole collection validated on build: resolving builds only its own
/// dependencies, and none of them connects to anything.
/// </remarks>
[TestFixture]
public class DonorGenotypePrecomputationRegistrationTests
{
    /// <summary>The clients are built with these connection strings, but never connect: nothing here is called.</summary>
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

    // Disposed asynchronously: the Service Bus client that the publisher is built on can only be disposed that way.
    [TestCase(typeof(IDonorGenotypePrecomputationStage))]
    [TestCase(typeof(IDonorGenotypePrecomputationSweeper))]
    [TestCase(typeof(IDonorGenotypePrecomputationBatchDispatcher))]
    [TestCase(typeof(IDonorGenotypePrecomputationRunCanceller))]
    public async Task RegisterDataRefresh_ResolvesTheServicesOfThePrecomputationStageAndTimers(Type serviceType)
    {
        await using var provider = DataRefreshServices().BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var service = scope.ServiceProvider.GetRequiredService(serviceType);

        service.Should().BeAssignableTo(serviceType);
    }

    [Test]
    public async Task RegisterDataRefresh_GivesTheSweeperTheSettingsOfTheHost()
    {
        var settings = fixture.Create<DonorGenotypePrecomputationSettings>();
        await using var provider = DataRefreshServices(settings).BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<DonorGenotypePrecomputationSettings>().Should().BeSameAs(settings);
    }

    [Test]
    public void RegisterDataRefresh_Alone_RegistersTheNoOpDonorPrecomputer()
    {
        var services = DataRefreshServices();

        services.Single(d => d.ServiceType == typeof(IDonorGenotypeSetPrecomputer))
            .ImplementationType.Should().Be<NoOpDonorGenotypeSetPrecomputer>();
    }

    [Test]
    public void RegisterDonorImportGenotypeSetPrecompute_AfterRegisterDataRefresh_ReplacesTheNoOpDonorPrecomputer()
    {
        // As the Startup of the Data Refresh app does it: the queued donor updates of the last stage precompute their donors.
        var services = DataRefreshServices();

        services.RegisterDonorImportGenotypeSetPrecompute(
            _ => new ApplicationInsightsSettings { LogLevel = nameof(LogLevel.Info) },
            _ => new HlaMetadataDictionarySettings { AzureStorageConnectionString = DevelopmentStorage },
            _ => new MacDictionarySettings { AzureStorageConnectionString = DevelopmentStorage, TableName = fixture.Create<string>() },
            _ => new GenotypeImputationSettings(),
            _ => SqlConnectionString);

        services.Single(d => d.ServiceType == typeof(IDonorGenotypeSetPrecomputer))
            .ImplementationType.Should().Be<DonorGenotypeSetPrecomputer>();
    }

    private ServiceCollection DataRefreshServices(DonorGenotypePrecomputationSettings precomputationSettings = null)
    {
        var services = new ServiceCollection();
        var settings = precomputationSettings ?? fixture.Create<DonorGenotypePrecomputationSettings>();

        // Registered by the host itself: the notification sender, which the stage uses, reads them as options.
        services.AddSingleton(Options.Create(new NotificationsServiceBusSettings
        {
            ConnectionString = ServiceBusConnectionString,
            AlertsTopic = fixture.Create<string>(),
            NotificationsTopic = fixture.Create<string>()
        }));
        services.AddSingleton(Options.Create(new ApplicationInsightsSettings { LogLevel = nameof(LogLevel.Info) }));

        services.RegisterDataRefresh(
            _ => new AzureAuthenticationSettings(),
            _ => new AzureDatabaseManagementSettings(),
            _ => fixture.Create<DataRefreshSettings>(),
            _ => settings,
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

        return services;
    }
}
