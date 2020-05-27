using Atlas.Common.GeneticData;
using Atlas.Common.Notifications;
using Atlas.MatchingAlgorithm.Clients.Http.DonorService;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Context;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MultipleAlleleCodeDictionary.HlaService;
using Atlas.MultipleAlleleCodeDictionary.HlaService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.DependencyInjection;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.Common.ApplicationInsights;
using Microsoft.Extensions.Options;

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
            
            services.RegisterSettings(configuration);

            services.RegisterSearchAlgorithmTypes();
            services.RegisterFileBasedHlaMetadataDictionaryForTesting(sp => sp.GetService<IOptions<ApplicationInsightsSettings>>().Value); //These configuration values won't be used, because all they are all (indirectly) overridden, below.
            services.RegisterDataServices();
            services.RegisterDonorManagementServices();
            
            services.AddScoped(sp =>
                new ContextFactory().Create(sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["SqlA"])
            );

            services.AddScoped<IActiveHlaVersionAccessor, MockHlaVersionProvider>();

            // Clients
            var mockSearchServiceBusClient = Substitute.For<ISearchServiceBusClient>();
            mockSearchServiceBusClient
                .PublishToSearchQueue(Arg.Any<IdentifiedSearchRequest>())
                .Returns(Task.CompletedTask);
            services.AddScoped(sp => mockSearchServiceBusClient);

            services.AddScoped(sp => Substitute.For<IDonorServiceClient>());
            services.AddScoped(sp => Substitute.For<INotificationsClient>());

            return services.BuildServiceProvider();
        }
    }
}