using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Functions;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.MatchingAlgorithm.Functions
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterSettings(builder.Services);
            builder.Services.RegisterMatchingAlgorithm(
                DependencyInjectionUtils.OptionsReaderFor<ApplicationInsightsSettings>(),
                DependencyInjectionUtils.OptionsReaderFor<AzureAuthenticationSettings>(),
                DependencyInjectionUtils.OptionsReaderFor<AzureAppServiceManagementSettings>(),
                DependencyInjectionUtils.OptionsReaderFor<AzureDatabaseManagementSettings>(),
                DependencyInjectionUtils.OptionsReaderFor<AzureStorageSettings>(),
                DependencyInjectionUtils.OptionsReaderFor<DataRefreshSettings>(),
                DependencyInjectionUtils.OptionsReaderFor<HlaMetadataDictionarySettings>(),
                DependencyInjectionUtils.OptionsReaderFor<MacDictionarySettings>(),
                DependencyInjectionUtils.OptionsReaderFor<MessagingServiceBusSettings>(),
                DependencyInjectionUtils.OptionsReaderFor<NotificationsServiceBusSettings>()
            );
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            services.RegisterOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterOptions<AzureAuthenticationSettings>("AzureManagement:Authentication");
            services.RegisterOptions<AzureAppServiceManagementSettings>("AzureManagement:AppService");
            services.RegisterOptions<AzureDatabaseManagementSettings>("AzureManagement:Database");
            services.RegisterOptions<AzureStorageSettings>("AzureStorage");
            services.RegisterOptions<DataRefreshSettings>("DataRefresh");
            services.RegisterOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
            services.RegisterOptions<MacDictionarySettings>("MacDictionary");
            services.RegisterOptions<MessagingServiceBusSettings>("MessagingServiceBus");
            services.RegisterOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");
        }
    }
}