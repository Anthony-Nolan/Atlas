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

            builder.RegisterType<Repositories.SearchRequests.SearchRequestRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Repositories.Donors.DonorCloudTables>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Repositories.Hla.HlaRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Repositories.SolarDonorRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            // builder.RegisterType<Repositories.Donors.BlobDonorMatchRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<Services.SearchRequestService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.SearchService>().AsImplementedInterfaces().InstancePerLifetimeScope();
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
            builder.RegisterType<MatchingDictionary.Repositories.MatchedHlaRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Repositories.AzureStorage.CloudTableFactory>().AsImplementedInterfaces().SingleInstance();

            builder.RegisterInstance(MatchingDictionary.Repositories.WmdaRepository.Instance).AsImplementedInterfaces().ExternallyOwned();

            builder.RegisterType<MatchingDictionary.Services.Dictionary.ManageDictionaryService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Services.Dictionary.DictionaryLookupService>().AsImplementedInterfaces().InstancePerLifetimeScope();
        }
    }
}