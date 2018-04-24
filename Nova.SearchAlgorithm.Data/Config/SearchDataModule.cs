using System.Configuration;
using Autofac;
using Microsoft.ApplicationInsights;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.WebApi.ApplicationInsights;

namespace Nova.SearchAlgorithm.Data.Config
{
    public class SearchDataModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            //var logger = new RequestAwareLogger(new TelemetryClient(), ConfigurationManager.AppSettings["insights.logLevel"].ToLogLevel());
            //builder.RegisterInstance(logger).AsImplementedInterfaces().SingleInstance();
            //builder.RegisterType<SearchAlgorithmContext>().AsSelf().InstancePerLifetimeScope();

            //builder.RegisterType<SqlDonorMatchRepository>()
            //    .AsImplementedInterfaces()
            //    .InstancePerLifetimeScope();
        }
    }
}
