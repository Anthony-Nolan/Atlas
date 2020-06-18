using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.NovaHttpClient.Client;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.HlaService;
using Atlas.MultipleAlleleCodeDictionary.MacCacheService;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices.SourceData;
using Atlas.MultipleAlleleCodeDictionary.Services;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Atlas.MultipleAlleleCodeDictionary.utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Atlas.MultipleAlleleCodeDictionary.ExternalInterface
{
    public static class ServiceConfiguration
    {
        public static void RegisterMacDictionary(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, MacImportSettings> fetchMacImportSettings)
        {
            services.RegisterSettings(fetchApplicationInsightsSettings, fetchMacImportSettings);
            services.RegisterServices();
            services.RegisterLifeTimeScopedCacheTypes();
        }

        private static void RegisterSettings(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, MacImportSettings> fetchMacImportSettings)
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
            services.RegisterAtlasLogger(sp => sp.GetService<IOptions<ApplicationInsightsSettings>>().Value);
            services.AddScoped<IMacCacheService, MacCacheService.MacCacheService>();
            services.AddScoped<IMacDictionary, MacDictionary>();
            services.AddScoped<IMacExpander, MacExpander>();
        }

        private static IHlaServiceClient GetHlaServiceClient(
            string hlaClientApiKey,
            string hlaClientBaseUrl,
            ILogger logger)
        {
            var clientSettings = new HttpClientSettings
            {
                ApiKey = hlaClientApiKey,
                BaseUrl = hlaClientBaseUrl,
                ClientName = "hla_service_client",
                JsonSettings = new JsonSerializerSettings()
            };
            
            try
            {
                return new HlaServiceClient(clientSettings, logger);
            }
            // When running on startup, the client setup will often throw a NullReferenceException.
            // This appears to go away when running not immediately after startup, so we retry once to circumvent
            catch (NullReferenceException)
            {
                return new HlaServiceClient(clientSettings, logger);
            }
        }
    }
}