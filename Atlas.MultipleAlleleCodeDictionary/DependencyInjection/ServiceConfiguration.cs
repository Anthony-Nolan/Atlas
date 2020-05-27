using Atlas.MultipleAlleleCodeDictionary.MacImportService;
using Atlas.MultipleAlleleCodeDictionary.utils;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MultipleAlleleCodeDictionary.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterMultipleAlleleCodeDictionaryTypes(this IServiceCollection services)
        {
            services.RegisterServices();
        }

        private static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<IMacRepository, MacRepository>();
            services.AddScoped<IMacParser, MacLineParser>();
            services.AddScoped<IMacImporter, MacImporter>();
        }
    }
}