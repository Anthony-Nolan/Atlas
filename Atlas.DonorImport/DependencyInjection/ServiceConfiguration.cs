using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.DonorImport.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterDonorImportTypes(this IServiceCollection services)
        {
            services.RegisterServices();
            services.RegisterDatabaseTypes();
        }

        private static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<IDonorFileImporter, DonorFileImporter>();
            services.AddScoped<IDonorOperationApplier, DonorOperationApplier>();
        }
        
        private static void RegisterDatabaseTypes(this IServiceCollection services)
        {
            services.AddScoped<IDonorRepository>(sp =>
                new DonorRepository(sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["Sql"])
            );
            services.AddScoped<IDonorImportFileParser, DonorImportFileParser>();
        }
    }
}