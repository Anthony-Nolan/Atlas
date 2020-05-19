using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.NovaHttpClient;
using Atlas.Common.NovaHttpClient.Client;
using Atlas.MultipleAlleleCodeDictionary.HlaService;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Atlas.MultipleAlleleCodeDictionary
{
    public static class MacDictionaryDependencyRegistration
    {
        public static void RegisterMacDictionaryServices(
            this IServiceCollection services,
            Func<IServiceProvider, string> fetchHlaClientApiKey,
            Func<IServiceProvider, string> fetchHlaClientBaseUrl,
            Func<IServiceProvider, string> fetchInsightsInstrumentationKey)
        {
            services.AddScoped<IAntigenCachingService, NmdpCodeCachingService>();
            services.AddScoped<INmdpCodeCache, NmdpCodeCachingService>();

            RegisterHlaServiceClient(
                services,
                fetchHlaClientApiKey,
                fetchHlaClientBaseUrl,
                fetchInsightsInstrumentationKey);
        }

        private static void RegisterHlaServiceClient(
            this IServiceCollection services,
            Func<IServiceProvider, string> fetchHlaClientApiKey,
            Func<IServiceProvider, string> fetchHlaClientBaseUrl,
            Func<IServiceProvider, string> fetchInsightsInstrumentationKey)

        {
            services.AddSingleton(sp => GetHlaServiceClient(
                    fetchHlaClientApiKey(sp),
                    fetchHlaClientBaseUrl(sp),
                    fetchInsightsInstrumentationKey(sp)
                )
            );
        }

        private static IHlaServiceClient GetHlaServiceClient(
            string hlaClientApiKey,
            string hlaClientBaseUrl,
            string insightsInstrumentationKey)
        {
            var clientSettings = new HttpClientSettings
            {
                ApiKey = hlaClientApiKey,
                BaseUrl = hlaClientBaseUrl,
                ClientName = "hla_service_client",
                JsonSettings = new JsonSerializerSettings()
            };
            var logger = LoggerRegistration.BuildNovaLogger(insightsInstrumentationKey);

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
