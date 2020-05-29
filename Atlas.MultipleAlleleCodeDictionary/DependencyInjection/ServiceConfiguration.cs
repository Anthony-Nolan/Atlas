using Atlas.Common.Notifications;
using Atlas.Common.Utils.Extensions;
using Atlas.MultipleAlleleCodeDictionary.MacImportService;
using Atlas.MultipleAlleleCodeDictionary.Settings.MacImport;
using Atlas.MultipleAlleleCodeDictionary.utils;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MultipleAlleleCodeDictionary.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterMultipleAlleleCodeDictionaryTypes(this IServiceCollection services)
        {
            services.RegisterServices();
            services.RegisterSettings();
        }
        
        private static void RegisterSettings(this IServiceCollection services)
        {
            services.RegisterOptions<MacImportSettings>("MacImport");
        }


        private static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<IMacRepository, MacRepository>();
            services.AddScoped<IMacParser, MacLineParser>();
            services.AddScoped<IMacImporter, MacImporter>();
            services.AddScoped<IMacCodeDownloader, MacCodeDownloader>();
        }
    }
}