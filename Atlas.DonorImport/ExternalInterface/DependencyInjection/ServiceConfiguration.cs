using System;
using Atlas.DonorImport.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.DonorImport.ExternalInterface.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterDonorReader(
            this IServiceCollection services,
            Func<IServiceProvider, string> fetchDonorImportDatabaseConnectionString)
        {
            services.RegisterServices();
            services.RegisterDatabaseTypes(fetchDonorImportDatabaseConnectionString);
        }

        private static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<IDonorReader, DonorReader>();
        }
        
        private static void RegisterDatabaseTypes(
            this IServiceCollection services,
            Func<IServiceProvider, string> fetchDonorImportDatabaseConnectionString)
        {
            services.AddScoped<IDonorRepository>(sp =>
                new DonorRepository(fetchDonorImportDatabaseConnectionString(sp))
            );
        }
    }
}