using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Functions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.DependencyInjection;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.Functions
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterSettings(builder.Services);

            // TODO: ATLAS-236: Split out data refresh from matching? Maybe not while Mike works on refresh, TechDebt card?
            builder.Services.RegisterMatchingAlgorithm(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                _ => new AzureAuthenticationSettings(),
                _ => new AzureAppServiceManagementSettings(),
                _ => new AzureDatabaseManagementSettings(),
                OptionsReaderFor<AzureStorageSettings>(),
                _ => new DataRefreshSettings(), 
                OptionsReaderFor<HlaMetadataDictionarySettings>(),
                OptionsReaderFor<MacDictionarySettings>(),
                OptionsReaderFor<MessagingServiceBusSettings>(),
                OptionsReaderFor<NotificationsServiceBusSettings>(),
                ConnectionStringReader("Matching:Sql:Persistent"),
                ConnectionStringReader("Matching:Sql:A"),
                ConnectionStringReader("Matching:Sql:B"),
                ConnectionStringReader("DonorImport:Sql")
            );

            builder.Services.RegisterMacDictionary(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<MacDictionarySettings>()
            );

            builder.Services.RegisterMatchPredictionServices(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<Atlas.MatchPrediction.Settings.Azure.AzureStorageSettings>(),
                OptionsReaderFor<HlaMetadataDictionarySettings>(),
                OptionsReaderFor<MacDictionarySettings>(),
                OptionsReaderFor<NotificationsServiceBusSettings>(),
                ConnectionStringReader("MatchPrediction:Sql")
            );
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            // Shared settings
            services.RegisterAsOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterAsOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");

            // Dictionary components
            services.RegisterAsOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
            services.RegisterAsOptions<MacDictionarySettings>("MacDictionary");

            // Matching Algorithm
            services.RegisterAsOptions<AzureStorageSettings>("Matching:AzureStorage");
            services.RegisterAsOptions<MessagingServiceBusSettings>("Matching:MessagingServiceBus");

            // Match Prediction Algorithm
            services.RegisterAsOptions<Atlas.MatchPrediction.Settings.Azure.AzureStorageSettings>("MatchPrediction:AzureStorage");
        }
    }
}