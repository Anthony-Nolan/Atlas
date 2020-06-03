using System;
using Atlas.Common.Notifications;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Clients;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Settings.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.DonorImport.ExternalInterface.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterDonorImport(this IServiceCollection services)
        {
            // TODO: ATLAS-327: Inject settings
            services.RegisterSettings();
            services.RegisterClients();
            services.RegisterServices();

            services.AddScoped<IDonorImportRepository>(sp =>
                // TODO: ATLAS-327: Inject settings
                new DonorImportRepository(sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["Sql"])
            );
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
            services.RegisterOptions<MessagingServiceBusSettings>("MessagingServiceBus");
            services.RegisterOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");
        }

        private static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<IDonorFileImporter, DonorFileImporter>();
            services.AddScoped<IDonorImportFileParser, DonorImportFileParser>();
            services.AddScoped<IDonorRecordChangeApplier, DonorRecordChangeApplier>();
        }

        private static void RegisterDonorReaderServices(this IServiceCollection services)
        {
            services.AddScoped<IDonorReader, DonorReader>();
        }

        private static void RegisterClients(this IServiceCollection services)
        {
            services.AddScoped<IMessagingServiceBusClient, MessagingServiceBusClient>();
            services.AddScoped<INotificationsClient, NotificationsClient>();
        }
    }
}