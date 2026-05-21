using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Notifications;
using Atlas.Common.ServiceBus;
using Atlas.Common.ServiceBus.BatchReceiving;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Worker.Services;
using Atlas.MatchPrediction.Worker.Settings;
using Atlas.MultipleAlleleCodeDictionary.Settings;
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
        services.AddScoped<IParallelMatchPredictionBatchRunner, ParallelMatchPredictionBatchRunner>();

        services.AddSingleton<IServiceBusMessageReceiver<ParallelMatchPredictionBatchRequest>>(sp =>
            {
                var requestSettings = sp.GetRequiredService<IOptions<MatchPredictionRequestsSettings>>().Value;
                var workerSettings = sp.GetRequiredService<IOptions<MatchPredictionWorkerSettings>>().Value;
                var factory = sp.GetRequiredKeyedService<IMessageReceiverFactory>(typeof(MessagingServiceBusSettings));
                return new ServiceBusMessageReceiver<ParallelMatchPredictionBatchRequest>(
                    factory,
                    requestSettings.RequestsTopic,
                    workerSettings.RequestsSubscription,
                    workerSettings.BatchSize
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