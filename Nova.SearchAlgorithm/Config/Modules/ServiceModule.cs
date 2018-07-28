using Autofac;
using Autofac.Integration.WebApi;
using Microsoft.ApplicationInsights;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Data;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.SearchAlgorithm.Services.Scoring;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Auth;
using Nova.Utils.WebApi.ApplicationInsights;
using Nova.Utils.WebApi.Filters;
using System.Configuration;
using System.Reflection;
using Nova.SearchAlgorithm.Services.Scoring.Confidence;
using Nova.SearchAlgorithm.Services.Scoring.Grading;
using Nova.SearchAlgorithm.Services.Scoring.Ranking;
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

            var sqlLogger = new RequestAwareLogger(new TelemetryClient(), ConfigurationManager.AppSettings["insights.logLevel"].ToLogLevel());
            builder.RegisterInstance(sqlLogger).AsImplementedInterfaces().SingleInstance();
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
            
            // Scoring Services
            builder.RegisterType<DonorScoringService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<GradingService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ConfidenceService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ConfidenceCalculator>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<RankingService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchScoreCalculator>().AsImplementedInterfaces().InstancePerLifetimeScope();
            
            builder.RegisterType<HLAService.Client.Services.AlleleStringSplitterService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HLAService.Client.Services.HlaCategorisationService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            
            builder.RegisterType<AppSettingsApiKeyProvider>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ApiKeyRequiredAttribute>().AsSelf().SingleInstance();

            builder.RegisterType<CloudTableFactory>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TableReferenceRepository>().AsImplementedInterfaces().SingleInstance();

            var logger = new RequestAwareLogger(new TelemetryClient(), ConfigurationManager.AppSettings["insights.logLevel"].ToLogLevel());
            builder.RegisterInstance(logger).AsImplementedInterfaces().SingleInstance();

            RegisterMatchingDictionaryTypes(builder);
        }

        private static void RegisterMatchingDictionaryTypes(ContainerBuilder builder)
        {
            builder.RegisterType<MatchingDictionary.Data.WmdaFileDownloader>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<MatchingDictionary.Repositories.HlaMatchingLookupRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Repositories.HlaScoringLookupRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Repositories.AlleleNamesLookupRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Repositories.WmdaDataRepository>()
                .AsImplementedInterfaces()
                .WithParameter("hlaDatabaseVersion", Configuration.HlaDatabaseVersion)
                .InstancePerLifetimeScope();

            builder.RegisterType<MatchingDictionary.Services.HlaMatchPreCalculationService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Services.ManageMatchingDictionaryService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Services.HlaMatchingLookupService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Services.HlaScoringLookupService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Services.AlleleNamesService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Services.AlleleNamesLookupService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Services.HlaDataConversion.HlaMatchingDataConverter>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Services.HlaDataConversion.HlaScoringDataConverter>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<MatchingDictionary.Services.AlleleNames.AlleleNameHistoriesConsolidator>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Services.AlleleNames.AlleleNamesFromHistoriesExtractor>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Services.AlleleNames.AlleleNameVariantsExtractor>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchingDictionary.Services.AlleleNames.ReservedAlleleNamesExtractor>().AsImplementedInterfaces().InstancePerLifetimeScope();
        }
    }
}