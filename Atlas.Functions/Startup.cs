using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Notifications;
using Atlas.DonorImport.ExternalInterface.DependencyInjection;
using Atlas.Functions;
using Atlas.Functions.Config;
using Atlas.Functions.Services;
using Atlas.Functions.Services.BlobStorageClients;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.DependencyInjection;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

            builder.Services.RegisterNotificationSender(
                OptionsReaderFor<NotificationsServiceBusSettings>(),
                OptionsReaderFor<ApplicationInsightsSettings>()
            );

            builder.Services.RegisterDonorReader(ConnectionStringReader("DonorImport:Sql"));

            builder.Services.RegisterMacDictionary(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<MacDictionarySettings>()
            );

            builder.Services.RegisterMacImport(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<MacDictionarySettings>(),
                OptionsReaderFor<MacDownloadSettings>()
            );

            builder.Services.RegisterMatchPredictionAlgorithm(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<HlaMetadataDictionarySettings>(),
                OptionsReaderFor<MacDictionarySettings>(),
                OptionsReaderFor<MatchPrediction.ExternalInterface.Settings.MatchPredictionAlgorithmSettings>(),
                OptionsReaderFor<NotificationsServiceBusSettings>(),
                OptionsReaderFor<MatchPrediction.ExternalInterface.Settings.AzureStorageSettings>(),
                ConnectionStringReader("MatchPrediction:Sql")
            );
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            // Atlas Function settings
            services.RegisterAsOptions<Settings.AzureStorageSettings>("AtlasFunction:AzureStorage");
            services.RegisterAsOptions<Settings.MessagingServiceBusSettings>("AtlasFunction:MessagingServiceBus");
            services.RegisterAsOptions<Settings.OrchestrationSettings>("AtlasFunction:Orchestration");

            // Shared settings
            services.RegisterAsOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterAsOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");

            // Dictionary components
            services.RegisterAsOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
            services.RegisterAsOptions<MacDictionarySettings>("MacDictionary");
            services.RegisterAsOptions<MacDownloadSettings>("MacDictionary:Download");

            // Matching Algorithm
            services.RegisterAsOptions<AzureStorageSettings>("Matching:AzureStorage");
            services.RegisterAsOptions<MessagingServiceBusSettings>("Matching:MessagingServiceBus");

            // Match Prediction Algorithm
            services.RegisterAsOptions<MatchPrediction.ExternalInterface.Settings.AzureStorageSettings>("MatchPrediction:AzureStorage");
            services.RegisterAsOptions<MatchPrediction.ExternalInterface.Settings.MatchPredictionAlgorithmSettings>("MatchPrediction:Algorithm");
        }

        private static void RegisterTopLevelFunctionServices(IServiceCollection services)
        {
            services.RegisterAtlasLogger(OptionsReaderFor<ApplicationInsightsSettings>());
            services.AddScoped<IMatchPredictionInputBuilder, MatchPredictionInputBuilder>();
            services.AddScoped<IResultsCombiner, ResultsCombiner>();
            services.AddScoped<ISearchCompletionMessageSender, SearchCompletionMessageSender>();
            services.AddScoped<ISearchResultsBlobStorageClient, SearchResultsBlobStorageClient>(sp =>
            {
                var settings = sp.GetService<IOptions<Settings.AzureStorageSettings>>().Value;
                var logger = sp.GetService<ILogger>();
                return new SearchResultsBlobStorageClient(settings.MatchingConnectionString, settings.SearchResultsBatchSize, logger);
            });
            services.AddSingleton<IMatchingResultsDownloader, MatchingResultsDownloader>(sp =>
            {
                var logger = sp.GetService<ILogger>();
                var options = sp.GetService<IOptions<Settings.AzureStorageSettings>>();
                var connectionString = options.Value.MatchingConnectionString;
                var downloader = new BlobDownloader(connectionString, logger);
                return new MatchingResultsDownloader(options, downloader, logger);
            });
            services.AddSingleton<IMatchPredictionResultsDownloader, MatchPredictionResultsDownloader>(sp =>
            {
                var logger = sp.GetService<ILogger>();
                var options = sp.GetService<IOptions<Settings.AzureStorageSettings>>();
                var connectionString = options.Value.MatchPredictionConnectionString;
                var downloader = new BlobDownloader(connectionString, logger);
                return new MatchPredictionResultsDownloader(options, downloader);
            });
            services.AddScoped<IMatchPredictionRequestBlobClient, MatchPredictionRequestBlobClient>();

            services.AddSingleton(sp => AutoMapperConfig.CreateMapper());
        }
    }
}