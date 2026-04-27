using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Common.ServiceBus;
using Atlas.Common.ServiceBus.BatchReceiving;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Worker.Settings;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.MatchPrediction.Worker;

public static class Startup
{
    public static void Configure(IServiceCollection services)
    {
        RegisterSettings(services);

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

        services.AddSingleton<IServiceBusMessageReceiver<IdentifiedMatchPredictionRequest>>(sp =>
            {
                var requestSettings = sp.GetRequiredService<IOptions<MatchPredictionRequestsSettings>>().Value;
                var workerSettings = sp.GetRequiredService<IOptions<MatchPredictionWorkerSettings>>().Value;
                var factory = sp.GetRequiredKeyedService<IMessageReceiverFactory>(typeof(MessagingServiceBusSettings));
                return new ServiceBusMessageReceiver<IdentifiedMatchPredictionRequest>(
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

    private static void RegisterSettings(IServiceCollection services)
    {
        services.RegisterAsOptions<ApplicationInsightsSettings>("ApplicationInsights");
        services.RegisterAsOptions<AzureStorageSettings>("AzureStorage");
        services.RegisterAsOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
        services.RegisterAsOptions<MacDictionarySettings>("MacDictionary");

        services.RegisterAsOptions<MatchPredictionRequestsSettings>("MatchPredictionRequests");
        services.RegisterAsOptions<MessagingServiceBusSettings>("MessagingServiceBus");
        services.RegisterAsOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");

        services.RegisterAsOptions<MatchPredictionWorkerSettings>("MatchPredictionWorker");
    }
}