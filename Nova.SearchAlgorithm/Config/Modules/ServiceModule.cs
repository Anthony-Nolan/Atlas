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
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Data;
using Nova.SearchAlgorithm.Data.Repositories;
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

            // TODO:NOVA-1151 remove any dependency on Solar
            builder.RegisterType<Repositories.SolarDonorRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();

            if (ConfigurationManager.AppSettings["backend.implementation"] == "table")
            {
                builder.RegisterType<Repositories.Donors.AzureStorage.CloudTableStorage>().AsImplementedInterfaces().InstancePerLifetimeScope();
                builder.RegisterType<Repositories.Donors.CloudStorageDonorSearchRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            }
            else if (ConfigurationManager.AppSettings["backend.implementation"] == "cosmos")
            {
                builder.RegisterType<Repositories.Donors.CosmosStorage.CosmosStorage>().AsImplementedInterfaces().InstancePerLifetimeScope();
                builder.RegisterType<Repositories.Donors.CloudStorageDonorSearchRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            }
            else if (ConfigurationManager.AppSettings["backend.implementation"] == "sql")
            {
                var sqlLogger = new RequestAwareLogger(new TelemetryClient(), ConfigurationManager.AppSettings["insights.logLevel"].ToLogLevel());
                builder.RegisterInstance(sqlLogger).AsImplementedInterfaces().SingleInstance();
                builder.RegisterType<SearchAlgorithmContext>().AsSelf().InstancePerLifetimeScope();

                builder.RegisterType<SqlDonorSearchRepository>()
                    .AsImplementedInterfaces()
                    .InstancePerLifetimeScope();
            }

            builder.RegisterType<Scoring.CalculateScore>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<Services.SearchService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.TestDataService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.DonorImportService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.HlaUpdateService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.AntigenCachingService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.DonorMatchingService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            
            builder.RegisterType<HLAService.Client.Services.AlleleStringSplitterService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HLAService.Client.Services.HlaCategorisationService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            
            builder.RegisterType<AppSettingsApiKeyProvider>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ApiKeyRequiredAttribute>().AsSelf().SingleInstance();

            builder.RegisterType<CloudTableFactory>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TableReferenceRepository>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SolarConnectionFactory>().AsImplementedInterfaces().SingleInstance();

            var solarSettings = new SolarConnectionSettings
            {
                ConnectionString = ConfigurationManager.ConnectionStrings["SolarConnectionString"].ConnectionString
            };
            builder.RegisterInstance(solarSettings).AsSelf().SingleInstance();

            var logger = new RequestAwareLogger(new TelemetryClient(), ConfigurationManager.AppSettings["insights.logLevel"].ToLogLevel());
            builder.RegisterInstance(logger).AsImplementedInterfaces().SingleInstance();

            RegisterMatchingDictionaryTypes(builder);
        }

        private static void RegisterMatchingDictionaryTypes(ContainerBuilder builder)
        {
            builder.RegisterType<MatchingDictionary.Data.WmdaFileDownloader>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<MatchingDictionary.Repositories.MatchingDictionaryRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Repositories.MatchingDictionaryMatchingDictionaryTableReferenceRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Repositories.WmdaDataRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<MatchingDictionary.Services.HlaMatchingService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Services.ManageMatchingDictionaryService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Services.MatchingDictionaryLookupService>().AsImplementedInterfaces().InstancePerLifetimeScope();
        }
    }
}