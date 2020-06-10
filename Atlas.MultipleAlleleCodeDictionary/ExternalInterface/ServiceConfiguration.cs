using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.NovaHttpClient.Client;
using Atlas.Common.Utils.Extensions;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.HlaService;
using Atlas.MultipleAlleleCodeDictionary.MacCacheService;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices.SourceData;
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
            Func<IServiceProvider, IOptions<ApplicationInsightsSettings>> fetchApplicationInsightsSettings,
            Func<IServiceProvider, IOptions<MacImportSettings>> fetchMacImportSettings)
        {
            services.RegisterSettings(fetchApplicationInsightsSettings, fetchMacImportSettings);
            services.RegisterServices();
            services.RegisterLifeTimeScopedCacheTypes();
        }
        
        /// <remarks>
        /// Expected to be made obsolete once the new Mac Dictionary
        /// process is fully implemented.
        /// TODO: Atlas-54
        /// </remarks>
        public static void RegisterMacDictionaryUsageServices(
            this IServiceCollection services,
            Func<IServiceProvider, string> fetchHlaClientApiKey,
            Func<IServiceProvider, string> fetchHlaClientBaseUrl,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings)
        {
            services.RegisterLifeTimeScopedCacheTypes();
            services.RegisterAtlasLogger(fetchApplicationInsightsSettings);
            services.AddScoped<IAntigenCachingService, NmdpCodeCachingService>();
            services.AddScoped<INmdpCodeCache, NmdpCodeCachingService>();

            services.RegisterHlaServiceClient(
                fetchHlaClientApiKey,
                fetchHlaClientBaseUrl);
        }
        
        private static void RegisterSettings(
            this IServiceCollection services,
            Func<IServiceProvider, IOptions<ApplicationInsightsSettings>> fetchApplicationInsightsSettings,
            Func<IServiceProvider, IOptions<MacImportSettings>> fetchMacImportSettings)
        {
            services.AddScoped(sp => fetchApplicationInsightsSettings(sp).Value);
            services.AddScoped(sp => fetchMacImportSettings(sp).Value);
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
        
        private static void RegisterHlaServiceClient(
            this IServiceCollection services,
            Func<IServiceProvider, string> fetchHlaClientApiKey,
            Func<IServiceProvider, string> fetchHlaClientBaseUrl)
        {
            services.AddSingleton(sp => GetHlaServiceClient(
                    fetchHlaClientApiKey(sp),
                    fetchHlaClientBaseUrl(sp),
                    sp.GetService<ILogger>()
                )
            );
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