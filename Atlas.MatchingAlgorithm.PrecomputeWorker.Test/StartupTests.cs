using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.PrecomputeWorker.Test;

/// <summary>
/// The registration of the worker host. The services of a batch are validated, not resolved: resolving the batch processor
/// builds the HLA Metadata Dictionary, which connects to storage. The services that the host starts are resolved.
/// </summary>
[TestFixture]
internal class StartupTests
{
    private static readonly ServiceProviderOptions ValidatingOptions = new() { ValidateOnBuild = true, ValidateScopes = true };

    /// <summary>Nothing listens on port 1, so a connection fails at once.</summary>
    private const string UnreachableSqlConnectionString =
        "Server=tcp:127.0.0.1,1;Database=Atlas;User ID=test;Password=test;Connect Timeout=1;TrustServerCertificate=True;";

    /// <summary>A connection string in a valid format. The client connects only when it receives, and no test starts it.</summary>
    private const string ServiceBusConnectionString = "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test";

    private Fixture fixture = null!;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
    }

    [Test]
    public void Configure_AllowsServiceProviderValidationOnBuild()
    {
        var services = WorkerServices();

        var buildProvider = () => services.BuildServiceProvider(ValidatingOptions);

        buildProvider.Should().NotThrow();
    }

    [TestCase(typeof(IDonorGenotypePrecomputationBatchProcessor))]
    [TestCase(typeof(ISubjectGenotypeSetValueService))]
    [TestCase(typeof(IDonorGenotypePrecomputationMetrics))]
    [TestCase(typeof(DonorGenotypePrecomputationMessageHandler))]
    public void Configure_RegistersTheServicesOfABatch(Type serviceType)
    {
        WorkerServices().Should().Contain(descriptor => descriptor.ServiceType == serviceType);
    }

    [Test]
    public void Configure_RegistersTheWorkerAsAHostedService()
    {
        WorkerServices().Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IHostedService) && descriptor.ImplementationType == typeof(DonorGenotypePrecomputationWorker));
    }

    [Test]
    public async Task Configure_WithNoReachableDatabase_CreatesTheHostedServices_AndReportsHealthy()
    {
        // As outside a data refresh, or before a release has migrated the databases. The host creates its hosted services
        // and serves its health checks when it starts, so none of them can read a database.
        await using var provider = WorkerServices(SettingsWithNoReachableDatabase()).BuildServiceProvider(ValidatingOptions);
        var healthChecks = provider.GetRequiredService<HealthCheckService>();

        var hostedServices = provider.GetServices<IHostedService>().ToList();
        var live = await healthChecks.CheckHealthAsync(check => check.Tags.Contains("live"));
        var ready = await healthChecks.CheckHealthAsync(check => check.Tags.Contains("ready"));

        hostedServices.Should().Contain(service => service is DonorGenotypePrecomputationWorker);
        live.Status.Should().Be(HealthStatus.Healthy);
        ready.Status.Should().Be(HealthStatus.Healthy);
    }

    /// <summary>
    /// Every database is unreachable. The other settings are only the ones that the hosted services need to be created.
    /// </summary>
    private Dictionary<string, string?> SettingsWithNoReachableDatabase() => new()
    {
        ["ConnectionStrings:PersistentSql"] = UnreachableSqlConnectionString,
        ["ConnectionStrings:SqlA"] = UnreachableSqlConnectionString,
        ["ConnectionStrings:SqlB"] = UnreachableSqlConnectionString,
        ["ConnectionStrings:MatchPredictionSql"] = UnreachableSqlConnectionString,
        ["MessagingServiceBus:ConnectionString"] = ServiceBusConnectionString,
        ["PrecomputeWorker:RequestsTopic"] = fixture.Create<string>(),
        ["PrecomputeWorker:RequestsSubscription"] = fixture.Create<string>()
    };

    private static ServiceCollection WorkerServices(Dictionary<string, string?>? settings = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        Startup.Configure(services, configuration);

        return services;
    }
}
