using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Worker.Services;
using Atlas.MatchPrediction.Worker.Settings;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Atlas.SearchTracking.Common.DependencyInjection;
using Atlas.SearchTracking.Common.Dispatchers;
using Atlas.SearchTracking.Common.Settings.ServiceBus;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.MatchPrediction.Worker;

public static class Startup
{
    public static void Configure(IServiceCollection services, IConfiguration configuration)
    {
        RegisterSettings(services, configuration);

        services.RegisterMatchPredictionAlgorithm(
            OptionsReaderFor<ApplicationInsightsSettings>(),
            OptionsReaderFor<HlaMetadataDictionarySettings>(),
            OptionsReaderFor<MacDictionarySettings>(),
            OptionsReaderFor<NotificationsServiceBusSettings>(),
            OptionsReaderFor<AzureStorageSettings>(),
            ConnectionStringReader("MatchPredictionSql")
        );

        services.RegisterMatchPredictionRequester(
            OptionsReaderFor<MessagingServiceBusSettings>(),
            OptionsReaderFor<MatchPredictionRequestsSettings>()
        );

        services.RegisterParallelMatchPredictionBatchResultPublisher(
            OptionsReaderFor<MessagingServiceBusSettings>(),
            OptionsReaderFor<MatchPredictionRequestsSettings>()
        );

        services.AddScoped<IBlobDownloader>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<AzureStorageSettings>>().Value;
            var atlasLogger = sp.GetRequiredService<IAtlasLogger>();
            return new BlobDownloader(settings.MatchPredictionConnectionString, atlasLogger);
        });

        // Make the settings available as a plain singleton so the shared registration (and the client's constructor) can resolve it.
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<SearchTrackingServiceBusSettings>>().Value);
        services.RegisterSearchTrackingServiceBusClient();

        services.AddScoped<IMatchPredictionSearchTrackingDispatcher, MatchPredictionSearchTrackingDispatcher>();

        services.AddScoped<IParallelMatchPredictionBatchRunner, ParallelMatchPredictionBatchRunner>();

        services.AddSingleton(sp =>
            {
                var requestSettings = sp.GetRequiredService<IOptions<MatchPredictionRequestsSettings>>().Value;
                var workerSettings = sp.GetRequiredService<IOptions<MatchPredictionWorkerSettings>>().Value;
                var client = sp.GetRequiredKeyedService<ServiceBusClient>(typeof(MessagingServiceBusSettings));

                return client.CreateProcessor(
                    requestSettings.RequestsTopic,
                    workerSettings.RequestsSubscription,
                    new ServiceBusProcessorOptions
                    {
                        // We complete/abandon explicitly once the batch has been processed.
                        AutoCompleteMessages = false,
                        MaxConcurrentCalls = workerSettings.MaxConcurrentCalls,
                        PrefetchCount = workerSettings.PrefetchCount,
                        MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(workerSettings.MaxAutoLockRenewalMinutes),
                    }
                );
            }
        );

        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"]);

        services.AddHostedService<MatchPredictionWorker>();
    }

    private static void RegisterSettings(IServiceCollection services, IConfiguration configuration)
    {
        services.AddWorkerValidatedOptions(configuration);
    }
}