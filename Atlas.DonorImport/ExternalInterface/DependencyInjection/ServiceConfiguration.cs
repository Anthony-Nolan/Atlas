using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Notifications;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Clients;
using Atlas.DonorImport.Data;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Settings;
using Atlas.DonorImport.ExternalInterface.Settings.ServiceBus;
using Atlas.DonorImport.Models.Mapping;
using Atlas.DonorImport.Services;
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
            Func<IServiceProvider, string> fetchSqlConnectionString)
        {
            // Perform static Dapper set up that should be performed once before any SQL requests are made.
            Initialise.InitaliseDapper();
            
            services.RegisterSettings(fetchMessagingServiceBusSettings, fetchNotificationConfigurationSettings, fetchStalledFileSettings);
            services.RegisterClients(fetchApplicationInsightsSettings, fetchNotificationsServiceBusSettings);
            services.RegisterAtlasLogger(fetchApplicationInsightsSettings);
            services.RegisterServices();
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
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, NotificationConfigurationSettings> fetchNotificationConfigurationSettings,
            Func<IServiceProvider, StalledFileSettings> fetchStalledFileSettings)
        {
            services.MakeSettingsAvailableForUse(fetchMessagingServiceBusSettings);
            services.MakeSettingsAvailableForUse(fetchStalledFileSettings);
            services.MakeSettingsAvailableForUse(fetchNotificationConfigurationSettings);
        }

        private static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<IDonorFileImporter, DonorFileImporter>();
            services.AddScoped<IDonorImportFileParser, DonorImportFileParser>();
            services.AddScoped<IDonorRecordChangeApplier, DonorRecordChangeApplier>();
            services.RegisterCommonGeneticServices();
            services.AddScoped<IImportedLocusInterpreter, ImportedLocusInterpreter>();
            services.AddScoped<IDonorImportFileHistoryService, DonorImportFileHistoryService>();
            services.AddScoped<IDonorImportLogService, DonorImportLogService>();
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
            services.AddScoped<IMessagingServiceBusClient, MessagingServiceBusClient>();
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
        }
    }
}