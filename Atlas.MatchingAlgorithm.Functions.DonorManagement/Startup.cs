using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Settings;
using Startup = Atlas.MatchingAlgorithm.Functions.DonorManagement.Startup;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.MatchingAlgorithm.Functions.DonorManagement
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterSettings(builder);
            builder.Services.RegisterHlaServiceClient();
            builder.Services.RegisterDataServices();
            builder.Services.RegisterTypesNeededForMatchingDictionaryLookups();
            builder.Services.RegisterSearchAlgorithmTypes();
            builder.Services.RegisterDonorManagementServices();
        }

        private static void RegisterSettings(IFunctionsHostBuilder builder)
        {
            builder.AddUserSecrets();
            builder.RegisterSettings<ApplicationInsightsSettings>("ApplicationInsights");
            builder.RegisterSettings<AzureStorageSettings>("AzureStorage");
            builder.RegisterSettings<MessagingServiceBusSettings>("MessagingServiceBus");
            builder.RegisterSettings<HlaServiceSettings>("Client.HlaService");
            builder.RegisterSettings<DonorManagementSettings>("MessagingServiceBus.DonorManagement");
            builder.RegisterSettings<NotificationsServiceBusSettings>("NotificationsServiceBus");
        }
    }
}