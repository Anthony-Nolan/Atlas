using System;
using System.Collections.Generic;
using System.Configuration;
using Microsoft.ApplicationInsights;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Auth;
using Nova.Utils.Solar;
using Nova.Utils.WebApi.ApplicationInsights;
using Nova.SearchAlgorithm.Config;
using NUnit.Framework;
using Autofac;

namespace Nova.SearchAlgorithm.Test.Integration
{
    public class IntegrationTestBase
    {
        private StorageEmulator emulator = new StorageEmulator();
        protected IContainer container;

        [OneTimeSetUp]
        public void Setup()
        {
            container = CreateContainer();
            emulator.Start();
            emulator.Clear();
        }

        [OneTimeTearDown]
        public void ShutdownStorage()
        {
            emulator.Stop();
        }

        // This is almost a duplicate of the container in 
        // Nova.SearchAlgorithm.Config.Modules.ServiceModule
        private IContainer CreateContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterInstance(AutomapperConfig.CreateMapper())
                .SingleInstance()
                .AsImplementedInterfaces();

            // TODO:NOVA-1034 cleanly switch between testing different implementations
            builder.RegisterType<Repositories.Donors.BlobDonorRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Repositories.Donors.BlobDonorMatchRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();

            //builder.RegisterType<Data.SearchAlgorithmContext>().AsSelf().InstancePerLifetimeScope();
            //builder.RegisterType<Data.Repositories.SqlDonorMatchRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<Repositories.Hla.HlaRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Repositories.SolarDonorRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<Services.SearchRequestService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.SearchService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.DonorImportService>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<Repositories.CloudTableFactory>().AsImplementedInterfaces().SingleInstance();
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
}
