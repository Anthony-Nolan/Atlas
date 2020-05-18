using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.ConfigSettings;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Services.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Atlas.Functions
{
    public static class ServiceConfiguration
    {
        public static void RegisterMatchingAlgorithm(this IServiceCollection services)
        {
            services.RegisterMatchingAlgorithmServices();
            services.RegisterMatchingAlgorithmSettings();
        }
    }
}