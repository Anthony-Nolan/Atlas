using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.MacCacheService;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices.SourceData;
using Atlas.MultipleAlleleCodeDictionary.Services;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Atlas.MultipleAlleleCodeDictionary.utils;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MultipleAlleleCodeDictionary.ExternalInterface.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterMacDictionary(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacImportSettings)
        {
            services.RegisterAtlasLogger(fetchApplicationInsightsSettings);
            services.RegisterSettings(fetchApplicationInsightsSettings, fetchMacImportSettings);
            services.RegisterServices();
            services.RegisterLifeTimeScopedCacheTypes();
        }

        private static void RegisterSettings(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacImportSettings)
        {
            services.AddScoped(fetchApplicationInsightsSettings);
            services.AddScoped(fetchMacImportSettings);
        }

        private static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<IMacRepository, MacRepository>();
            services.AddScoped<IMacParser, MacLineParser>();
            services.AddScoped<IMacImporter, MacImporter>();
            services.AddScoped<IMacCodeDownloader, MacCodeDownloader>();
            services.AddScoped<IMacCacheService, MacCacheService.MacCacheService>();
            services.AddScoped<IMacDictionary, MacDictionary>();
            services.AddScoped<IMacExpander, MacExpander>();
        }
    }
}