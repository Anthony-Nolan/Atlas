using System.Configuration;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Config;
using Nova.SearchAlgorithm.Data.Persistent;
using Nova.SearchAlgorithm.Data.Persistent.Repositories;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Data.Services;
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

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests
{
    public class ServiceModule
    {
        public static void RegisterSearchAlgorithmTypes(ServiceCollection services)
        {
            services.AddSingleton(sp => AutomapperConfig.CreateMapper());

            services.AddSingleton<ILogger>(sp =>
                new Logger(new TelemetryClient(), sp.GetService<IConfiguration>().GetSection("AppConfig")["Insights.LogLevel"].ToLogLevel())
            );

            services.AddScoped<IDonorScoringService, DonorScoringService>();
            services.AddScoped<IDonorService, Services.DonorService>();

            services.AddScoped<ISearchService, SearchService>();
            services.AddScoped<IDonorImportService, DonorImportService>();
            services.AddScoped<IHlaUpdateService, HlaUpdateService>();
            services.AddScoped<IAntigenCachingService, AntigenCachingService>();

            // Matching Services
            services.AddScoped<IDonorMatchingService, DonorMatchingService>();
            services.AddScoped<IDatabaseDonorMatchingService, DatabaseDonorMatchingService>();
            services.AddScoped<IDonorMatchCalculator, DonorMatchCalculator>();
            services.AddScoped<IMatchFilteringService, MatchFilteringService>();
            services.AddScoped<IMatchCriteriaAnalyser, MatchCriteriaAnalyser>();
            services.AddScoped<IDatabaseFilteringAnalyser, DatabaseFilteringAnalyser>();
            services.AddScoped<IExpandHlaPhenotypeService, ExpandHlaPhenotypeService>();

            // Scoring Services
            services.AddScoped<IDonorScoringService, DonorScoringService>();
            services.AddScoped<IGradingService, GradingService>();
            services.AddScoped<IConfidenceService, ConfidenceService>();
            services.AddScoped<IConfidenceCalculator, ConfidenceCalculator>();
            services.AddScoped<IRankingService, RankingService>();
            services.AddScoped<IMatchScoreCalculator, MatchScoreCalculator>();
            services.AddScoped<IScoringRequestService, ScoringRequestService>();
            services.AddScoped<IPermissiveMismatchCalculator, PermissiveMismatchCalculator>();

            services.AddScoped<IAlleleStringSplitterService, AlleleStringSplitterService>();
            services.AddScoped<IHlaCategorisationService, HlaCategorisationService>();

            services.AddSingleton<IApiKeyProvider, AppSettingsApiKeyProvider>();

            services.AddScoped<IMatchingDictionaryService, MatchingDictionaryService>();

            services.AddScoped<IWmdaHlaVersionProvider, WmdaHlaVersionProvider>(sp =>
                new WmdaHlaVersionProvider(ConfigurationManager.AppSettings["HlaDatabaseVersion"])
            );
        }

        public static void RegisterDataServices(ServiceCollection services)
        {
            services.AddScoped(sp =>
                new Data.Context.ContextFactory().Create(sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["SqlA"])
            );
            services.AddScoped<IDonorSearchRepository, DonorSearchRepository>();
            services.AddScoped<IDonorImportRepository, DonorImportRepository>();
            services.AddScoped<IDonorInspectionRepository, DonorInspectionRepository>();
            services.AddScoped<IPGroupRepository, PGroupRepository>();

            // Persistent storage
            services.AddScoped(sp =>
                new Data.Persistent.ContextFactory().Create(sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["PersistentSql"])
            );
            services.AddScoped<IScoringWeightingRepository, ScoringWeightingRepository>();
            services.AddScoped<IDataRefreshHistoryRepository, DataRefreshHistoryRepository>();

            services.AddScoped<IConnectionStringProvider, TransientSqlConnectionStringProvider>(sp =>
                new TransientSqlConnectionStringProvider(
                    sp.GetService<IDataRefreshHistoryRepository>(),
                    sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["SqlA"],
                    sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["SqlB"]
                )
            );
        }

        public static void RegisterMatchingDictionaryTypes(ServiceCollection services)
        {
            services.AddSingleton<ICloudTableFactory, CloudTableFactory>();
            services.AddSingleton<ITableReferenceRepository, TableReferenceRepository>();

            services.AddScoped<IWmdaFileReader, WmdaFileDownloader>();

            services.AddScoped<IHlaMatchingLookupRepository, HlaMatchingLookupRepository>();
            services.AddScoped<IHlaScoringLookupRepository, HlaScoringLookupRepository>();
            services.AddScoped<IAlleleNamesLookupRepository, AlleleNamesLookupRepository>();
            services.AddScoped<IDpb1TceGroupsLookupRepository, Dpb1TceGroupsLookupRepository>();
            services.AddScoped<IWmdaDataRepository, WmdaDataRepository>();

            services.AddScoped<IAlleleNameHistoriesConsolidator, AlleleNameHistoriesConsolidator>();
            services.AddScoped<IAlleleNamesFromHistoriesExtractor, AlleleNamesFromHistoriesExtractor>();
            services.AddScoped<IAlleleNameVariantsExtractor, AlleleNameVariantsExtractor>();
            services.AddScoped<IReservedAlleleNamesExtractor, ReservedAlleleNamesExtractor>();

            services.AddScoped<IAlleleNamesService, AlleleNamesService>();
            services.AddScoped<IHlaMatchPreCalculationService, HlaMatchPreCalculationService>();
            services.AddScoped<IDpb1TceGroupsService, Dpb1TceGroupsService>();
            services.AddScoped<IHlaMatchingDataConverter, HlaMatchingDataConverter>();
            services.AddScoped<IHlaScoringDataConverter, HlaScoringDataConverter>();

            services.AddScoped<IRecreateHlaLookupResultsService, RecreateHlaLookupResultsService>();

            services.AddScoped<IAlleleNamesLookupService, AlleleNamesLookupService>();
            services.AddScoped<IHlaLookupResultsService, HlaLookupResultsService>();
            services.AddScoped<ILocusHlaMatchingLookupService, LocusHlaMatchingLookupService>();
            services.AddScoped<IHlaMatchingLookupService, HlaMatchingLookupService>();
            services.AddScoped<IHlaScoringLookupService, HlaScoringLookupService>();
            services.AddScoped<IDpb1TceGroupLookupService, Dpb1TceGroupLookupService>();
        }
    }
}