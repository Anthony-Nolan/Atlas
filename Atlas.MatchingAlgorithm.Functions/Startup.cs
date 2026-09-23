using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.HlaMetadataDictionary.ExternalInterface.DependencyInjection;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Atlas.SearchTracking.Common.Settings.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.MatchingAlgorithm.Functions
{
    internal static class Startup
    {
        public static void Configure(IServiceCollection services)
        {
            RegisterSettings(services);

            services.AddHealthChecks();

            services.RegisterSearchForMatchingAlgorithm(OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<AzureStorageSettings>(),
                OptionsReaderFor<HlaMetadataDictionarySettings>(),
                OptionsReaderFor<MacDictionarySettings>(),
                OptionsReaderFor<MessagingServiceBusSettings>(),
                OptionsReaderFor<SearchTrackingServiceBusSettings>(),
                OptionsReaderFor<NotificationsServiceBusSettings>(),
                OptionsReaderFor<MatchingConfigurationSettings>(),
                ConnectionStringReader("PersistentSql"),
                ConnectionStringReader("SqlA"),
                ConnectionStringReader("SqlB"),
                ConnectionStringReader("DonorSql"));

            // After the registration above, so that the real publisher displaces the non-publishing default the HLA
            // Metadata Dictionary registers for apps that only read it. This app can recreate the dictionary, via
            // RefreshHlaMetadataDictionary(ToSpecificVersion), so it has to announce when it has.
            services.RegisterHlaMetadataDictionaryUpdateNotifications(OptionsReaderFor<HlaMetadataDictionaryNotificationSettings>());

            services.RegisterDebugServices(
                OptionsReaderFor<MessagingServiceBusSettings>(),
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<AzureStorageSettings>(),
                OptionsReaderFor<AzureAuthenticationSettings>());
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            services.RegisterAsOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterAsOptions<AzureAuthenticationSettings>("AzureManagement:Authentication");
            services.RegisterAsOptions<AzureMonitoringSettings>("AzureManagement:Monitoring");
            services.RegisterAsOptions<AzureStorageSettings>("AzureStorage");
            services.RegisterAsOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
            services.RegisterAsOptions<HlaMetadataDictionaryNotificationSettings>("HlaMetadataDictionaryNotifications");
            services.RegisterAsOptions<MacDictionarySettings>("MacDictionary");
            services.RegisterAsOptions<MatchingConfigurationSettings>("MatchingConfiguration");
            services.RegisterAsOptions<MessagingServiceBusSettings>("MessagingServiceBus");
            services.RegisterAsOptions<SearchTrackingServiceBusSettings>("SearchTrackingServiceBus");
            services.RegisterAsOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");
        }
    }
}