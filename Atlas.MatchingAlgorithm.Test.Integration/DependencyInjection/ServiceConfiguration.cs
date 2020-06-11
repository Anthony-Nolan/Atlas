using System;
using System.IO;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.DonorImport.ExternalInterface;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.DependencyInjection;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Context;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection
{
    public class ServiceConfiguration
    {
        public static IServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .SetBasePath(Directory.GetCurrentDirectory())
                .Build();
            
            services.AddSingleton<IConfiguration>(sp => configuration);
            
            services.RegisterMatchingAlgorithm();
            services.RegisterMatchingAlgorithmDonorManagement();
            
            // This call must be made after `RegisterMatchingAlgorithm()`, as it overrides the non-mock dictionary set up in that method
            services.RegisterFileBasedHlaMetadataDictionaryForTesting(sp => sp.GetService<IOptions<ApplicationInsightsSettings>>().Value); //These configuration values won't be used, because all they are all (indirectly) overridden, below.

            services.AddScoped(sp =>
                new ContextFactory().Create(sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["SqlA"])
            );

            RegisterMockServices(services);
            RegisterIntegrationTestServices(services);
            
            return services.BuildServiceProvider();
        }

        private static void RegisterMockServices(IServiceCollection services)
        {
            services.AddScoped(sp => Substitute.For<IDonorReader>());

            // Clients
            var mockSearchServiceBusClient = Substitute.For<ISearchServiceBusClient>();
            mockSearchServiceBusClient
                .PublishToSearchQueue(Arg.Any<IdentifiedSearchRequest>())
                .Returns(Task.CompletedTask);
            services.AddScoped(sp => mockSearchServiceBusClient);

            services.AddScoped(sp => Substitute.For<INotificationsClient>());
            
            services.AddScoped(sp => Substitute.For<IAzureDatabaseManager>());
            services.AddScoped(sp => Substitute.For<IAzureFunctionManager>());
        }

        private static void RegisterIntegrationTestServices(IServiceCollection services)
        {
            services.AddScoped<ITestDataRefreshHistoryRepository, TestDataRefreshHistoryRepository>();
        }
    }
}