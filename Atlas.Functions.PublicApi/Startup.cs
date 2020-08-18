using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Functions.PublicApi;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.Functions.PublicApi
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterSettings(builder.Services);
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            // Atlas Function settings
            // services.RegisterAsOptions<Settings.AzureStorageSettings>("AtlasFunction:AzureStorage");
            // services.RegisterAsOptions<Settings.MessagingServiceBusSettings>("AtlasFunction:MessagingServiceBus");
            // services.RegisterAsOptions<Settings.OrchestrationSettings>("AtlasFunction:Orchestration");

            // Shared settings
            services.RegisterAsOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterAsOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");

            // Dictionary components
            // services.RegisterAsOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
            // services.RegisterAsOptions<MacDictionarySettings>("MacDictionary");
            // services.RegisterAsOptions<MacImportSettings>("MacDictionary:Import");

            // Matching Algorithm
            // services.RegisterAsOptions<AzureStorageSettings>("Matching:AzureStorage");
            // services.RegisterAsOptions<MessagingServiceBusSettings>("Matching:MessagingServiceBus");
        }
    }
}