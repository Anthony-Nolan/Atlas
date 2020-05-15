using Atlas.HLAService.Client;
using Atlas.MatchingAlgorithm.Clients.Http.DonorService;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Data.Context;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.HlaMetadataDictionary.Services;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories;
using Atlas.Utils.Core.Models;
using Atlas.Utils.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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
            services.Configure<IntegrationTestSettings>(configuration.GetSection("Testing"));

            services.RegisterSearchAlgorithmTypes();
            services.RegisterAllHlaMetadataDictionaryTypes();
            services.RegisterDataServices();
            services.RegisterDonorManagementServices();
            
            services.AddScoped(sp =>
                new ContextFactory().Create(sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["SqlA"])
            );

            // HlaMetadataDictionary Overrides
            services.AddScoped<IWmdaHlaVersionProvider, MockHlaVersionProvider>();
            services.AddScoped<IActiveHlaVersionAccessor, MockHlaVersionProvider>();
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