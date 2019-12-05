using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nova.HLAService.Client;
using Nova.SearchAlgorithm.Clients.Http;
using Nova.SearchAlgorithm.Clients.ServiceBus;
using Nova.SearchAlgorithm.Data.Context;
using Nova.SearchAlgorithm.DependencyInjection;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories;
using Nova.Utils.Models;
using Nova.Utils.Notifications;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.Integration.DependencyInjection
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
            services.Configure<IntegrationTestSettings>(configuration.GetSection("Testing"));

            services.RegisterSearchAlgorithmTypes();
            services.RegisterAllMatchingDictionaryTypes();
            services.RegisterDataServices();
            services.RegisterDonorManagementServices();
            
            services.AddScoped(sp =>
                new ContextFactory().Create(sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["SqlA"])
            );

            // Matching Dictionary Overrides
            services.AddScoped<IHlaScoringLookupRepository, FileBackedHlaScoringLookupRepository>();
            services.AddScoped<IHlaMatchingLookupRepository, FileBackedHlaMatchingLookupRepository>();
            services.AddScoped<IAlleleNamesLookupRepository, FileBackedAlleleNamesLookupRepository>();
            services.AddScoped<IDpb1TceGroupsLookupRepository, FileBackedTceLookupRepository>();

            // Clients
            var mockHlaServiceClient = Substitute.For<IHlaServiceClient>();
            mockHlaServiceClient.GetAntigens(Arg.Any<LocusType>(), Arg.Any<bool>()).Returns(new List<Antigen>());
            services.AddScoped(sp => mockHlaServiceClient);

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