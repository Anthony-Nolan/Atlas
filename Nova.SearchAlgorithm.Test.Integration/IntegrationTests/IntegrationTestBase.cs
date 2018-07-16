using Autofac;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Caching.Memory;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Config;
using Nova.SearchAlgorithm.Data;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Data;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.SearchAlgorithm.Services.Scoring;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.WebApi.ApplicationInsights;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Configuration;
using Nova.SearchAlgorithm.Test.Integration.Storage.FileBackedMatchingDictionaryRepository;
using Configuration = Nova.SearchAlgorithm.Config.Configuration;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests
{
    public abstract class IntegrationTestBase
    {
        protected IContainer container;

        protected IDonorIdGenerator DonorIdGenerator
        {
            get
            {
                if (container == null)
                {
                    throw new Exception("Cannot access injected property before DI container setup");
                }

                return container.Resolve<IDonorIdGenerator>();
            }
        }

        [OneTimeSetUp]
        public void Setup()
        {
            container = CreateContainer();
            ClearDatabase();
            SetupDatabase();
        }

        private void SetupDatabase()
        {
            if (container.TryResolve(out SearchAlgorithmContext context))
            {
                context.Database.CreateIfNotExists();
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

            builder.RegisterType<SearchAlgorithmContext>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<SqlDonorSearchRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<DonorScoringService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<SearchService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<DonorImportService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HlaUpdateService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<AntigenCachingService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<DonorMatchingService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<DatabaseDonorMatchingService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<DonorMatchCalculator>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchFilteringService>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<CloudTableFactory>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TableReferenceRepository>().AsImplementedInterfaces().SingleInstance();

            var mockHlaServiceClient = Substitute.For<IHlaServiceClient>();
            mockHlaServiceClient.GetAntigens(Arg.Any<LocusType>(), Arg.Any<bool>()).Returns(new List<Antigen>());
            builder.RegisterInstance(mockHlaServiceClient).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<WmdaFileDownloader>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<FileBackedHlaMatchingLookupRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<WmdaDataRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<HlaMatchPreCalculationService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ManageMatchingDictionaryService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HlaMatchingLookupService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HlaScoringLookupService>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<AlleleStringSplitterService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HlaCategorisationService>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<MemoryCache>().As<IMemoryCache>().WithParameter("optionsAccessor", new MemoryCacheOptions()).SingleInstance();

            builder.RegisterType<AlleleNamesLookupRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<WmdaDataRepository>()
                .AsImplementedInterfaces()
                .WithParameter("hlaDatabaseVersion", Configuration.HlaDatabaseVersion)
                .InstancePerLifetimeScope();


            builder.RegisterType<AlleleNamesService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<AlleleNamesLookupService>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<AlleleNameHistoriesConsolidator>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<AlleleNamesFromHistoriesExtractor>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<AlleleNameVariantsExtractor>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ReservedAlleleNamesExtractor>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<DonorIdGenerator>().AsImplementedInterfaces().SingleInstance();
            
            var logger = new RequestAwareLogger(new TelemetryClient(),
                ConfigurationManager.AppSettings["insights.logLevel"].ToLogLevel());
            builder.RegisterInstance(logger).AsImplementedInterfaces().SingleInstance();

            return builder.Build();
        }

        /// <summary>
        /// Clears the test database. Can be accessed by fixtures to run after each fixture, but not after each test.
        /// </summary>
        protected void ClearDatabase()
        {
            if (container.TryResolve(out SearchAlgorithmContext context))
            {
                context.Database.Delete();
            }
        }
    }
}