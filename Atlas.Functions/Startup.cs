using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Functions;
using Atlas.Functions.Services;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
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
            RegisterTopLevelFunctionServices(builder.Services);

            // TODO: ATLAS-472: Allow registration of matching with no data refresh functionality
            builder.Services.RegisterMatchingAlgorithm(
                _ => new AzureAuthenticationSettings(),
                _ => new AzureDatabaseManagementSettings(),
                _ => new DataRefreshSettings(),
                _ => new DonorManagementSettings(),
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<AzureStorageSettings>(),
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

            builder.Services.RegisterMacImport(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<MacDictionarySettings>(),
                OptionsReaderFor<MacImportSettings>()
            );

            builder.Services.RegisterMatchPredictionServices(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<HlaMetadataDictionarySettings>(),
                OptionsReaderFor<MacDictionarySettings>(),
                OptionsReaderFor<NotificationsServiceBusSettings>(),
                ConnectionStringReader("MatchPrediction:Sql")
            );
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            // Atlas Function settings
            services.RegisterAsOptions<Settings.AzureStorageSettings>("AzureStorage");

            // Shared settings
            services.RegisterAsOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterAsOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");

            // Dictionary components
            services.RegisterAsOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
            services.RegisterAsOptions<MacDictionarySettings>("MacDictionary");
            services.RegisterAsOptions<MacImportSettings>("MacDictionary:Import");

            // Matching Algorithm
            services.RegisterAsOptions<AzureStorageSettings>("Matching:AzureStorage");
            services.RegisterAsOptions<MessagingServiceBusSettings>("Matching:MessagingServiceBus");
        }

        private static void RegisterTopLevelFunctionServices(IServiceCollection services)
        {
            services.RegisterAtlasLogger(OptionsReaderFor<ApplicationInsightsSettings>());
            services.AddScoped<IResultsUploader, ResultsUploader>();
            services.AddScoped<IMatchPredictionInputBuilder, MatchPredictionInputBuilder>();
        }
    }
}