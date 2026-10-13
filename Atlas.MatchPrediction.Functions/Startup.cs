using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface.DependencyInjection;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.MatchPrediction.Functions
{
    internal static class Startup
    {
        public static void Configure(IServiceCollection services)
        {
            RegisterSettings(services);

            services.AddHealthChecks();

            services.RegisterMatchPredictionAlgorithm(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<HlaMetadataDictionarySettings>(),
                OptionsReaderFor<MacDictionarySettings>(),
                OptionsReaderFor<NotificationsServiceBusSettings>(),
                OptionsReaderFor<AzureStorageSettings>(),
                OptionsReaderFor<GenotypeImputationSettings>(),
                // This app has no access to the transient matching databases, so it keeps the no-op reader and writer
                // and every donor is computed live. The kill-switch is left at its default (off).
                _ => new PrecomputedGenotypeSetSettings(),
                ConnectionStringReader("MatchPredictionSql")
            );

            services.RegisterMatchPredictionRequester(
                OptionsReaderFor<MessagingServiceBusSettings>(),
                OptionsReaderFor<MatchPredictionRequestsSettings>());

            // This app caches HLA Metadata Dictionary data, so it has to be told when the dictionary is recreated.
            services.RegisterHlaMetadataDictionaryCacheInvalidation();
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            services.RegisterAsOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterAsOptions<AzureStorageSettings>("AzureStorage");
            services.RegisterAsOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
            services.RegisterAsOptions<HaplotypeFrequencySetCacheSettings>("HaplotypeFrequencySetCache");
            services.RegisterAsOptions<GenotypeImputationSettings>("GenotypeImputation");
            services.RegisterAsOptions<MacDictionarySettings>("MacDictionary");
            services.RegisterAsOptions<MatchPredictionRequestsSettings>("MatchPredictionRequests");
            services.RegisterAsOptions<MessagingServiceBusSettings>("MessagingServiceBus");
            services.RegisterAsOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");
        }
    }
}