using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.SearchAlgorithm.DependencyInjection;
using Nova.SearchAlgorithm.Functions.DonorManagement.Services;
using Nova.SearchAlgorithm.Functions.DonorManagement.Services.ServiceBus;
using Nova.SearchAlgorithm.Functions.DonorManagement.Settings;
using Nova.SearchAlgorithm.Services;
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
            builder.Services.RegisterTypesNeededForMatchingDictionaryLookups();
            builder.Services.RegisterSearchAlgorithmTypes();

            RegisterFunctionServices(builder);
        }

        private static void RegisterSettings(IFunctionsHostBuilder builder)
        {
            builder.AddUserSecrets();
            builder.RegisterSettings<ApplicationInsightsSettings>("ApplicationInsights");
            builder.RegisterSettings<AzureStorageSettings>("AzureStorage");
            builder.RegisterSettings<MessagingServiceBusSettings>("MessagingServiceBus");
            builder.RegisterSettings<HlaServiceSettings>("Client.HlaService");
            builder.RegisterSettings<DonorManagementSettings>("MessagingServiceBus.DonorManagement");
        }

        private static void RegisterFunctionServices(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IMessageReceiverFactory, MessageReceiverFactory>(sp =>
                new MessageReceiverFactory(sp.GetService<IOptions<MessagingServiceBusSettings>>().Value.ConnectionString)
            );

            builder.Services.AddScoped<IMessageProcessorService<SearchableDonorUpdateModel>, MessageProcessorService<SearchableDonorUpdateModel>>(sp =>
            {
                var settings = sp.GetService<IOptions<DonorManagementSettings>>().Value;
                var factory = sp.GetService<IMessageReceiverFactory>();
                return new MessageProcessorService<SearchableDonorUpdateModel>(factory, settings.Topic, settings.Subscription);
            });

            builder.Services.AddScoped<IDonorUpdateProcessor, DonorUpdateProcessor>(sp =>
            {
                var settings = sp.GetService<IOptions<DonorManagementSettings>>().Value;
                var messageReceiverService = sp.GetService<IMessageProcessorService<SearchableDonorUpdateModel>>();
                var managementService = sp.GetService<IDonorManagementService>();
                return new DonorUpdateProcessor(messageReceiverService, managementService, int.Parse(settings.BatchSize));
            });
        }
    }
}