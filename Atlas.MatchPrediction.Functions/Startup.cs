using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchPrediction.DependencyInjection;
using Atlas.MatchPrediction.Settings.Azure;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Atlas.MatchPrediction.Functions.Startup))]

namespace Atlas.MatchPrediction.Functions
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterSettings(builder.Services);
            builder.Services.RegisterMatchPredictionServices(
                DependencyInjectionUtils.OptionsReaderFor<ApplicationInsightsSettings>(),
                DependencyInjectionUtils.OptionsReaderFor<AzureStorageSettings>(),
                DependencyInjectionUtils.OptionsReaderFor<HlaMetadataDictionarySettings>(),
                DependencyInjectionUtils.OptionsReaderFor<MacDictionarySettings>(),
                DependencyInjectionUtils.OptionsReaderFor<NotificationsServiceBusSettings>()
            );
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            services.RegisterOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterOptions<AzureStorageSettings>("AzureStorage");
            services.RegisterOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
            services.RegisterOptions<MacDictionarySettings>("MacDictionary");
            services.RegisterOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");
        }
    }
}