using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Notifications;
using Atlas.Common.ServiceBus;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Data;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.ExternalInterface.Settings;
using Atlas.DonorImport.ExternalInterface.Settings.ServiceBus;
using Atlas.DonorImport.Models.Mapping;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Services.DonorIdChecker;
using Atlas.DonorImport.Services.DonorUpdates;
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
            Func<IServiceProvider, StalledFileSettings> fetchStalledFileSettings,
            Func<IServiceProvider, PublishDonorUpdatesSettings> fetchPublishDonorUpdatesSettings,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings,
            Func<IServiceProvider, string> fetchSqlConnectionString)
        {
            // Perform static Dapper set up that should be performed once before any SQL requests are made.
            Initialise.InitaliseDapper();

            services.RegisterSettings(
                fetchNotificationConfigurationSettings, fetchStalledFileSettings, fetchPublishDonorUpdatesSettings, fetchAzureStorageSettings);
            services.RegisterClients(fetchApplicationInsightsSettings, fetchNotificationsServiceBusSettings);
            services.RegisterAtlasLogger(fetchApplicationInsightsSettings);
            services.RegisterServices(fetchMessagingServiceBusSettings);
            services.RegisterImportDatabaseTypes(fetchSqlConnectionString);
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
            Func<IServiceProvider, StalledFileSettings> fetchStalledFileSettings,
            Func<IServiceProvider, PublishDonorUpdatesSettings> fetchPublishDonorUpdatesSettings,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings)
        {
            services.MakeSettingsAvailableForUse(fetchStalledFileSettings);
            services.MakeSettingsAvailableForUse(fetchNotificationConfigurationSettings);
            services.MakeSettingsAvailableForUse(fetchPublishDonorUpdatesSettings);
            services.MakeSettingsAvailableForUse(fetchAzureStorageSettings);
        }

        private static void RegisterServices(
            this IServiceCollection services,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings)
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
                return new MessageBatchPublisher<SearchableDonorUpdate>(serviceBusSettings.ConnectionString, serviceBusSettings.UpdatedSearchableDonorsTopic);
            });
            services.AddScoped<IDonorUpdatesCleaner, DonorUpdatesCleaner>();

            services.AddScoped<IDonorRecordIdChecker, DonorRecordIdChecker>();
            services.AddScoped<IDonorRecordIdCheckerFileParser, DonorRecordIdCheckerFileParser>();
            services.AddScoped<IDonorRecordIdCheckerBlobStorageClient, DonorRecordIdCheckerBlobStorageClient>();
            services.AddScoped<IDonorRecordIdCheckerNotificationSender, DonorRecordIdCheckerNotificationSender>();
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

        internal static void RegisterImportDatabaseTypes(
             this IServiceCollection services,
             Func<IServiceProvider, string> fetchDonorImportDatabaseConnectionString)
        {
            services.AddScoped<IDonorImportRepository>(sp => new DonorImportRepository(fetchDonorImportDatabaseConnectionString(sp)));
            services.AddScoped<IDonorReadRepository>(sp => new DonorReadRepository(fetchDonorImportDatabaseConnectionString(sp)));
            services.AddScoped<IDonorImportHistoryRepository>(sp => new DonorImportHistoryRepository(fetchDonorImportDatabaseConnectionString(sp)));
            services.AddScoped<IDonorImportLogRepository>(sp => new DonorImportLogRepository(fetchDonorImportDatabaseConnectionString(sp)));
            services.AddScoped<IPublishableDonorUpdatesRepository>(sp => new PublishableDonorUpdatesRepository(fetchDonorImportDatabaseConnectionString(sp)));
        }
    }
}