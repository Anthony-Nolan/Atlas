using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Nova.SearchAlgorithm.DependencyInjection;
using Nova.SearchAlgorithm.Functions;
using Nova.SearchAlgorithm.Settings;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Nova.SearchAlgorithm.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterSettings(builder);
            builder.Services.RegisterClients();
            builder.Services.RegisterDataServices();
            builder.Services.RegisterMatchingDictionaryTypes();
            builder.Services.RegisterSearchAlgorithmTypes();
        }

        private static void RegisterSettings(IFunctionsHostBuilder builder)
        {
            builder.AddUserSecrets();
            builder.RegisterSettings<ApplicationInsightsSettings>("ApplicationInsights");
            builder.RegisterSettings<AzureStorageSettings>("AzureStorage");
            builder.RegisterSettings<DonorServiceSettings>("Client.DonorService");
            builder.RegisterSettings<HlaServiceSettings>("Client.HlaService");
            builder.RegisterSettings<WmdaSettings>("Wmda");
            builder.RegisterSettings<MessagingServiceBusSettings>("MessagingServiceBus");
            builder.RegisterSettings<AzureAuthenticationSettings>("AzureManagement.Authentication");
            builder.RegisterSettings<AzureAppServiceManagementSettings>("AzureManagement.AppService");
            builder.RegisterSettings<AzureDatabaseManagementSettings>("AzureManagement.Database");
            builder.RegisterSettings<DataRefreshSettings>("DataRefresh");
            builder.RegisterSettings<NotificationsServiceBusSettings>("NotificationsServiceBus");
        }
    }
}