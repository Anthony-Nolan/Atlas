using Atlas.Common.Notifications;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Clients;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Settings.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.DonorImport.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterDonorImportTypes(this IServiceCollection services)
        {
            services.RegisterSettings();
            services.RegisterClients();
            services.RegisterServices();
            services.RegisterDatabaseTypes();
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

        private static void RegisterClients(this IServiceCollection services)
        {
            services.AddScoped<IMessagingServiceBusClient, MessagingServiceBusClient>();
            services.AddScoped<INotificationsClient, NotificationsClient>();
        }

        private static void RegisterDatabaseTypes(this IServiceCollection services)
        {
            services.AddScoped<IDonorRepository>(sp =>
                // TODO: ATLAS-186: Ensure this is passed into an appropriate public configuration method when accessing from another project
                new DonorRepository(sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["Sql"])
            );
        }
    }
}