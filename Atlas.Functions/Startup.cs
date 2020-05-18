using Atlas.Functions;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.ConfigSettings;
using Atlas.MatchingAlgorithm.Services.Search;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterMatchingComponentTypes(builder);
        }

        private static void RegisterMatchingComponentTypes(IFunctionsHostBuilder builder)
        {
            builder.Services.RegisterSettings();
            builder.Services.RegisterMatchingAlgorithmServices();
        }
    }
}