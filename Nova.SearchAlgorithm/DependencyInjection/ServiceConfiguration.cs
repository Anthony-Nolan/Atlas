using LazyCache;
using LazyCache.Providers;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.Clients.AzureManagement;
using Nova.SearchAlgorithm.Clients.AzureStorage;
using Nova.SearchAlgorithm.Clients.Http;
using Nova.SearchAlgorithm.Clients.ServiceBus;
using Nova.SearchAlgorithm.Config;
using Nova.SearchAlgorithm.Data.Persistent;
using Nova.SearchAlgorithm.Data.Persistent.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Data;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames;
using Nova.SearchAlgorithm.MatchingDictionary.Services.DataGeneration.AlleleNames;
using Nova.SearchAlgorithm.MatchingDictionary.Services.HlaDataConversion;
using Nova.SearchAlgorithm.Services.AzureManagement;
using Nova.SearchAlgorithm.Services.ConfigurationProviders;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Nova.SearchAlgorithm.Services.DataRefresh;
using Nova.SearchAlgorithm.Services.DonorManagement;
using Nova.SearchAlgorithm.Services.Donors;
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
using Nova.Utils.Notifications;
using Nova.Utils.ServiceBus.BatchReceiving;
using System;
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
            services.Configure<DataRefreshSettings>(configuration.GetSection("DataRefresh"));
            services.Configure<NotificationsServiceBusSettings>(configuration.GetSection("NotificationsServiceBus"));
            services.Configure<DonorManagementSettings>(configuration.GetSection("MessagingServiceBus.DonorManagement"));
        }

        public static void RegisterSearchAlgorithmTypes(this IServiceCollection services)
        {
            services.AddScoped(sp => new ConnectionStrings
            {
                Persistent = sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["PersistentSql"],
                TransientA = sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["SqlA"],
                TransientB = sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["SqlB"],
            });

            services.AddSingleton<IMemoryCache, MemoryCache>(sp => new MemoryCache(new MemoryCacheOptions()));

            services.AddSingleton(sp => AutomapperConfig.CreateMapper());

            services.AddScoped<IThreadSleeper, ThreadSleeper>();
            services.AddSingleton<ILogger>(sp =>
                new Logger(new TelemetryClient(), sp.GetService<IOptions<ApplicationInsightsSettings>>().Value.LogLevel.ToLogLevel())
            );
            services.AddTransient<IAppCache, CachingService>(sp =>
                new CachingService(new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions())))
            );

            services.AddScoped<ActiveTransientSqlConnectionStringProvider>();
            services.AddScoped<DormantTransientSqlConnectionStringProvider>();
            services.AddScoped<IActiveDatabaseProvider, ActiveDatabaseProvider>();
            services.AddScoped<IAzureDatabaseNameProvider, AzureDatabaseNameProvider>();

            services.AddScoped<IDonorScoringService, DonorScoringService>();
            services.AddScoped<IDonorService, Services.Donors.DonorService>();

            services.AddScoped<ISearchService, SearchService>();
            services.AddScoped<IDonorImporter, DonorImporter>();
            services.AddScoped<IHlaProcessor, HlaProcessor>();
            services.AddScoped<IDataRefreshOrchestrator, DataRefreshOrchestrator>();
            services.AddScoped<IDataRefreshService, DataRefreshService>();
            services.AddScoped<INotificationSender, NotificationSender>();
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

            services.AddScoped<IWmdaHlaVersionProvider, WmdaHlaVersionProvider>();

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

            services.AddScoped<INotificationsClient, NotificationsClient>(sp =>
            {
                var settings = sp.GetService<IOptions<NotificationsServiceBusSettings>>().Value;
                return new NotificationsClient(settings.ConnectionString, settings.NotificationsTopic, settings.AlertsTopic);
            });
        }

        public static void RegisterDataServices(this IServiceCollection services)
        {
            services.AddScoped<IActiveRepositoryFactory, ActiveRepositoryFactory>();
            services.AddScoped<IDormantRepositoryFactory, DormantRepositoryFactory>();
            // Persistent storage
            services.AddScoped(sp =>
            {
                var persistentConnectionString = sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["PersistentSql"];
                return new ContextFactory().Create(persistentConnectionString);
            });
            services.AddScoped<IScoringWeightingRepository, ScoringWeightingRepository>();
            services.AddScoped<IDataRefreshHistoryRepository, DataRefreshHistoryRepository>();
        }
        
        public static void RegisterAllMatchingDictionaryTypes(this IServiceCollection services)
        {
            RegisterMatchingDictionaryLookupStorageTypes(services);
            RegisterMatchingDictionaryPreCalculationTypes(services);
            RegisterMatchingDictionaryLookupServices(services);
        }

        public static void RegisterTypesNeededForMatchingDictionaryLookups(this IServiceCollection services)
        {
            RegisterMatchingDictionaryLookupStorageTypes(services);
            RegisterMatchingDictionaryLookupServices(services);
        }

        private static void RegisterMatchingDictionaryLookupStorageTypes(this IServiceCollection services)
        {
            services.AddSingleton<ICloudTableFactory, CloudTableFactory>(sp =>
                new CloudTableFactory(sp.GetService<IOptions<AzureStorageSettings>>().Value.ConnectionString)
            );

            services.AddSingleton<ITableReferenceRepository, TableReferenceRepository>();

            services.AddScoped<IHlaMatchingLookupRepository, HlaMatchingLookupRepository>();
            services.AddScoped<IHlaScoringLookupRepository, HlaScoringLookupRepository>();
            services.AddScoped<IAlleleNamesLookupRepository, AlleleNamesLookupRepository>();
            services.AddScoped<IDpb1TceGroupsLookupRepository, Dpb1TceGroupsLookupRepository>();
        }

        private static void RegisterMatchingDictionaryPreCalculationTypes(this IServiceCollection services)
        {
            services.AddScoped<IWmdaDataRepository, WmdaDataRepository>();

            services.AddScoped<IWmdaFileReader, WmdaFileDownloader>(sp =>
                new WmdaFileDownloader(sp.GetService<IOptions<WmdaSettings>>().Value.WmdaFileUri)
            );

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
        }

        private static void RegisterMatchingDictionaryLookupServices(this IServiceCollection services)
        {
            services.AddScoped<IAlleleNamesLookupService, AlleleNamesLookupService>();
            services.AddScoped<IHlaLookupResultsService, HlaLookupResultsService>();
            services.AddScoped<ILocusHlaMatchingLookupService, LocusHlaMatchingLookupService>();
            services.AddScoped<IHlaMatchingLookupService, HlaMatchingLookupService>();
            services.AddScoped<IHlaScoringLookupService, HlaScoringLookupService>();
            services.AddScoped<IDpb1TceGroupLookupService, Dpb1TceGroupLookupService>();
        }

        public static void RegisterClients(this IServiceCollection services)
        {
            RegisterHlaServiceClient(services);
            services.AddScoped(GetDonorServiceClient);
        }

        public static void RegisterHlaServiceClient(this IServiceCollection services)
        {
            services.AddScoped(GetHlaServiceClient);
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

        public static void RegisterDonorManagementServices(this IServiceCollection services)
        {
            services.AddScoped<IDonorManagementService, DonorManagementService>();

            services.AddSingleton<IMessageReceiverFactory, MessageReceiverFactory>(sp =>
                new MessageReceiverFactory(sp.GetService<IOptions<MessagingServiceBusSettings>>().Value.ConnectionString)
            );

            services.AddScoped<IMessageProcessor<SearchableDonorUpdateModel>, MessageProcessor<SearchableDonorUpdateModel>>(sp =>
            {
                var settings = sp.GetService<IOptions<DonorManagementSettings>>().Value;
                var factory = sp.GetService<IMessageReceiverFactory>();
                return new MessageProcessor<SearchableDonorUpdateModel>(factory, settings.Topic, settings.Subscription);
            });

            services.AddScoped<IDonorUpdateProcessor, DonorUpdateProcessor>(sp =>
            {
                var settings = sp.GetService<IOptions<DonorManagementSettings>>().Value;
                var messageReceiverService = sp.GetService<IMessageProcessor<SearchableDonorUpdateModel>>();
                var managementService = sp.GetService<IDonorManagementService>();
                var logger = sp.GetService<ILogger>();
                return new DonorUpdateProcessor(messageReceiverService, managementService, logger, int.Parse(settings.BatchSize));
            });
        }
    }
}