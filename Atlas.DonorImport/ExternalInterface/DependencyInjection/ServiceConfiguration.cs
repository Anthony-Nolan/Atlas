using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Notifications;
using Atlas.Common.ServiceBus;
using Atlas.Common.ServiceBus.DependencyInjection;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Data;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.ExternalInterface.Settings;
using Atlas.DonorImport.ExternalInterface.Settings.ServiceBus;
using Atlas.DonorImport.Logger;
using Atlas.DonorImport.Models.Mapping;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Services.Debug;
using Atlas.DonorImport.Services.DonorChecker;
using Atlas.DonorImport.Services.DonorUpdates;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.DonorImport.ExternalInterface.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterDonorImport(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, NotificationConfigurationSettings> fetchNotificationConfigurationSettings,
            Func<IServiceProvider, NotificationsServiceBusSettings> fetchNotificationsServiceBusSettings,
            Func<IServiceProvider, DonorImportSettings> fetchStalledFileSettings,
            Func<IServiceProvider, PublishDonorUpdatesSettings> fetchPublishDonorUpdatesSettings,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings,
            Func<IServiceProvider, FailureLogsSettings> fetchFailureLogsSettings,
            Func<IServiceProvider, string> fetchSqlConnectionString)
        {
            // Perform static Dapper set up that should be performed once before any SQL requests are made.
            Initialise.InitaliseDapper();

            RegisterDonorImportLogger(services);

            services.RegisterSettings(
                fetchNotificationConfigurationSettings, fetchStalledFileSettings, fetchPublishDonorUpdatesSettings, fetchAzureStorageSettings, fetchMessagingServiceBusSettings, fetchFailureLogsSettings);
            services.RegisterClients(fetchApplicationInsightsSettings, fetchNotificationsServiceBusSettings);
            services.RegisterServices(fetchMessagingServiceBusSettings, fetchAzureStorageSettings);
            services.RegisterDebugServices(fetchMessagingServiceBusSettings, fetchAzureStorageSettings);
            services.RegisterImportDatabaseTypes(fetchSqlConnectionString);
        }

        public static void RegisterImportDatabaseTypes(
        this IServiceCollection services,
        Func<IServiceProvider, string> fetchDonorImportDatabaseConnectionString)
        {
            services.AddScoped<IDonorImportRepository>(sp => new DonorImportRepository(fetchDonorImportDatabaseConnectionString(sp)));
            services.AddScoped<IDonorReadRepository>(sp => new DonorReadRepository(fetchDonorImportDatabaseConnectionString(sp)));
            services.AddScoped<IDonorImportHistoryRepository>(sp => new DonorImportHistoryRepository(fetchDonorImportDatabaseConnectionString(sp)));
            services.AddScoped<IDonorImportLogRepository>(sp => new DonorImportLogRepository(fetchDonorImportDatabaseConnectionString(sp)));
            services.AddScoped<IPublishableDonorUpdatesRepository>(sp => new PublishableDonorUpdatesRepository(fetchDonorImportDatabaseConnectionString(sp)));
            services.AddScoped<IDonorImportFailureRepository>(sp => new DonorImportFailureRepository(fetchDonorImportDatabaseConnectionString(sp)));
        }

        public static void RegisterDonorUpdateServices(
             this IServiceCollection services,
             Func<IServiceProvider, string> fetchDonorImportDatabaseConnectionString)
        {
            services.AddScoped<IDonorUpdatesSaver, DonorUpdatesSaver>();
            services.AddScoped<IPublishableDonorUpdatesRepository>(sp => new PublishableDonorUpdatesRepository(fetchDonorImportDatabaseConnectionString(sp)));
        }

        public static void RegisterDonorReader(
            this IServiceCollection services,
            Func<IServiceProvider, string> fetchDonorImportDatabaseConnectionString)
        {
            services.RegisterDonorReaderServices();

            services.AddScoped<IDonorReadRepository>(sp =>
                new DonorReadRepository(fetchDonorImportDatabaseConnectionString(sp))
            );
        }

        private static void RegisterSettings(
            this IServiceCollection services,
            Func<IServiceProvider, NotificationConfigurationSettings> fetchNotificationConfigurationSettings,
            Func<IServiceProvider, DonorImportSettings> fetchStalledFileSettings,
            Func<IServiceProvider, PublishDonorUpdatesSettings> fetchPublishDonorUpdatesSettings,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, FailureLogsSettings> fetchFailureLogsSettings)
        {
            services.MakeSettingsAvailableForUse(fetchStalledFileSettings);
            services.MakeSettingsAvailableForUse(fetchNotificationConfigurationSettings);
            services.MakeSettingsAvailableForUse(fetchPublishDonorUpdatesSettings);
            services.MakeSettingsAvailableForUse(fetchAzureStorageSettings);
            services.MakeSettingsAvailableForUse(fetchMessagingServiceBusSettings);
            services.MakeSettingsAvailableForUse(fetchFailureLogsSettings);
        }

        private static void RegisterServices(
            this IServiceCollection services,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings)
        {
            services.AddScoped<IDonorFileImporter, DonorFileImporter>();
            services.AddScoped<IDonorImportFileParser, DonorImportFileParser>();
            services.AddScoped<IDonorRecordChangeApplier, DonorRecordChangeApplier>();
            services.RegisterCommonGeneticServices();
            services.AddScoped<IImportedLocusInterpreter, ImportedLocusInterpreter>();
            services.AddScoped<IDonorImportFileHistoryService, DonorImportFileHistoryService>();
            services.AddScoped<IDonorImportLogService, DonorImportLogService>();
            services.AddScoped<IDonorUpdateCategoriser, DonorUpdateCategoriser>();

            services.AddScoped<IDonorUpdatesSaver, DonorUpdatesSaver>();
            services.AddScoped<IDonorUpdatesPublisher, DonorUpdatesPublisher>();
            services.AddScoped<IMessageBatchPublisher<SearchableDonorUpdate>, MessageBatchPublisher<SearchableDonorUpdate>>(sp =>
            {
                var serviceBusSettings = fetchMessagingServiceBusSettings(sp);
                var client = sp.GetRequiredKeyedService<ServiceBusClient>(typeof(MessagingServiceBusSettings));
                return new MessageBatchPublisher<SearchableDonorUpdate>(client, serviceBusSettings.UpdatedSearchableDonorsTopic);
            });
            services.AddScoped<IDonorUpdatesCleaner, DonorUpdatesCleaner>();

            services.AddScoped<IDonorIdChecker, DonorIdChecker>();
            services.AddScoped<IDonorIdCheckerFileParser, DonorIdCheckerFileParser>();
            services.AddScoped<IDonorInfoCheckerMessageSender, DonorCheckerMessageSender>(sp =>
            {
                var messagingServiceBusSettings = fetchMessagingServiceBusSettings(sp);
                var logger = sp.GetService<ILogger>();
                var topicClientFactory = sp.GetRequiredKeyedService<ITopicClientFactory>(typeof(MessagingServiceBusSettings));
                return new DonorCheckerMessageSender(logger, topicClientFactory, messagingServiceBusSettings.DonorInfoCheckerResultsTopic);
            });
            services.AddScoped<IDonorIdCheckerMessageSender, DonorCheckerMessageSender>(sp =>
            {
                var messagingServiceBusSettings = fetchMessagingServiceBusSettings(sp);
                var logger = sp.GetService<ILogger>();
                var topicClientFactory = sp.GetRequiredKeyedService<ITopicClientFactory>(typeof(MessagingServiceBusSettings));
                return new DonorCheckerMessageSender(logger, topicClientFactory, messagingServiceBusSettings.DonorIdCheckerResultsTopic);
            });

            services.AddScoped<IDonorIdCheckerBlobStorageClient, DonorCheckerBlobStorageClient>(sp =>
            {
                var storageSettings = fetchAzureStorageSettings(sp);
                var logger = sp.GetService<ILogger>();
                return new DonorCheckerBlobStorageClient(logger, storageSettings.ConnectionString, storageSettings.DonorFileBlobContainer,
                    storageSettings.DonorIdCheckerResultsBlobContainer);
            });
            services.AddScoped<IDonorInfoCheckerBlobStorageClient, DonorCheckerBlobStorageClient>(sp =>
            {
                var storageSettings = fetchAzureStorageSettings(sp);
                var logger = sp.GetService<ILogger>();
                return new DonorCheckerBlobStorageClient(logger, storageSettings.ConnectionString, storageSettings.DonorFileBlobContainer,
                    storageSettings.DonorInfoCheckerResultsBlobContainer);
            });

            services.AddScoped<IDonorInfoChecker, DonorInfoChecker>();
            services.AddScoped<IDonorUpdateMapper, DonorUpdateMapper>();
            services.AddScoped<IDonorImportMessageSender, DonorImportMessageSender>();
            services.AddScoped<IDonorImportFailuresCleaner, DonorImportFailuresCleaner>();
        }

        private static void RegisterDebugServices(
            this IServiceCollection services,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings
            )
        {
            var serviceKey = typeof(MessagingServiceBusSettings);
            services.RegisterServiceBusAsKeyedServices(
                serviceKey,
                sp => fetchMessagingServiceBusSettings(sp).ConnectionString
                );

            services.AddScoped<IDonorImportResultsPeeker, DonorImportResultsPeeker>(sp =>
            {
                var settings = fetchMessagingServiceBusSettings(sp);
                return new DonorImportResultsPeeker(
                    sp.GetRequiredKeyedService<IMessageReceiverFactory>(serviceKey),
                    settings.DonorImportResultsTopic,
                    settings.DonorImportResultsDebugSubscription);
            });

            services.AddScoped<IDonorImportBlobStorageClient, DonorImportBlobStorageClient>(sp =>
            {
                var storageSettings = fetchAzureStorageSettings(sp);
                var logger = sp.GetService<ILogger>();
                return new DonorImportBlobStorageClient(logger, storageSettings.ConnectionString, storageSettings.DonorFileBlobContainer);
            });
        }

        private static void RegisterDonorReaderServices(this IServiceCollection services)
        {
            services.AddScoped<IDonorReader, DonorReader>();
        }

        private static void RegisterClients(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, NotificationsServiceBusSettings> fetchNotificationsServiceBusSettings)
        {
            services.RegisterNotificationSender(fetchNotificationsServiceBusSettings, fetchApplicationInsightsSettings);
        }

        private static void RegisterDonorImportLogger(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetryWorkerService();
            services.AddScoped<DonorImportLoggingContext>();
            services.AddScoped(typeof(IDonorImportLogger<>), typeof(DonorImportLogger<>));
        }
    }
}