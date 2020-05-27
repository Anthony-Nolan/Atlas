using Atlas.Common.ApplicationInsights;
using Atlas.Common.NovaHttpClient.Client;
using Atlas.MultipleAlleleCodeDictionary.HlaService;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using Atlas.Common.Caching;

namespace Atlas.MultipleAlleleCodeDictionary
{
    public static class MacDictionaryDependencyRegistration //QQ combine with Josh's Registration code.
    {
        public static void RegisterMacDictionaryServices(
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
