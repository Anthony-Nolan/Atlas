using System;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
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
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings)
        {
            services.RegisterSettings(fetchMessagingServiceBusSettings);
            services.RegisterServices();
        }

        private static void RegisterSettings(
            this IServiceCollection services,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings)
        {
            services.MakeSettingsAvailableForUse(fetchMessagingServiceBusSettings);
        }

        private static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<ISearchServiceBusClient, SearchServiceBusClient>();
            services.AddScoped<ISearchDispatcher, SearchDispatcher>();
        }
    }
}