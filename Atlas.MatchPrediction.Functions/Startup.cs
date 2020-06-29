using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MatchPrediction.ExternalInterface.Settings.Azure;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

[assembly: FunctionsStartup(typeof(Atlas.MatchPrediction.Functions.Startup))]

namespace Atlas.MatchPrediction.Functions
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterSettings(builder.Services);
            builder.Services.RegisterMatchPredictionServices(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<AzureStorageSettings>(),
                OptionsReaderFor<HlaMetadataDictionarySettings>(),
                OptionsReaderFor<MacDictionarySettings>(),
                OptionsReaderFor<NotificationsServiceBusSettings>(),
                ConnectionStringReader("MatchPredictionSql")
            );
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            services.RegisterAsOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterAsOptions<AzureStorageSettings>("AzureStorage");
            services.RegisterAsOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
            services.RegisterAsOptions<MacDictionarySettings>("MacDictionary");
            services.RegisterAsOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");
        }
    }
}