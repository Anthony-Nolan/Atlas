using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nova.HLAService.Client;
using Nova.SearchAlgorithm.Clients;
using Nova.SearchAlgorithm.Clients.Http;
using Nova.SearchAlgorithm.Data.Context;
using Nova.SearchAlgorithm.DependencyInjection;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers;
using Nova.Utils.Models;
using NSubstitute;

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
            services.RegisterMatchingDictionaryTypes();
            services.RegisterDataServices();
            
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

            services.AddScoped(sp => Substitute.For<IDonorServiceClient>());

            // Integration Test Types
            services.AddScoped<IMemoryCache, MemoryCache>(sp => new MemoryCache(new MemoryCacheOptions()));

            return services.BuildServiceProvider();
        }

    }
}