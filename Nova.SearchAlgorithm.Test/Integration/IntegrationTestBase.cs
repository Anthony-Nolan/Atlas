using System;
using System.Configuration;
using Microsoft.ApplicationInsights;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Solar;
using Nova.Utils.WebApi.ApplicationInsights;
using Nova.SearchAlgorithm.Config;
using Nova.SearchAlgorithm.Data;
using Nova.SearchAlgorithm.Repositories;
using Nova.SearchAlgorithm.Repositories.Donors;
using Nova.SearchAlgorithm.Test.FileBackedMatchingDictionary;
using NUnit.Framework;
using Autofac;
using Nova.SearchAlgorithm.Repositories.Donors.AzureStorage;
using Nova.SearchAlgorithm.Repositories.Donors.CosmosStorage;

namespace Nova.SearchAlgorithm.Test.Integration
{
    [TestFixture(DonorStorageImplementation.CloudTable)]
    [TestFixture(DonorStorageImplementation.SQL)]
    [TestFixture(DonorStorageImplementation.Cosmos)]
    public abstract class IntegrationTestBase
    {
        private readonly StorageEmulator tableStorageEmulator = new StorageEmulator();
        private readonly CosmosTestDatabase cosmosDatabase = new CosmosTestDatabase();
        private readonly DonorStorageImplementation donorStorageImplementation;
        protected IContainer container;

        protected IntegrationTestBase(DonorStorageImplementation input)
        {
            this.donorStorageImplementation = input;
        }

        [OneTimeSetUp]
        public void Setup()
        {
            // Starting and stopping the tableStorageEmulator is managed in the setup fixture StorageSetup.cs
            tableStorageEmulator.Clear();

            // Starting the cosmos emulator is currently a manual step.
            cosmosDatabase.Clear();

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

            builder.RegisterType<FileBackedMatchingDictionaryLookup>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<SolarDonorRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<Scoring.CalculateScore>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.SearchService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.DonorImportService>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<CloudTableFactory>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SolarConnectionFactory>().AsImplementedInterfaces().SingleInstance();

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
