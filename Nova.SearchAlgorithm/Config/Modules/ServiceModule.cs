using System.Configuration;
using System.Reflection;
using Autofac;
using Autofac.Integration.WebApi;
using Microsoft.ApplicationInsights;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.Data;
using Nova.SearchAlgorithm.Data.Persistent;
using Nova.SearchAlgorithm.Data.Persistent.Repositories;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Data;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames;
using Nova.SearchAlgorithm.MatchingDictionary.Services.DataGeneration.AlleleNames;
using Nova.SearchAlgorithm.MatchingDictionary.Services.HlaDataConversion;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Services.DonorImport;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.SearchAlgorithm.Services.Scoring;
using Nova.SearchAlgorithm.Services.Scoring.Confidence;
using Nova.SearchAlgorithm.Services.Scoring.Grading;
using Nova.SearchAlgorithm.Services.Scoring.Ranking;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Auth;
using Nova.Utils.WebApi.ApplicationInsights;
using Nova.Utils.WebApi.Filters;
using Module = Autofac.Module;

namespace Nova.SearchAlgorithm.Config.Modules
{
    public class ServiceModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            RegisterSearchAlgorithmTypes(builder);
            RegisterMatchingDictionaryTypes(builder);
            RegisterDataServices(builder);
        }

        public static void RegisterSearchAlgorithmTypes(ContainerBuilder builder)
        {
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
            builder.RegisterInstance(AutomapperConfig.CreateMapper()).SingleInstance().AsImplementedInterfaces();
            
            var sqlLogger = new RequestAwareLogger(new TelemetryClient(), ConfigurationManager.AppSettings["insights.logLevel"].ToLogLevel());
            builder.RegisterInstance(sqlLogger).AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<DonorScoringService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Services.DonorService>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<SearchService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<DonorImportService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HlaUpdateService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<AntigenCachingService>().AsImplementedInterfaces().InstancePerLifetimeScope();

            // Matching Services
            builder.RegisterType<DonorMatchingService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<DatabaseDonorMatchingService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<DonorMatchCalculator>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchFilteringService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchCriteriaAnalyser>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<DatabaseFilteringAnalyser>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ExpandHlaPhenotypeService>().AsImplementedInterfaces().InstancePerLifetimeScope();

            // Scoring Services
            builder.RegisterType<DonorScoringService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<GradingService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ConfidenceService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ConfidenceCalculator>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<RankingService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MatchScoreCalculator>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ScoringRequestService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<PermissiveMismatchCalculator>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<AlleleStringSplitterService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HlaCategorisationService>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<AppSettingsApiKeyProvider>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ApiKeyRequiredAttribute>().AsSelf().SingleInstance();

            var logger = new RequestAwareLogger(new TelemetryClient(), ConfigurationManager.AppSettings["insights.logLevel"].ToLogLevel());
            builder.RegisterInstance(logger).AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<MatchingDictionaryService>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<WmdaHlaVersionProvider>().AsImplementedInterfaces()
                .WithParameter("hlaDatabaseVersion", Configuration.HlaDatabaseVersion)
                .InstancePerLifetimeScope();
        }

        public static void RegisterDataServices(ContainerBuilder builder)
        {
            builder.RegisterType<SearchAlgorithmContext>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<DonorSearchRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<DonorImportRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<DonorInspectionRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<PGroupRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            
            // Persistent storage
            builder.RegisterType<SearchAlgorithmPersistentContext>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<ScoringWeightingRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<DataRefreshHistoryRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<TransientSqlConnectionStringProvider>().AsImplementedInterfaces().InstancePerLifetimeScope();
        }

        public static void RegisterMatchingDictionaryTypes(ContainerBuilder builder)
        {
            builder.RegisterType<CloudTableFactory>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TableReferenceRepository>().AsImplementedInterfaces().SingleInstance();
            
            builder.RegisterType<WmdaFileDownloader>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<HlaMatchingLookupRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HlaScoringLookupRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<AlleleNamesLookupRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Dpb1TceGroupsLookupRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<WmdaDataRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<AlleleNameHistoriesConsolidator>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<AlleleNamesFromHistoriesExtractor>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<AlleleNameVariantsExtractor>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ReservedAlleleNamesExtractor>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<AlleleNamesService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HlaMatchPreCalculationService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Dpb1TceGroupsService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HlaMatchingDataConverter>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HlaScoringDataConverter>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<RecreateHlaLookupResultsService>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<AlleleNamesLookupService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HlaLookupResultsService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<LocusHlaMatchingLookupService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HlaMatchingLookupService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HlaScoringLookupService>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Dpb1TceGroupLookupService>().AsImplementedInterfaces().InstancePerLifetimeScope();
        }
    }
}