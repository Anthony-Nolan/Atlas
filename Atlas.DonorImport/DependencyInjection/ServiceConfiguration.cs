using Atlas.DonorImport.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.DonorImport.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterDonorImportTypes(this IServiceCollection services)
        {
            services.AddScoped<IDonorImporter, DonorImporter>();
        }
    }
}