using Atlas.Client.Models.SupportMessages;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Debugging;
using Atlas.Common.Notifications;
using Atlas.Common.ServiceBus;
using Atlas.Common.ServiceBus.DependencyInjection;
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
using Atlas.SearchTracking.Common.Clients;
using Atlas.SearchTracking.Common.Settings.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;
using AzureStorageSettings = Atlas.MatchingAlgorithm.Settings.Azure.AzureStorageSettings;
using MessagingServiceBusSettings = Atlas.MatchingAlgorithm.Settings.ServiceBus.MessagingServiceBusSettings;

namespace Atlas.Functions
{
    internal static class Startup
    {
        public static void Configure(IServiceCollection services)
        {
            RegisterSettings(services);

            services.AddHealthChecks();

            RegisterTopLevelFunctionServices(services);

            services.RegisterServiceBusAsKeyedServices(
                typeof(Settings.MessagingServiceBusSettings),
                sp => sp.GetRequiredService<IOptions<Settings.MessagingServiceBusSettings>>().Value.ConnectionString);

            services.RegisterNotificationSender(
                OptionsReaderFor<NotificationsServiceBusSettings>(),
                OptionsReaderFor<ApplicationInsightsSettings>()
            );

            services.RegisterMacDictionary(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<MacDictionarySettings>()
            );

            services.RegisterMacImport(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<MacDictionarySettings>(),
                OptionsReaderFor<MacDownloadSettings>()
            );

            services.RegisterMatchPredictionAlgorithm(
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
            services.RegisterAsOptions<SearchTrackingServiceBusSettings>("SearchTrackingServiceBus");

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

            RegisterMatchPredictionSearchTracking(services, OptionsReaderFor<SearchTrackingServiceBusSettings>());

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

        private static void RegisterMatchPredictionSearchTracking(
            this IServiceCollection services,
            Func<IServiceProvider, SearchTrackingServiceBusSettings> fetchSearchTrackingServiceBusSettings)
        {
            services.MakeSettingsAvailableForUse(fetchSearchTrackingServiceBusSettings);

            services.AddScoped<IMatchPredictionSearchTrackingDispatcher, MatchPredictionSearchTrackingDispatcher>();
            services.AddScoped<ISearchTrackingServiceBusClient, SearchTrackingServiceBusClient>();
        }

        private static void RegisterDebugServices(IServiceCollection services)
        {
            var serviceKey = typeof(NotificationsServiceBusSettings);
            services.RegisterServiceBusAsKeyedServices(
                serviceKey,
                sp => sp.GetRequiredService<IOptions<NotificationsServiceBusSettings>>().Value.ConnectionString
                );

            services.AddScoped<IServiceBusPeeker<Alert>, AlertsPeeker>(sp =>
            {
                var notificationsOptions = sp.GetService<IOptions<NotificationsServiceBusSettings>>();
                var debugOptions = sp.GetService<IOptions<NotificationsDebugSettings>>();
                return new AlertsPeeker(
                    sp.GetRequiredKeyedService<IMessageReceiverFactory>(serviceKey),
                    notificationsOptions.Value.AlertsTopic,
                    debugOptions.Value.AlertsSubscription);
            });

            services.AddScoped<IServiceBusPeeker<Notification>, NotificationsPeeker>(sp =>
            {
                var notificationsOptions = sp.GetService<IOptions<NotificationsServiceBusSettings>>();
                var debugOptions = sp.GetService<IOptions<NotificationsDebugSettings>>();
                return new NotificationsPeeker(
                    sp.GetRequiredKeyedService<IMessageReceiverFactory>(serviceKey),
                    notificationsOptions.Value.NotificationsTopic,
                    debugOptions.Value.NotificationsSubscription);
            });

            services.AddScoped<ISearchResultNotificationsPeeker, SearchResultNotificationsPeeker>(sp =>
            {
                var messagingOptions = sp.GetService<IOptions<Settings.MessagingServiceBusSettings>>();
                return new SearchResultNotificationsPeeker(
                    sp.GetRequiredKeyedService<IMessageReceiverFactory>(serviceKey),
                    messagingOptions.Value.SearchResultsTopic,
                    messagingOptions.Value.SearchResultsDebugSubscription);
            });

            services.AddScoped<IRepeatSearchResultNotificationsPeeker, RepeatSearchResultNotificationsPeeker>(sp =>
            {
                var messagingOptions = sp.GetService<IOptions<Settings.MessagingServiceBusSettings>>();
                return new RepeatSearchResultNotificationsPeeker(
                    sp.GetRequiredKeyedService<IMessageReceiverFactory>(serviceKey),
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