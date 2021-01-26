using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Clients.AzureStorage;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.RepeatSearch.Clients;
using Atlas.RepeatSearch.Settings.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.RepeatSearch.ExternalInterface.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterRepeatSearch(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, string> fetchSqlConnectionString)
        {
            services.RegisterSettings(fetchApplicationInsightsSettings, fetchMessagingServiceBusSettings);
        }

        private static void RegisterSettings(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings)
        {
            services.MakeSettingsAvailableForUse(fetchApplicationInsightsSettings);
            services.MakeSettingsAvailableForUse(fetchMessagingServiceBusSettings);
        }

        private static void RegisterServices(
            this IServiceCollection services)
        {
            services.AddScoped<IRepeatSearchServiceBusClient, RepeatSearchServiceBusClient>();
            services.AddScoped<IActiveHlaNomenclatureVersionAccessor, ActiveHlaNomenclatureVersionAccessor>();
            services.AddScoped<ISearchService>();
            services.AddScoped<MatchingAlgorithmSearchLoggingContext, MatchingAlgorithmSearchLoggingContext>();
            services.AddScoped<IResultsBlobStorageClient, ResultsBlobStorageClient>();
        }
    }
}