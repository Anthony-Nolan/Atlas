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

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.MatchPrediction.Functions
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterSettings(builder.Services);
            builder.Services.RegisterMatchPredictionAlgorithm(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<HlaMetadataDictionarySettings>(),
                OptionsReaderFor<MacDictionarySettings>(),
                OptionsReaderFor<NotificationsServiceBusSettings>(),
                // TODO: ATLAS-600: Read HLA nomenclature version from file data, rather than hard coding
                _ => new MatchPredictionImportSettings {HlaNomenclatureVersion = "3410"},
                ConnectionStringReader("MatchPredictionSql")
            );
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            services.RegisterAsOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterAsOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
            services.RegisterAsOptions<MacDictionarySettings>("MacDictionary");
            services.RegisterAsOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");
        }
    }
}