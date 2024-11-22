using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Atlas.RepeatSearch.ExternalInterface.DependencyInjection;
using Atlas.RepeatSearch.Functions;
using Atlas.RepeatSearch.Settings.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;
using Atlas.SearchTracking.Common.Settings.ServiceBus;


namespace Atlas.RepeatSearch.Functions
{
    internal static class Startup
    {
        public static void Configure(IServiceCollection services)
        {
            RegisterSettings(services);

            services.AddHealthChecks();

            services.RegisterRepeatSearch(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<RepeatSearch.Settings.Azure.AzureStorageSettings>(),
                OptionsReaderFor<HlaMetadataDictionarySettings>(),
                OptionsReaderFor<MacDictionarySettings>(),
                OptionsReaderFor<MatchingConfigurationSettings>(),
                OptionsReaderFor<MessagingServiceBusSettings>(),
                OptionsReaderFor<SearchTrackingServiceBusSettings>(),
                OptionsReaderFor<NotificationsServiceBusSettings>(),
                ConnectionStringReader("RepeatSearchSql"),
                ConnectionStringReader("MatchingPersistentSql"),
                ConnectionStringReader("MatchingSqlA"),
                ConnectionStringReader("MatchingSqlB"),
                ConnectionStringReader("DonorSql")
                );
            services.RegisterDebugServices(
                OptionsReaderFor<MessagingServiceBusSettings>(),
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<RepeatSearch.Settings.Azure.AzureStorageSettings>());
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            services.RegisterAsOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterAsOptions<RepeatSearch.Settings.Azure.AzureStorageSettings>("AzureStorage");
            services.RegisterAsOptions<MatchingAlgorithm.Settings.Azure.AzureStorageSettings>("AzureStorage");
            services.RegisterAsOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
            services.RegisterAsOptions<MacDictionarySettings>("MacDictionary");
            services.RegisterAsOptions<MatchingConfigurationSettings>("MatchingConfiguration");
            services.RegisterAsOptions<MessagingServiceBusSettings>("MessagingServiceBus");
            services.RegisterAsOptions<SearchTrackingServiceBusSettings>("SearchTrackingServiceBus");
            services.RegisterAsOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");
        }
    }
}