using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Functions;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.MatchPrediction.Functions
{
    internal static class Startup
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
                OptionsReaderFor<MatchPredictionRequestsSettings>());
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
        }
    }
}