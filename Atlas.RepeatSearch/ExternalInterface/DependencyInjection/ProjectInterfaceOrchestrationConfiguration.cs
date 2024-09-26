using System;
using Atlas.Common.ServiceBus.DependencyInjection;
using Atlas.Common.Utils.Extensions;
using Atlas.RepeatSearch.Clients;
using Atlas.RepeatSearch.Services.Search;
using Atlas.RepeatSearch.Settings.ServiceBus;
using Atlas.SearchTracking.Common.Clients;
using Atlas.SearchTracking.Common.Settings.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.RepeatSearch.ExternalInterface.DependencyInjection
{
    /// <summary>
    /// Contains registrations necessary to set up a project-project interface for orchestration of repeat searching.
    /// e.g. Top level Atlas function will need to be able to queue searches, but does not need to be able to run them.
    /// </summary>
    public static class ProjectInterfaceOrchestrationConfiguration
    {
        public static void RegisterRepeatSearchOrchestration(
            this IServiceCollection services,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, SearchTrackingServiceBusSettings> fetchSearchTrackingServiceBusSettings)
        {
            services.RegisterSettings(fetchMessagingServiceBusSettings);
            services.RegisterSearchTrackingSettings(fetchSearchTrackingServiceBusSettings);
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

            services.AddScoped<IRepeatSearchServiceBusClient, RepeatSearchServiceBusClient>();
            services.AddScoped<ISearchTrackingServiceBusClient, SearchTrackingServiceBusClient>();
            services.AddScoped<IRepeatSearchDispatcher, RepeatSearchDispatcher>();
        }
    }
}