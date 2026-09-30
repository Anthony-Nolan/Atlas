using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus.DependencyInjection;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Persistent.Context;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.PrecomputeWorker.Settings;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;
using MessagingServiceBusSettings = Atlas.MatchingAlgorithm.Settings.ServiceBus.MessagingServiceBusSettings;

namespace Atlas.MatchingAlgorithm.PrecomputeWorker;

public static class Startup
{
    public static void Configure(IServiceCollection services, IConfiguration configuration)
    {
        services.AddWorkerValidatedOptions(configuration);

        services.MakeSettingsAvailableForUse(OptionsReaderFor<DonorGenotypePrecomputationWorkerSettings>());

        services.RegisterGenotypeSetPipeline(
            OptionsReaderFor<ApplicationInsightsSettings>(),
            OptionsReaderFor<HlaMetadataDictionarySettings>(),
            OptionsReaderFor<MacDictionarySettings>(),
            OptionsReaderFor<GenotypeImputationSettings>(),
            ConnectionStringReader("MatchPredictionSql"));

        // The logger of the repository factory.
        services.AddScoped<MatchingAlgorithmImportLoggingContext>();
        services.AddScoped<IMatchingAlgorithmImportLogger, MatchingAlgorithmImportLogger>();

        services.AddScoped(sp => new ConnectionStrings
        {
            TransientA = ConnectionStringReader("SqlA")(sp),
            TransientB = ConnectionStringReader("SqlB")(sp)
        });
        services.AddScoped<StaticallyChosenTransientSqlConnectionStringProviderFactory>();
        services.AddScoped<IStaticallyChosenDatabaseRepositoryFactory, StaticallyChosenDatabaseRepositoryFactory>();

        // The persistent database, only to read the target of the worker. The target is a singleton, so the worker reads
        // it once. The hosted worker takes the target, so the read happens when the host starts.
        services.AddScoped(sp => new ContextFactory().Create(ConnectionStringReader("PersistentSql")(sp)));
        services.AddScoped<IDataRefreshHistoryRepository, DataRefreshHistoryRepository>();
        services.AddSingleton(sp =>
        {
            using var scope = sp.CreateScope();
            return DonorGenotypePrecomputationTargetReader.Read(scope.ServiceProvider.GetRequiredService<IDataRefreshHistoryRepository>());
        });

        services.AddScoped<ISubjectGenotypeSetValueService, SubjectGenotypeSetValueService>();
        services.AddSingleton<IDonorGenotypePrecomputationMetrics, DonorGenotypePrecomputationMetrics>();
        services.AddScoped<IDonorGenotypePrecomputationBatchProcessor, DonorGenotypePrecomputationBatchProcessor>();

        services.RegisterServiceBusServices(sp => sp.GetRequiredService<IOptions<MessagingServiceBusSettings>>().Value.ConnectionString);

        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<DonorGenotypePrecomputationWorkerSettings>>().Value;

            return sp.GetRequiredService<ServiceBusClient>().CreateProcessor(
                settings.RequestsTopic,
                settings.RequestsSubscription,
                new ServiceBusProcessorOptions
                {
                    // The worker completes, abandons or dead-letters each message itself, after the batch.
                    AutoCompleteMessages = false,
                    MaxConcurrentCalls = settings.MaxConcurrentCalls,
                    PrefetchCount = settings.PrefetchCount,
                    MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(settings.MaxAutoLockRenewalMinutes),
                }
            );
        });

        services.AddSingleton<DonorGenotypePrecomputationMessageHandler>();

        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"]);

        services.AddHostedService<DonorGenotypePrecomputationWorker>();
    }
}
