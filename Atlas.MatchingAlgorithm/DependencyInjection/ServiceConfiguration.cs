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
using Atlas.MatchingAlgorithm.Common.Services;
using Atlas.MatchingAlgorithm.ApplicationInsights.SearchRequests;
using Atlas.MatchingAlgorithm.Clients.AzureManagement;
using Atlas.MatchingAlgorithm.Clients.AzureStorage;
using Atlas.MatchingAlgorithm.Clients.Http;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Config;
using Atlas.MatchingAlgorithm.Data.Persistent;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Helpers;
using Atlas.MatchingAlgorithm.MatchingDictionary.Caching;
using Atlas.MatchingAlgorithm.MatchingDictionary.Data;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using Atlas.MatchingAlgorithm.MatchingDictionary.Services;
using Atlas.MatchingAlgorithm.MatchingDictionary.Services.AlleleNames;
using Atlas.MatchingAlgorithm.MatchingDictionary.Services.DataGeneration.AlleleNames;
using Atlas.MatchingAlgorithm.MatchingDictionary.Services.HlaDataConversion;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Services.Matching;
using Atlas.MatchingAlgorithm.Services.MatchingDictionary;
using Atlas.MatchingAlgorithm.Services.Scoring;
using Atlas.MatchingAlgorithm.Services.Scoring.Confidence;
using Atlas.MatchingAlgorithm.Services.Scoring.Grading;
using Atlas.MatchingAlgorithm.Services.Scoring.Ranking;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Services.Search.Matching;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Aggregation;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Ranking;
using Atlas.MatchingAlgorithm.Services.Utility;
using Atlas.MatchingAlgorithm.Settings;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Notifications;
using Nova.Utils.ServiceBus.BatchReceiving;
using System;
using ClientSettings = Nova.Utils.Client.ClientSettings;

namespace Atlas.MatchingAlgorithm.DependencyInjection
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

            services.AddScoped<ISearchRequestContext, SearchRequestContext>();
            services.AddScoped<ILogger>(sp =>
                new SearchRequestAwareLogger(
                    sp.GetService<ISearchRequestContext>(),
                    new TelemetryClient(),
                    sp.GetService<IOptions<ApplicationInsightsSettings>>().Value.LogLevel.ToLogLevel())
            );

            // The default IAppCache registration should be a singleton, to avoid re-caching large collections e.g. Matching Dictionary and Alleles each request
            // Persistent has been picked as the default for ease of injection into the MatchingDictionary, which will not be able to access any wrappers defined in the core project
            services.AddSingleton<IAppCache, CachingService>(sp =>
                new CachingService(new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions())))
                {
                    DefaultCachePolicy = new CacheDefaults { DefaultCacheDurationSeconds = 86400 }
                }
            );

            // A wrapper for IAppCache to allow classes to depend explicitly on a transient-only cache.
            // This should be used for non-heavyweight cached items, e.g. hla database version.
            services.AddTransient<ITransientCacheProvider, TransientCacheProvider>(sp =>
                new TransientCacheProvider(new CachingService(new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))))
            );

            services.AddScoped<ActiveTransientSqlConnectionStringProvider>();
            services.AddScoped<DormantTransientSqlConnectionStringProvider>();
            services.AddScoped<IActiveDatabaseProvider, ActiveDatabaseProvider>();
            services.AddScoped<IAzureDatabaseNameProvider, AzureDatabaseNameProvider>();

            services.AddScoped<IDonorScoringService, DonorScoringService>();
            services.AddScoped<IDonorService, Services.Donors.DonorService>();
            services.AddScoped<IDonorHlaExpander, DonorHlaExpander>();

            services.AddScoped<ISearchService, SearchService>();
            services.AddScoped<IFailedDonorsNotificationSender, FailedDonorsNotificationSender>();
            services.AddScoped<IDonorInfoConverter, DonorInfoConverter>();
            services.AddScoped<IDonorImporter, DonorImporter>();
            services.AddScoped<IHlaProcessor, HlaProcessor>();
            services.AddScoped<IDataRefreshOrchestrator, DataRefreshOrchestrator>();
            services.AddScoped<IDataRefreshService, DataRefreshService>();
            services.AddScoped<IDataRefreshNotificationSender, DataRefreshNotificationSender>();
            services.AddScoped<IDataRefreshCleanupService, DataRefreshCleanupService>();
            services.AddScoped<IAntigenCachingService, NmdpCodeCachingService>();

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
            services.AddScoped<IScoreResultAggregator, ScoreResultAggregator>();

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

            services.AddScoped<IScoringCache, ScoringCache>();
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
            services.AddScoped<INmdpCodeCache, NmdpCodeCachingService>();
            services.AddScoped<IAlleleNamesLookupService, AlleleNamesLookupService>();
            services.AddScoped<IHlaLookupResultsService, HlaLookupResultsService>();
            services.AddScoped<ILocusHlaMatchingLookupService, LocusHlaMatchingLookupService>();
            services.AddScoped<IHlaMatchingLookupService, HlaMatchingLookupService>();
            services.AddScoped<IHlaScoringLookupService, HlaScoringLookupService>();
            services.AddScoped<IDpb1TceGroupLookupService, Dpb1TceGroupLookupService>();
        }

        public static void RegisterNovaClients(this IServiceCollection services)
        {
            RegisterHlaServiceClient(services);
            services.AddSingleton(GetDonorServiceClient);
        }

        public static void RegisterHlaServiceClient(this IServiceCollection services)
        {
            services.AddSingleton(GetHlaServiceClient);
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

            try
            {
                return new HlaServiceClient(clientSettings, logger);
            }
            // When running on startup, the client setup will often throw a NullReferenceException.
            // This appears to go away when running not immediately after startup, so we retry once to circumvent
            catch (NullReferenceException)
            {
                return new HlaServiceClient(clientSettings, logger);
            }
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
            services.AddScoped<ISearchableDonorUpdateConverter, SearchableDonorUpdateConverter>();

            services.AddSingleton<IMessageReceiverFactory, MessageReceiverFactory>(sp =>
                new MessageReceiverFactory(sp.GetService<IOptions<MessagingServiceBusSettings>>().Value.ConnectionString)
            );

            services.AddScoped<IServiceBusMessageReceiver<SearchableDonorUpdate>, ServiceBusMessageReceiver<SearchableDonorUpdate>>(sp =>
            {
                var settings = sp.GetService<IOptions<DonorManagementSettings>>().Value;
                var factory = sp.GetService<IMessageReceiverFactory>();
                return new ServiceBusMessageReceiver<SearchableDonorUpdate>(factory, settings.Topic, settings.Subscription);
            });

            services.AddScoped<IMessageProcessor<SearchableDonorUpdate>, MessageProcessor<SearchableDonorUpdate>>(sp =>
            {
                var messageReceiver = sp.GetService<IServiceBusMessageReceiver<SearchableDonorUpdate>>();
                return new MessageProcessor<SearchableDonorUpdate>(messageReceiver);
            });

            services.AddScoped<IDonorUpdateProcessor, DonorUpdateProcessor>(sp =>
            {
                var messageReceiverService = sp.GetService<IMessageProcessor<SearchableDonorUpdate>>();
                var managementService = sp.GetService<IDonorManagementService>();
                var updateConverter = sp.GetService<ISearchableDonorUpdateConverter>();
                var logger = sp.GetService<ILogger>();
                var settings = sp.GetService<IOptions<DonorManagementSettings>>().Value;
                return new DonorUpdateProcessor(
                    messageReceiverService,
                    managementService,
                    updateConverter,
                    logger,
                    int.Parse(settings.BatchSize));
            });
        }
    }
}