using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations;
using Autofac;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Caching.Memory;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.Config;
using Nova.SearchAlgorithm.Config.Modules;
using Nova.SearchAlgorithm.Data;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Services.DonorImport;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.SearchAlgorithm.Services.Scoring;
using Nova.SearchAlgorithm.Services.Scoring.Confidence;
using Nova.SearchAlgorithm.Services.Scoring.Grading;
using Nova.SearchAlgorithm.Services.Scoring.Ranking;
using Nova.SearchAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Models;
using Nova.Utils.WebApi.ApplicationInsights;
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
            var config = new Data.Migrations.Configuration();
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
            
            // Matching Dictionary Overrides
            builder.RegisterType<FileBackedHlaScoringLookupRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<FileBackedHlaMatchingLookupRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<FileBackedAlleleNamesLookupRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            
            // Clients
            var mockHlaServiceClient = Substitute.For<IHlaServiceClient>();
            mockHlaServiceClient.GetAntigens(Arg.Any<LocusType>(), Arg.Any<bool>()).Returns(new List<Antigen>());
            builder.RegisterInstance(mockHlaServiceClient).AsImplementedInterfaces().SingleInstance();

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
                context.Database.Delete();
            }
        }
    }
}