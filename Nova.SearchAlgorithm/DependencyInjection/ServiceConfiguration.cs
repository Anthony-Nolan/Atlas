using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.Clients;
using Nova.SearchAlgorithm.Clients.AzureManagement;
using Nova.SearchAlgorithm.Clients.AzureStorage;
using Nova.SearchAlgorithm.Clients.Http;
using Nova.SearchAlgorithm.Clients.ServiceBus;
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
using Nova.SearchAlgorithm.Services.AzureManagement;
using Nova.SearchAlgorithm.Services.ConfigurationProviders;
using Nova.SearchAlgorithm.Services.DonorImport;
using Nova.SearchAlgorithm.Services.DonorImport.PreProcessing;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.SearchAlgorithm.Services.MatchingDictionary;
using Nova.SearchAlgorithm.Services.Scoring;
using Nova.SearchAlgorithm.Services.Scoring.Confidence;
using Nova.SearchAlgorithm.Services.Scoring.Grading;
using Nova.SearchAlgorithm.Services.Scoring.Ranking;
using Nova.SearchAlgorithm.Services.Search;
using Nova.SearchAlgorithm.Services.Utility;
using Nova.SearchAlgorithm.Settings;
using Nova.Utils.ApplicationInsights;
using ClientSettings = Nova.Utils.Client.ClientSettings;

namespace Nova.SearchAlgorithm.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterSettings(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();
            services.Configure<HlaServiceSettings>(configuration.GetSection("Client:HlaService"));
            services.Configure<DonorServiceSettings>(configuration.GetSection("Client:DonorService"));
            services.Configure<ApplicationInsightsSettings>(configuration.GetSection("ApplicationInsights"));
            services.Configure<AzureStorageSettings>(configuration.GetSection("AzureStorage"));
            services.Configure<WmdaSettings>(configuration.GetSection("Wmda"));
            services.Configure<MessagingServiceBusSettings>(configuration.GetSection("MessagingServiceBus"));
            services.Configure<AzureAuthenticationSettings>(configuration.GetSection("AzureManagement.Authentication"));
            services.Configure<AzureAppServiceManagementSettings>(configuration.GetSection("AzureManagement.AppService"));
            services.Configure<AzureDatabaseManagementSettings>(configuration.GetSection("AzureManagement.Database"));
        }

        public static void RegisterSearchAlgorithmTypes(this IServiceCollection services)
        {
            services.AddSingleton<IMemoryCache, MemoryCache>(sp => new MemoryCache(new MemoryCacheOptions()));

            services.AddSingleton(sp => AutomapperConfig.CreateMapper());

            services.AddScoped<IThreadSleeper, ThreadSleeper>();
            services.AddSingleton<ILogger>(sp =>
                new Logger(new TelemetryClient(), sp.GetService<IOptions<ApplicationInsightsSettings>>().Value.LogLevel.ToLogLevel())
            );

            services.AddScoped<IWmdaLatestVersionFetcher, WmdaLatestVersionFetcher>();

            services.AddScoped<IDonorScoringService, DonorScoringService>();
            services.AddScoped<IDonorService, Services.DonorImport.DonorService>();
            services.AddScoped<IDonorManagementService, DonorManagementService>();

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

            services.AddScoped<IMatchingDictionaryService, MatchingDictionaryService>();

            services.AddScoped<IWmdaHlaVersionProvider, WmdaHlaVersionProvider>(sp =>
                new WmdaHlaVersionProvider(sp.GetService<IOptions<WmdaSettings>>().Value.HlaDatabaseVersion)
            );

            services.AddScoped<ISearchServiceBusClient, SearchServiceBusClient>(sp =>
            {
                var serviceBusSettings = sp.GetService<IOptions<MessagingServiceBusSettings>>().Value;
                return new SearchServiceBusClient(
                    serviceBusSettings.ConnectionString,
                    serviceBusSettings.SearchRequestsQueue,
                    serviceBusSettings.SearchResultsTopic
                );
            });
            services.AddScoped<ISearchDispatcher, SearchDispatcher>();
            services.AddScoped<IResultsBlobStorageClient, ResultsBlobStorageClient>(sp =>
            {
                var azureStorageSettings = sp.GetService<IOptions<AzureStorageSettings>>().Value;
                var logger = sp.GetService<ILogger>();
                return new ResultsBlobStorageClient(azureStorageSettings.ConnectionString, logger, azureStorageSettings.SearchResultsBlobContainer);
            });

            services.AddScoped<IAzureDatabaseManagementClient, AzureDatabaseManagementClient>();
            services.AddScoped<IAzureAppServiceManagementClient, AzureAppServiceManagementClient>();
            services.AddScoped<IAzureAuthenticationClient, AzureAuthenticationClient>();
            services.AddScoped<IAzureFunctionManager, AzureFunctionManager>();
            services.AddScoped<IAzureDatabaseManager, AzureDatabaseManager>();
        }

        public static void RegisterDataServices(this IServiceCollection services)
        {
            services.AddScoped<IDonorSearchRepository, DonorSearchRepository>();
            services.AddScoped<IDonorImportRepository, DonorImportRepository>();
            services.AddScoped<IDonorInspectionRepository, DonorInspectionRepository>();
            services.AddScoped<IPGroupRepository, PGroupRepository>();

            // Persistent storage
            services.AddScoped(sp =>
                new ContextFactory().Create(sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["PersistentSql"])
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

        public static void RegisterMatchingDictionaryTypes(this IServiceCollection services)
        {
            services.AddSingleton<ICloudTableFactory, CloudTableFactory>(sp =>
                new CloudTableFactory(sp.GetService<IOptions<AzureStorageSettings>>().Value.ConnectionString)
            );
            services.AddSingleton<ITableReferenceRepository, TableReferenceRepository>();

            services.AddScoped<IWmdaFileReader, WmdaFileDownloader>(sp =>
                new WmdaFileDownloader(sp.GetService<IOptions<WmdaSettings>>().Value.WmdaFileUri)
            );

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

        public static void RegisterClients(this IServiceCollection services)
        {
            services.AddScoped(GetHlaServiceClient);
            services.AddScoped(GetDonorServiceClient);
        }

        private static IHlaServiceClient GetHlaServiceClient(IServiceProvider sp)
        {
            var hlaServiceSettings = sp.GetService<IOptions<HlaServiceSettings>>().Value;
            var insightsSettings = sp.GetService<IOptions<ApplicationInsightsSettings>>().Value;
            var clientSettings = new ClientSettings
            {
                ApiKey = hlaServiceSettings.ApiKey,
                BaseUrl = hlaServiceSettings.BaseUrl,
                ClientName = "hla_service_client",
                JsonSettings = new JsonSerializerSettings()
            };
            var telemetryConfig = new TelemetryConfiguration
            {
                InstrumentationKey = insightsSettings.InstrumentationKey
            };
            var logger = new Logger(new TelemetryClient(telemetryConfig), LogLevel.Info);

            return new HlaServiceClient(clientSettings, logger);
        }

        private static IDonorServiceClient GetDonorServiceClient(IServiceProvider sp)
        {
            var donorServiceSettings = sp.GetService<IOptions<DonorServiceSettings>>().Value;
            var insightsSettings = sp.GetService<IOptions<ApplicationInsightsSettings>>().Value;
            var clientSettings = new ClientSettings
            {
                ApiKey = donorServiceSettings.ApiKey,
                BaseUrl = donorServiceSettings.BaseUrl,
                ClientName = "donor_service_algorithm_client",
                JsonSettings = new JsonSerializerSettings()
            };
            var telemetryConfig = new TelemetryConfiguration
            {
                InstrumentationKey = insightsSettings.InstrumentationKey
            };
            var logger = new Logger(new TelemetryClient(telemetryConfig), LogLevel.Info);

            return new DonorServiceClient(clientSettings, logger);
        }
    }
}