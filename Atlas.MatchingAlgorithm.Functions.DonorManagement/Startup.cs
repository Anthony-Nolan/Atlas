using Atlas.HlaMetadataDictionary.ExternalInterface.DependencyInjection;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Functions.DonorManagement;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.MatchingAlgorithm.Functions.DonorManagement
{
    internal static class Startup
    {
        public static void Configure(IServiceCollection services)
        {
            RegisterSettings(services);

            services.AddHealthChecks();

            services.RegisterDonorManagement(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<AzureStorageSettings>(),
                OptionsReaderFor<DonorManagementSettings>(),
                OptionsReaderFor<HlaMetadataDictionarySettings>(),
                OptionsReaderFor<MacDictionarySettings>(),
                OptionsReaderFor<MessagingServiceBusSettings>(),
                OptionsReaderFor<NotificationsServiceBusSettings>(),
                ConnectionStringReader("PersistentSql"),
                ConnectionStringReader("SqlA"),
                ConnectionStringReader("SqlB"),
                ConnectionStringReader("DonorSql")
            );

            // This app holds a cached copy of the dictionary, so it needs to notice when the dictionary is
            // recreated by one of the apps that can.
            services.RegisterHlaMetadataDictionaryCacheInvalidation();
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            services.RegisterAsOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterAsOptions<AzureStorageSettings>("AzureStorage");
            services.RegisterAsOptions<DonorManagementSettings>("MessagingServiceBus:DonorManagement");
            services.RegisterAsOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
            services.RegisterAsOptions<MacDictionarySettings>("MacDictionary");
            services.RegisterAsOptions<MessagingServiceBusSettings>("MessagingServiceBus");
            services.RegisterAsOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");
        }
    }
}