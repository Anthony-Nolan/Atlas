using System.Configuration;
using System.Reflection;
using Autofac;
using Autofac.Integration.WebApi;
using Microsoft.ApplicationInsights;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Auth;
using Nova.Utils.Solar;
using Nova.Utils.WebApi.ApplicationInsights;
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

            builder.RegisterType<Repositories.SolarDonorRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<Repositories.Donors.DonorCloudTables>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Repositories.Donors.CloudStorageDonorSearchRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<Services.SearchService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.TestDataService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.DonorImportService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.HlaUpdateService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            
            builder.RegisterType<AppSettingsApiKeyProvider>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ApiKeyRequiredAttribute>().AsSelf().SingleInstance();

            builder.RegisterType<Repositories.CloudTableFactory>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SolarConnectionFactory>().AsImplementedInterfaces().SingleInstance();

            var solarSettings = new SolarConnectionSettings
            {
                ConnectionString = ConfigurationManager.ConnectionStrings["SolarConnectionString"].ConnectionString
            };
            builder.RegisterInstance(solarSettings).AsSelf().SingleInstance();

            var logger = new RequestAwareLogger(new TelemetryClient(),
                ConfigurationManager.AppSettings["insights.logLevel"].ToLogLevel());
            builder.RegisterInstance(logger).AsImplementedInterfaces().SingleInstance();

            RegisterMatchingDictionaryTypes(builder);
        }

        private static void RegisterMatchingDictionaryTypes(ContainerBuilder builder)
        {
            builder.RegisterType<MatchingDictionary.Data.WmdaFileDownloader>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<MatchingDictionary.Repositories.MatchingDictionaryRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Repositories.WmdaDataRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Repositories.AzureStorage.CloudTableFactory>().AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<MatchingDictionary.Services.HlaMatchingService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Services.ManageMatchingDictionaryService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Services.MatchingDictionaryLookupService>().AsImplementedInterfaces().InstancePerLifetimeScope();
        }
    }
}