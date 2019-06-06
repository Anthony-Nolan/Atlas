using System.Collections.Generic;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nova.HLAService.Client;
using Nova.SearchAlgorithm.Data;
using Nova.SearchAlgorithm.Data.Persistent;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.Standard.Clients;
using Nova.SearchAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers;
using Nova.Utils.Models;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests
{
    [SetUpFixture]
    public class IntegrationTestSetUp
    {
        [OneTimeSetUp]
        public void Setup()
        {
            DependencyInjection.DependencyInjection.Provider = CreateProvider();
            DependencyInjection.DependencyInjection.Provider.GetService<IStorageEmulator>().Start();
            ResetDatabase();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            DependencyInjection.DependencyInjection.Provider.GetService<IStorageEmulator>().Stop();
        }

        private static void ResetDatabase()
        {
            DatabaseManager.SetupDatabase();
            DatabaseManager.ClearDatabase();
        }

        private static ServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(sp => 
                new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .Build());
            
            ServiceModule.RegisterSearchAlgorithmTypes(services);
            ServiceModule.RegisterMatchingDictionaryTypes(services);
            ServiceModule.RegisterDataServices(services);

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

            services.AddSingleton<IStorageEmulator, StorageEmulator>(sp =>
                new StorageEmulator(sp.GetService<IConfiguration>().GetSection("AppConfig")["StorageEmulatorLocation"])
            );

            return services.BuildServiceProvider();
        }
    }
}