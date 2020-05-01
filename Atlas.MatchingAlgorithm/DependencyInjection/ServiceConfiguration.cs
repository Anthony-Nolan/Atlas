using LazyCache;
using LazyCache.Providers;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Atlas.MatchingAlgorithm.Models;
using Atlas.HLAService.Client;
using Atlas.Utils.Hla.Services;
using Atlas.Utils.Notifications;
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
using Atlas.MatchingAlgorithm.ConfigSettings;
using Nova.Utils.ApplicationInsights;
using Atlas.Utils.ServiceBus.BatchReceiving;
using System;

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
            var clientSettings = new HttpClientSettings
            {
                ApiKey = hlaServiceSettings.ApiKey,
                BaseUrl = hlaServiceSettings.BaseUrl,
                ClientName = "hla_service_client",
                JsonSettings = new JsonSerializerSettings()
            };
            var logger = BuildNovaLogger(sp);

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
            var overridePathProvided = !string.IsNullOrWhiteSpace(donorServiceSettings.OverrideFilePath);
            var apiKeyProvided = !string.IsNullOrWhiteSpace(donorServiceSettings.ApiKey);

            if (overridePathProvided && apiKeyProvided)
            {
                throw new InvalidOperationException("Both an ApiKey AND an Override File were provided for the DonorService. Please choose one or the other way to create an " + nameof(IDonorServiceClient));
            }
            else if(overridePathProvided)
            {
                return GetFileBasedDonorServiceClient(sp);
            }
            else if(apiKeyProvided)
            {
                return GetRemoteDonorServiceClient(sp);
            }
            else
            {
                throw new InvalidOperationException("Neither an ApiKey, nor an Override File were provided, for the DonorService. Unable to create a functional " + nameof(IDonorServiceClient));
            }
        }

        private static IDonorServiceClient GetRemoteDonorServiceClient(IServiceProvider sp)
        {
            var donorServiceSettings = sp.GetService<IOptions<DonorServiceSettings>>().Value;
            var clientSettings = new HttpClientSettings
            {
                ApiKey = donorServiceSettings.ApiKey,
                BaseUrl = donorServiceSettings.BaseUrl,
                ClientName = "donor_service_algorithm_client",
                JsonSettings = new JsonSerializerSettings()
            };
            var logger = BuildNovaLogger(sp);

            return new DonorServiceClient(clientSettings, logger);
        }

        private static IDonorServiceClient GetFileBasedDonorServiceClient(IServiceProvider sp)
        {
            var donorServiceSettings = sp.GetService<IOptions<DonorServiceSettings>>().Value;
            var logger = BuildNovaLogger(sp);

            return new FileBasedDonorServiceClient(donorServiceSettings.OverrideFilePath, logger);
        }

        private static Logger BuildNovaLogger(IServiceProvider sp)
        {
            var insightsSettings = sp.GetService<IOptions<ApplicationInsightsSettings>>().Value;

            var telemetryConfig = new TelemetryConfiguration
            {
                InstrumentationKey = insightsSettings.InstrumentationKey
            };
            var logger = new Logger(new TelemetryClient(telemetryConfig), LogLevel.Info);
            return logger;
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
        
        public static void RegisterSettingsForDonorManagementFunctionsApp(this IServiceCollection services)
        {
            services.ManuallyRegisterSettings<ApplicationInsightsSettings>("ApplicationInsights");
            services.ManuallyRegisterSettings<AzureStorageSettings>("AzureStorage");
            services.ManuallyRegisterSettings<MessagingServiceBusSettings>("MessagingServiceBus");
            services.ManuallyRegisterSettings<HlaServiceSettings>("Client.HlaService");
            services.ManuallyRegisterSettings<DonorManagementSettings>("MessagingServiceBus.DonorManagement");
            services.ManuallyRegisterSettings<NotificationsServiceBusSettings>("NotificationsServiceBus");
        }
        
        public static void RegisterSettingsForFunctionsApp(this IServiceCollection services)
        {
            services.ManuallyRegisterSettings<ApplicationInsightsSettings>("ApplicationInsights");
            services.ManuallyRegisterSettings<AzureStorageSettings>("AzureStorage");
            services.ManuallyRegisterSettings<DonorServiceSettings>("Client:DonorService");
            services.ManuallyRegisterSettings<HlaServiceSettings>("Client:HlaService");
            services.ManuallyRegisterSettings<WmdaSettings>("Wmda");
            services.ManuallyRegisterSettings<MessagingServiceBusSettings>("MessagingServiceBus");
            services.ManuallyRegisterSettings<AzureAuthenticationSettings>("AzureManagement:Authentication");
            services.ManuallyRegisterSettings<AzureAppServiceManagementSettings>("AzureManagement:AppService");
            services.ManuallyRegisterSettings<AzureDatabaseManagementSettings>("AzureManagement:Database");
            services.ManuallyRegisterSettings<DataRefreshSettings>("DataRefresh");
            services.ManuallyRegisterSettings<NotificationsServiceBusSettings>("NotificationsServiceBus");
        }
        
        /// <summary>
        /// The common search algorithm project relies on the same app settings regardless of whether it is called by the azure function, or the web api.
        /// Both frameworks use different configuration patterns:
        /// - ASP.NET Core uses the Options pattern with nested settings in appsettings.json
        /// - Functions v2 uses a flat collections of string app settings in the "Values" object of local.settings.json
        ///
        /// This method explicitly sets up the IOptions classes that would be set up by "services.Configure".
        /// All further DI can assume these IOptions are present in either scenario.
        ///
        /// This method has been moved from the functions app to the shared app to ensure the version of the package that provides IConfiguration
        /// is the same for both this set up, and any other DI set up involving IConfiguration (e.g. database connection strings) 
        /// </summary>
        private static void ManuallyRegisterSettings<TSettings>(this IServiceCollection services, string configPrefix = "") where TSettings : class, new()
        {
            services.AddSingleton<IOptions<TSettings>>(sp =>
            {
                var config = sp.GetService<IConfiguration>();
                return new OptionsWrapper<TSettings>(BuildSettings<TSettings>(config, configPrefix));
            });
        }

        private static TSettings BuildSettings<TSettings>(IConfiguration config, string configPrefix) where TSettings : class, new()
        {
            var settings = new TSettings();

            var properties = typeof(TSettings).GetProperties();
            foreach (var property in properties)
            {
                var value = config.GetSection($"{configPrefix}:{property.Name}")?.Value;
                property.SetValue(settings, value);
            }

            return settings;
        }

        // This method is currently unused, but is expected to be the replacement pattern for setting up IOptions dependencies in functions v3. 
        private static void RegisterOptions<T>(this IServiceCollection services, string sectionName) where T : class
        {
            services.AddOptions<T>().Configure<IConfiguration>((settings, config) => { config.GetSection(sectionName).Bind(settings); });
        }
    }
}