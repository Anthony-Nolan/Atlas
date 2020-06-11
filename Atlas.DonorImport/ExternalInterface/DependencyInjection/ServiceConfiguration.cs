using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Notifications;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Clients;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Models.Mapping;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Settings.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.DonorImport.ExternalInterface.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterDonorImport(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings)
        {
            services.RegisterSettings();
            services.RegisterClients(fetchApplicationInsightsSettings);
            services.RegisterAtlasLogger(fetchApplicationInsightsSettings);
            services.RegisterServices();
            // TODO: ATLAS-327: Inject settings
            services.RegisterImportDatabaseTypes(sp => sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["Sql"]);
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

        private static void RegisterSettings(this IServiceCollection services)
        {
            // TODO: ATLAS-327: Inject settings
            services.RegisterOptions<MessagingServiceBusSettings>("MessagingServiceBus");
            services.RegisterOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");
        }

        private static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<IDonorFileImporter, DonorFileImporter>();
            services.AddScoped<IDonorImportFileParser, DonorImportFileParser>();
            services.AddScoped<IDonorRecordChangeApplier, DonorRecordChangeApplier>();
            services.RegisterCommonGeneticServices();
            services.AddScoped<IImportedLocusInterpreter, ImportedLocusInterpreter>();
        }

        private static void RegisterDonorReaderServices(this IServiceCollection services)
        {
            services.AddScoped<IDonorReader, DonorReader>();
        }

        private static void RegisterClients(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings)
        {
            services.AddScoped<IMessagingServiceBusClient, MessagingServiceBusClient>();
            services.RegisterNotificationSender(
                OptionsReaderFor<NotificationsServiceBusSettings>(), //TODO: ATLAS-327
                fetchApplicationInsightsSettings
            );
        }

        private static void RegisterImportDatabaseTypes(
            this IServiceCollection services,
            Func<IServiceProvider, string> fetchDonorImportDatabaseConnectionString)
        {
            services.AddScoped<IDonorImportRepository>(sp => new DonorImportRepository(fetchDonorImportDatabaseConnectionString(sp)));
            services.AddScoped<IDonorReadRepository>(sp => new DonorReadRepository(fetchDonorImportDatabaseConnectionString(sp)));
        }
    }
}