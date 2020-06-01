using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Atlas.MatchingAlgorithm.DependencyInjection
{
    /// <summary>
    /// Contains registrations necessary to set up a project-project interface with the matching algorithm.
    /// e.g. Top level Atlas function will need to be able to queue searches, but does not need to be able to run them. 
    /// </summary>
    public static class ProjectInterfaceServiceConfiguration
    {
        public static void RegisterMatchingAlgorithmOrchestration(this IServiceCollection services)
        {
            services.RegisterSettings();
            services.RegisterServices();
        }

        private static void RegisterSettings(this IServiceCollection services)
        {
            services.RegisterOptions<MessagingServiceBusSettings>("MessagingServiceBus");
        }

        private static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<ISearchServiceBusClient, SearchServiceBusClient>(sp =>
            {
                var serviceBusSettings = sp.GetService<IOptions<MessagingServiceBusSettings>>().Value;
                return new SearchServiceBusClient(
                    serviceBusSettings.ConnectionString,
                    serviceBusSettings.SearchRequestsQueue,
                    serviceBusSettings.SearchResultsTopic
                );
            });

            services.AddScoped<ISearchDispatcher, SearchDispatcher>();
        }
    }
}