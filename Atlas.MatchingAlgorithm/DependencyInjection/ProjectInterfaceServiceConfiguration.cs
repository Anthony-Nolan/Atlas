using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.ConfigSettings;
using Atlas.MatchingAlgorithm.Services.Search;
using Microsoft.Extensions.Configuration;
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
        
        public static void RegisterMatchingAlgorithm(this IServiceCollection services)
        {
            services.RegisterMatchingAlgorithmSettings();
            services.RegisterMatchingAlgorithmServices();
        }
        
        private static void RegisterMatchingAlgorithmServices(this IServiceCollection services)
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
        
        private static void RegisterMatchingAlgorithmSettings(this IServiceCollection services)
        {
            services.RegisterOptions<MessagingServiceBusSettings>("MessagingServiceBus");
        }

        // TODO: ATLAS-121: Move this to Atlas.Common once Utils rename is merged
        private static void RegisterOptions<T>(this IServiceCollection services, string sectionName) where T : class
        {
            services.AddOptions<T>().Configure<IConfiguration>((settings, config) => { config.GetSection(sectionName).Bind(settings); });
        }
    }
}