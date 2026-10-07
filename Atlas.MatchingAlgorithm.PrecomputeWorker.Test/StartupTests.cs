using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.PrecomputeWorker.Test;

/// <summary>
/// The registration of the worker host. The services are validated, not resolved: resolving the batch processor builds the
/// HLA Metadata Dictionary, which connects to storage.
/// </summary>
[TestFixture]
internal class StartupTests
{
    private static readonly ServiceProviderOptions ValidatingOptions = new() { ValidateOnBuild = true, ValidateScopes = true };

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
    public void Configure_RegistersTheTargetAsASingleton()
    {
        // The worker reads its target once, and uses that database until it stops.
        WorkerServices().Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(DonorGenotypePrecomputationTarget))
            .Which.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Test]
    public async Task Configure_ReadsTheTargetFromTheOpenDataRefreshRecord_Once()
    {
        var database = fixture.Create<TransientDatabase>();
        var record = fixture.Build<DataRefreshRecord>().With(r => r.Database, database.ToString()).Create();
        var dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();
        dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns([record]);
        var services = WorkerServices();
        services.AddScoped(_ => dataRefreshHistoryRepository);
        await using var provider = services.BuildServiceProvider(ValidatingOptions);

        var firstTarget = provider.GetRequiredService<DonorGenotypePrecomputationTarget>();
        var secondTarget = provider.GetRequiredService<DonorGenotypePrecomputationTarget>();

        firstTarget.Should().Be(new DonorGenotypePrecomputationTarget(record.Id, database));
        secondTarget.Should().BeSameAs(firstTarget);
        dataRefreshHistoryRepository.Received(1).GetIncompleteRefreshJobs();
    }

    private static ServiceCollection WorkerServices()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        Startup.Configure(services, configuration);

        return services;
    }
}
