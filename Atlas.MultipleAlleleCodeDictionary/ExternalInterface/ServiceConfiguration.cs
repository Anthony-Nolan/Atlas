using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.Utils.Extensions;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.MacCacheService;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices.SourceData;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Atlas.MultipleAlleleCodeDictionary.utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Atlas.MultipleAlleleCodeDictionary.ExternalInterface
{
    public static class ServiceConfiguration
    {
        public static void RegisterMacDictionary(this IServiceCollection services)
        {
            services.RegisterSettings();
            services.RegisterServices();
            services.RegisterLifeTimeScopedCacheTypes();
        }
        
        private static void RegisterSettings(this IServiceCollection services)
        {
            services.RegisterOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterOptions<MacImportSettings>("MacImport");
        }
        
        private static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<IMacRepository, MacRepository>();
            services.AddScoped<IMacParser, MacLineParser>();
            services.AddScoped<IMacImporter, MacImporter>();
            services.AddScoped<IMacCodeDownloader, MacCodeDownloader>();
            services.RegisterAtlasLogger(sp => sp.GetService<IOptions<ApplicationInsightsSettings>>().Value);
            services.AddScoped<IMacCache, MacCache>();
            services.AddScoped<IMultipleAlleleCodeDictionary, MultipleAlleleCodeDictionary>();
        }
    }
}