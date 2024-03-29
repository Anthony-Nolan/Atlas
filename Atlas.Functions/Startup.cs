using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.SupportMessages;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Debugging;
using Atlas.Common.Notifications;
using Atlas.Common.ServiceBus;
using Atlas.Functions;
using Atlas.Functions.Config;
using Atlas.Functions.Services;
using Atlas.Functions.Services.BlobStorageClients;
using Atlas.Functions.Services.Debug;
using Atlas.Functions.Services.MatchCategories;
using Atlas.Functions.Settings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.DependencyInjection;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;
using AzureStorageSettings = Atlas.MatchingAlgorithm.Settings.Azure.AzureStorageSettings;
using MessagingServiceBusSettings = Atlas.MatchingAlgorithm.Settings.ServiceBus.MessagingServiceBusSettings;

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
            services.RegisterAsOptions<NotificationsDebugSettings>("NotificationsServiceBus:Debug");

            // Dictionary components
            services.RegisterAsOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
            services.RegisterAsOptions<MacDictionarySettings>("MacDictionary");
            services.RegisterAsOptions<MacDownloadSettings>("MacDictionary:Download");

            // Matching Algorithm
            services.RegisterAsOptions<AzureStorageSettings>("Matching:AzureStorage");
            services.RegisterAsOptions<MessagingServiceBusSettings>("Matching:MessagingServiceBus");

            // Match Prediction Algorithm
            services.RegisterAsOptions<MatchPrediction.ExternalInterface.Settings.AzureStorageSettings>("MatchPrediction:AzureStorage");
        }

        private static void RegisterTopLevelFunctionServices(IServiceCollection services)
        {
            RegisterSearchLogger(services);

            RegisterDebugServices(services);

            services.AddScoped<IMatchPredictionInputBuilder, MatchPredictionInputBuilder>();
            services.AddScoped<IResultsCombiner, ResultsCombiner>(sp =>
            {
                var logger = sp.GetService<ISearchLogger<SearchLoggingContext>>();
                var options = sp.GetService<IOptions<Settings.AzureStorageSettings>>();
                var connectionString = options.Value.MatchPredictionConnectionString;
                var downloader = new BlobDownloader(connectionString, logger);
                var matchCategoryService = new PositionalMatchCategoryService(new PositionalMatchCategoryOrientator());
                return new ResultsCombiner(options, logger, downloader, matchCategoryService);
            });
            services.AddScoped<ISearchCompletionMessageSender, SearchCompletionMessageSender>();
            services.AddScoped<ISearchResultsBlobStorageClient, SearchResultsBlobStorageClient>(sp =>
            {
                var settings = sp.GetService<IOptions<Settings.AzureStorageSettings>>().Value;
                var logger = sp.GetService<ILogger>();
                return new SearchResultsBlobStorageClient(settings.MatchingConnectionString, logger);
            });

            services.AddScoped<IMatchingResultsDownloader, MatchingResultsDownloader>(sp =>
            {
                var logger = sp.GetService<ISearchLogger<SearchLoggingContext>>();
                var options = sp.GetService<IOptions<Settings.AzureStorageSettings>>();
                var connectionString = options.Value.MatchingConnectionString;
                var downloader = new BlobDownloader(connectionString, logger);
                return new MatchingResultsDownloader(options, downloader, logger);
            });
            services.AddScoped<IMatchPredictionRequestBlobClient, MatchPredictionRequestBlobClient>();

            services.AddSingleton(sp => AutoMapperConfig.CreateMapper());
        }

        private static void RegisterSearchLogger(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetryWorkerService();
            services.AddScoped<SearchLoggingContext>();
            services.AddScoped(typeof(ISearchLogger<>), typeof(SearchLogger<>));
        }

        private static void RegisterDebugServices(IServiceCollection services)
        {
            services.AddSingleton<IMessageReceiverFactory, MessageReceiverFactory>();

            services.AddScoped<IServiceBusPeeker<Alert>, AlertsPeeker>(sp =>
            {
                var notificationsOptions = sp.GetService<IOptions<NotificationsServiceBusSettings>>();
                var debugOptions = sp.GetService<IOptions<NotificationsDebugSettings>>();
                return new AlertsPeeker(
                    sp.GetService<IMessageReceiverFactory>(),
                    notificationsOptions.Value.ConnectionString,
                    notificationsOptions.Value.AlertsTopic,
                    debugOptions.Value.AlertsSubscription);
            });

            services.AddScoped<IServiceBusPeeker<Notification>, NotificationsPeeker>(sp =>
            {
                var notificationsOptions = sp.GetService<IOptions<NotificationsServiceBusSettings>>();
                var debugOptions = sp.GetService<IOptions<NotificationsDebugSettings>>();
                return new NotificationsPeeker(
                    sp.GetService<IMessageReceiverFactory>(),
                    notificationsOptions.Value.ConnectionString,
                    notificationsOptions.Value.NotificationsTopic,
                    debugOptions.Value.NotificationsSubscription);
            });

            services.AddScoped<ISearchResultNotificationsPeeker, SearchResultNotificationsPeeker>(sp =>
            {
                var messagingOptions = sp.GetService<IOptions<Settings.MessagingServiceBusSettings>>();
                return new SearchResultNotificationsPeeker(
                    sp.GetService<IMessageReceiverFactory>(),
                    messagingOptions.Value.ConnectionString,
                    messagingOptions.Value.SearchResultsTopic,
                    messagingOptions.Value.SearchResultsDebugSubscription);
            });
            
            services.AddScoped<IRepeatSearchResultNotificationsPeeker, RepeatSearchResultNotificationsPeeker>(sp =>
            {
                var messagingOptions = sp.GetService<IOptions<Settings.MessagingServiceBusSettings>>();
                return new RepeatSearchResultNotificationsPeeker(
                    sp.GetService<IMessageReceiverFactory>(),
                    messagingOptions.Value.ConnectionString,
                    messagingOptions.Value.RepeatSearchResultsTopic,
                    messagingOptions.Value.RepeatSearchResultsDebugSubscription);
            });

            services.RegisterDebugLogger(OptionsReaderFor<ApplicationInsightsSettings>());
            services.AddScoped<IDebugResultsDownloader, DebugResultsDownloader>(sp =>
            {
                var settings = sp.GetService<IOptions<Settings.AzureStorageSettings>>();
                var downloader = new BlobDownloader(settings.Value.MatchingConnectionString, sp.GetService<IDebugLogger>());
                return new DebugResultsDownloader(downloader);
            });
        }
    }
}