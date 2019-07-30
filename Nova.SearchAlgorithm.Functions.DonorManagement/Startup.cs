using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Nova.SearchAlgorithm.DependencyInjection;
using Nova.SearchAlgorithm.Settings;
using Startup = Nova.SearchAlgorithm.Functions.DonorManagement.Startup;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Nova.SearchAlgorithm.Functions.DonorManagement
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterSettings(builder);
            builder.Services.RegisterHlaServiceClient();
            builder.Services.RegisterDataServices();
            builder.Services.RegisterMatchingDictionaryTypes();
            builder.Services.RegisterSearchAlgorithmTypes();
        }

        private static void RegisterSettings(IFunctionsHostBuilder builder)
        {
            builder.AddUserSecrets();
            builder.RegisterSettings<ApplicationInsightsSettings>("ApplicationInsights");
            builder.RegisterSettings<MessagingServiceBusSettings>("MessagingServiceBus");
            builder.RegisterSettings<HlaServiceSettings>("Client.HlaService");
        }
    }
}