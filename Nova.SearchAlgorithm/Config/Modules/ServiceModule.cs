using System.Configuration;
using System.Reflection;
using Autofac;
using Autofac.Integration.WebApi;
using Nova.SearchAlgorithm.Solar.Connection;
using Nova.Utils.Auth;
using Nova.Utils.WebApi.Filters;
using Module = Autofac.Module;

namespace Nova.SearchAlgorithm.Config.Modules
{
    public class ServiceModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

            builder.RegisterInstance(AutomapperConfig.CreateMapper())
                .SingleInstance()
                .AsImplementedInterfaces();

            builder.RegisterType<Services.SearchRequestService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.ServiceStatusService>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<AppSettingsApiKeyProvider>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ApiKeyRequiredAttribute>().AsSelf().SingleInstance();

            builder.RegisterType<SolarConnectionFactory>().AsImplementedInterfaces().SingleInstance();

            var solarSettings = new SolarConnectionSettings
            {
                ConnectionString = ConfigurationManager.ConnectionStrings["SolarConnectionString"].ConnectionString
            };
            builder.RegisterInstance(solarSettings).AsSelf().SingleInstance();
        }
    }
}