using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Nova.SearchAlgorithm.Config;
using Nova.SearchAlgorithm.Services;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Solar;
using Nova.Utils.TestUtils.Assertions;
using NUnit.Framework;
using Autofac;
using Autofac.Builder;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;

namespace Nova.SearchAlgorithm.Test.Integration
{
    [TestFixture]
    public class SearchTests
    {
        // Destined for base class
        private StorageEmulator emulator = new StorageEmulator();
        protected IContainer container;

        // Test only
        private IDonorImportService donorImportService;
        private ISearchService searchService;

        private IContainer CreateContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterInstance(AutomapperConfig.CreateMapper())
                .SingleInstance()
                .AsImplementedInterfaces();

            builder.RegisterType<Repositories.SearchRequests.SearchRequestRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Repositories.Donors.DonorRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Repositories.Hla.HlaRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Repositories.SolarDonorRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<Services.SearchRequestService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.SearchService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.DonorImportService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            
            builder.RegisterType<Repositories.CloudTableFactory>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SolarConnectionFactory>().AsImplementedInterfaces().SingleInstance();

            var solarSettings = new SolarConnectionSettings
            {
                //ConnectionString = ConfigurationManager.ConnectionStrings["SolarConnectionString"].ConnectionString
            };
            builder.RegisterInstance(solarSettings).AsSelf().SingleInstance();

            //var logger = new RequestAwareLogger(new TelemetryClient(),
            //    ConfigurationManager.AppSettings["insights.logLevel"].ToLogLevel());
            //builder.RegisterInstance(logger).AsImplementedInterfaces().SingleInstance();

            return builder.Build();
        }

        [OneTimeSetUp]
        public void Setup()
        {
            container = CreateContainer();
            emulator.Start();
        }

        [OneTimeTearDown]
        public void ShutdownStorage()
        {
            emulator.Stop();
        }

        [SetUp]
        public void CleanStorage()
        {
            emulator.ClearBlobItems();
            emulator.ClearTableItems();
        }

        [OneTimeSetUp]
        public void ImportTestDonors()
        {
            // TODO: realistically use the repo directly to import
            // bespoke test data relevant to this classes tests
            // Using ImportSingleTestDonor is a POC shortcut
            donorImportService = container.Resolve<IDonorImportService>();
            donorImportService.ImportSingleTestDonor();
        }

        [SetUp]
        public void ResolveSearchService()
        {
            searchService = container.Resolve<ISearchService>();
        }

        [Test]
        public void TestSomething()
        {
            IEnumerable<DonorMatch> results = searchService.Search(new SearchRequest
            {
            });

            results.Should().Contain(d => d.DonorId == "1");
        }
    }
}
