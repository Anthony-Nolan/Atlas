using System.Collections.Generic;
using System.Configuration;
using Autofac;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Caching.Memory;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Config;
using Nova.SearchAlgorithm.Data;
using Nova.SearchAlgorithm.Repositories;
using Nova.SearchAlgorithm.Repositories.Donors;
using Nova.SearchAlgorithm.Repositories.Donors.AzureStorage;
using Nova.SearchAlgorithm.Repositories.Donors.CosmosStorage;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.SearchAlgorithm.Test.Integration.FileBackedMatchingDictionary;
using Nova.SearchAlgorithm.Test.Integration.Integration.FileBackedMatchingDictionary;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Solar;
using Nova.Utils.WebApi.ApplicationInsights;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Integration.Integration
{
    [TestFixture(DonorStorageImplementation.CloudTable)]
    [TestFixture(DonorStorageImplementation.SQL)]
    //[TestFixture(DonorStorageImplementation.Cosmos)]
    public abstract class IntegrationTestBase
    {
        private readonly StorageEmulator tableStorageEmulator = new StorageEmulator();
        private readonly CosmosTestDatabase cosmosDatabase = new CosmosTestDatabase();
        private readonly DonorStorageImplementation donorStorageImplementation;
        protected IContainer container;

        protected IntegrationTestBase(DonorStorageImplementation input)
        {
            donorStorageImplementation = input;
        }

        [OneTimeSetUp]
        public void Setup()
        {
            // Starting and stopping the tableStorageEmulator is managed in the setup fixture StorageSetup.cs
            tableStorageEmulator.Clear();

            // Starting the cosmos emulator is currently a manual step.
            if (DonorStorageImplementation.Cosmos.Equals(donorStorageImplementation))
            {
                cosmosDatabase.Clear();
            }

            container = CreateContainer();

            if (container.TryResolve(out SearchAlgorithmContext context))
            {
                context.Database.Delete();
            }
        }

        // This is almost a duplicate of the container in 
        // Nova.SearchAlgorithm.Config.Modules.ServiceModule
        private IContainer CreateContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterInstance(AutomapperConfig.CreateMapper())
                .SingleInstance()
                .AsImplementedInterfaces();

            // Switch between testing different implementations
            if (donorStorageImplementation == DonorStorageImplementation.CloudTable)
            {
                builder.RegisterType<CloudTableStorage>().AsImplementedInterfaces().InstancePerLifetimeScope();
                builder.RegisterType<CloudStorageDonorSearchRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            }
            else if (donorStorageImplementation == DonorStorageImplementation.Cosmos)
            {
                builder.RegisterType<CosmosStorage>().AsImplementedInterfaces().InstancePerLifetimeScope();
                builder.RegisterType<CloudStorageDonorSearchRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            }
            else
            {
                builder.RegisterType<SearchAlgorithmContext>().AsSelf().InstancePerLifetimeScope();
                builder.RegisterType<Data.Repositories.SqlDonorSearchRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            }

            builder.RegisterType<FileBackedMatchingDictionaryRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<SolarDonorRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<Scoring.CalculateScore>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.SearchService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.DonorImportService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.HlaUpdateService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.AntigenCachingService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<DonorMatchingService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<DatabaseDonorMatchingService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<DonorMatchCalculator>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<CloudTableFactory>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TableReferenceRepository>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SolarConnectionFactory>().AsImplementedInterfaces().SingleInstance();

            var mockHlaServiceClient = Substitute.For<IHlaServiceClient>();
            mockHlaServiceClient.GetAntigens(Arg.Any<LocusType>(), Arg.Any<bool>()).Returns(new List<Antigen>());
            builder.RegisterInstance(mockHlaServiceClient).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SearchAlgorithm.MatchingDictionary.Data.WmdaFileDownloader>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<FileBackedMatchingDictionaryRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<SearchAlgorithm.MatchingDictionary.Repositories.WmdaDataRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<SearchAlgorithm.MatchingDictionary.Services.HlaMatchingService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<SearchAlgorithm.MatchingDictionary.Services.ManageMatchingDictionaryService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<SearchAlgorithm.MatchingDictionary.Services.MatchingDictionaryLookupService>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<HLAService.Client.Services.AlleleStringSplitterService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HLAService.Client.Services.HlaCategorisationService>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<MemoryCache>().As<IMemoryCache>().WithParameter("optionsAccessor", new MemoryCacheOptions()).SingleInstance();
            
            // Tests should not use Solar, so don't provide an actual connection string.
            var solarSettings = new SolarConnectionSettings();
            builder.RegisterInstance(solarSettings).AsSelf().SingleInstance();

            var logger = new RequestAwareLogger(new TelemetryClient(),
                ConfigurationManager.AppSettings["insights.logLevel"].ToLogLevel());
            builder.RegisterInstance(logger).AsImplementedInterfaces().SingleInstance();

            return builder.Build();
        }
    }

    public enum DonorStorageImplementation
    {
        SQL,
        CloudTable,
        Cosmos
    }
}
