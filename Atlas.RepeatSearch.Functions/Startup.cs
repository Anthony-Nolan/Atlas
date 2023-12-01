using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Atlas.RepeatSearch.ExternalInterface.DependencyInjection;
using Atlas.RepeatSearch.Functions;
using Atlas.RepeatSearch.Settings.ServiceBus;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.RepeatSearch.Functions
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterSettings(builder.Services);
            builder.Services.RegisterRepeatSearch(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<RepeatSearch.Settings.Azure.AzureStorageSettings>(),
                OptionsReaderFor<HlaMetadataDictionarySettings>(),
                OptionsReaderFor<MacDictionarySettings>(),
                OptionsReaderFor<MatchingConfigurationSettings>(),
                OptionsReaderFor<MessagingServiceBusSettings>(),
                OptionsReaderFor<NotificationsServiceBusSettings>(),
                ConnectionStringReader("RepeatSearchSql"),
                ConnectionStringReader("MatchingPersistentSql"),
                ConnectionStringReader("MatchingSqlA"), 
                ConnectionStringReader("MatchingSqlB"),
                ConnectionStringReader("DonorSql")
                );
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
            services.RegisterAsOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");
        }
    }
}