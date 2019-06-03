using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using Autofac;
using Microsoft.Extensions.Caching.Memory;
using Nova.DonorService.Client;
using Nova.HLAService.Client;
using Nova.SearchAlgorithm.Config.Modules;
using Nova.SearchAlgorithm.Data;
using Nova.SearchAlgorithm.Data.Migrations;
using Nova.SearchAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers;
using Nova.Utils.Models;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests
{
    public abstract class IntegrationTestBase
    {
        protected IContainer Container;

        protected IDonorIdGenerator DonorIdGenerator
        {
            get
            {
                if (Container == null)
                {
                    throw new Exception("Cannot access injected property before DI container setup");
                }

                return Container.Resolve<IDonorIdGenerator>();
            }
        }
        
        protected static IDonorServiceClient MockDonorServiceClient { get; set; }

        [OneTimeSetUp]
        public void Setup()
        {
            StorageEmulator.Start();
            Container = CreateContainer();
            ResetDatabase();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            StorageEmulator.Stop();
        }

        private void ResetDatabase()
        {
            ClearDatabase();
            SetupDatabase();
        }

        private void SetupDatabase()
        {
            if (Container.TryResolve(out SearchAlgorithmContext context))
            {
                context.Database.CreateIfNotExists();
            }
            var config = new Configuration();
            var migrator = new DbMigrator(config);
            migrator.Update();
        }

        // This is almost a duplicate of the container in 
        // Nova.SearchAlgorithm.Config.Modules.ServiceModule
        private static IContainer CreateContainer()
        {
            var builder = new ContainerBuilder();
            
            ServiceModule.RegisterSearchAlgorithmTypes(builder);
            ServiceModule.RegisterMatchingDictionaryTypes(builder);
            ServiceModule.RegisterDataServices(builder);
            
            // Matching Dictionary Overrides
            builder.RegisterType<FileBackedHlaScoringLookupRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<FileBackedHlaMatchingLookupRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<FileBackedAlleleNamesLookupRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<FileBackedTceLookupRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            
            // Clients
            var mockHlaServiceClient = Substitute.For<IHlaServiceClient>();
            mockHlaServiceClient.GetAntigens(Arg.Any<LocusType>(), Arg.Any<bool>()).Returns(new List<Antigen>());
            builder.RegisterInstance(mockHlaServiceClient).AsImplementedInterfaces().SingleInstance();

            MockDonorServiceClient = Substitute.For<IDonorServiceClient>();
            builder.RegisterInstance(MockDonorServiceClient).AsImplementedInterfaces().SingleInstance();
            
            // Integration Test Types
            builder.RegisterType<MemoryCache>().As<IMemoryCache>().WithParameter("optionsAccessor", new MemoryCacheOptions()).SingleInstance();
            builder.RegisterType<DonorIdGenerator>().AsImplementedInterfaces().SingleInstance();

            return builder.Build();
        }

        /// <summary>
        /// Clears the test database. Can be accessed by fixtures to run after each fixture, but not after each test.
        /// </summary>
        protected void ClearDatabase()
        {
            if (Container.TryResolve(out SearchAlgorithmContext context))
            {
                context.Donors.RemoveRange(context.Donors);
                context.MatchingHlaAtA.RemoveRange(context.MatchingHlaAtA);
                context.MatchingHlaAtB.RemoveRange(context.MatchingHlaAtB);
                context.MatchingHlaAtC.RemoveRange(context.MatchingHlaAtC);
                context.MatchingHlaAtDrb1.RemoveRange(context.MatchingHlaAtDrb1);
                context.MatchingHlaAtDqb1.RemoveRange(context.MatchingHlaAtDqb1);
                context.SaveChanges();
            }
        }
    }
}