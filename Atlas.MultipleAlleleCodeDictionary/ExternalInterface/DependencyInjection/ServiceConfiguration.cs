using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.Utils.Extensions;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.Services;
using Atlas.MultipleAlleleCodeDictionary.Services.MacImportServices;
using Atlas.MultipleAlleleCodeDictionary.Services.MacImportServices.SourceData;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MultipleAlleleCodeDictionary.ExternalInterface.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterMacDictionary(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacDictionarySettings
        )
        {
            services.RegisterMacDictionarySettings(fetchApplicationInsightsSettings, fetchMacDictionarySettings);
            services.RegisterMacDictionaryServices();
            services.RegisterAtlasLogger(fetchApplicationInsightsSettings);
            services.RegisterLifeTimeScopedCacheTypes();
        }

        public static void RegisterMacImport(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacDictionarySettings,
            Func<IServiceProvider, MacImportSettings> fetchMacImportSettings
        )
        {
            services.RegisterMacImportSettings(fetchApplicationInsightsSettings, fetchMacDictionarySettings, fetchMacImportSettings);
            services.RegisterMacImportServices();
            services.RegisterAtlasLogger(fetchApplicationInsightsSettings);
            services.RegisterLifeTimeScopedCacheTypes();
        }

        private static void RegisterMacDictionarySettings(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacDictionarySettings)
        {
            services.MakeSettingsAvailableForUse(fetchApplicationInsightsSettings);
            services.MakeSettingsAvailableForUse(fetchMacDictionarySettings);
        }

        private static void RegisterMacImportSettings(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacDictionarySettings,
            Func<IServiceProvider, MacImportSettings> fetchMacImportSettings)
        {
            services.MakeSettingsAvailableForUse(fetchApplicationInsightsSettings);
            services.MakeSettingsAvailableForUse(fetchMacDictionarySettings);
            services.MakeSettingsAvailableForUse(fetchMacImportSettings);
        }

        private static void RegisterMacDictionaryServices(this IServiceCollection services)
        {
            services.RegisterSharedServices();

            services.AddScoped<IMacCacheService, MacCacheService>();
            services.AddScoped<IMacDictionary, MacDictionary>();
            services.AddScoped<IMacExpander, MacExpander>();
        }

        private static void RegisterMacImportServices(this IServiceCollection services)
        {
            services.RegisterSharedServices();

            services.AddScoped<IMacParser, MacLineParser>();
            services.AddScoped<IMacImporter, MacImporter>();
            services.AddScoped<IMacCodeDownloader, MacCodeDownloader>();
        }
     
        private static void RegisterSharedServices(this IServiceCollection services)
        {
            services.AddScoped<IMacRepository, MacRepository>();
        }
    }
}