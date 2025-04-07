using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus.DependencyInjection;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Atlas.SearchTracking.Common.Clients;
using Atlas.SearchTracking.Common.Settings.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MatchingAlgorithm.DependencyInjection
{
    /// <summary>
    /// Contains registrations necessary to set up a project-project interface for orchestration of the matching algorithm.
    /// e.g. Top level Atlas function will need to be able to queue searches, but does not need to be able to run them.
    /// </summary>
    public static class ProjectInterfaceOrchestrationConfiguration
    {
        public static void RegisterMatchingAlgorithmOrchestration(
            this IServiceCollection services,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, SearchTrackingServiceBusSettings> fetchSearchTrackingServiceBusSettings,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings)
        {
            services.RegisterSettings(fetchMessagingServiceBusSettings);
            services.RegisterSearchTrackingSettings(fetchSearchTrackingServiceBusSettings);
            services.RegisterAtlasLogger(fetchApplicationInsightsSettings);
            services.RegisterServices();
        }

        private static void RegisterSettings(
            this IServiceCollection services,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings)
        {
            services.MakeSettingsAvailableForUse(fetchMessagingServiceBusSettings);
        }

        private static void RegisterSearchTrackingSettings(
            this IServiceCollection services,
            Func<IServiceProvider, SearchTrackingServiceBusSettings> fetchSearchTrackingServiceBusSettings)
        {
            services.MakeSettingsAvailableForUse(fetchSearchTrackingServiceBusSettings);
        }

        private static void RegisterServices(this IServiceCollection services)
        {
            var serviceKey = typeof(MessagingServiceBusSettings);
            services.RegisterServiceBusAsKeyedServices(
                serviceKey,
                sp => sp.GetRequiredService<MessagingServiceBusSettings>().ConnectionString);

            services.AddScoped<ISearchServiceBusClient, SearchServiceBusClient>();
            services.AddScoped<ISearchTrackingServiceBusClient, SearchTrackingServiceBusClient>();
            services.AddScoped<ISearchDispatcher, SearchDispatcher>();
        }
    }
}