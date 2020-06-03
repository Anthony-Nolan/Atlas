using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Notifications;
using Atlas.Common.ServiceBus.BatchReceiving;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.ExternalInterface.DependencyInjection;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.ApplicationInsights.SearchRequests;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Clients.AzureManagement;
using Atlas.MatchingAlgorithm.Clients.AzureStorage;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Config;
using Atlas.MatchingAlgorithm.Data.Persistent.Context;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Services.Search.Matching;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Aggregation;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Confidence;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Ranking;
using Atlas.MatchingAlgorithm.Services.Utility;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Atlas.MatchingAlgorithm.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterMatchingAlgorithm(this IServiceCollection services)
        {
            services.RegisterSettingsForMatchingAlgorithm();
            services.RegisterMatchingAlgorithmServices();
            services.RegisterDataServices();
            services.RegisterHlaMetadataDictionary(
                sp => sp.GetService<IOptions<AzureStorageSettings>>().Value.ConnectionString,
                sp => sp.GetService<IOptions<WmdaSettings>>().Value.WmdaFileUri,
                sp => sp.GetService<IOptions<HlaServiceSettings>>().Value.ApiKey,
                sp => sp.GetService<IOptions<HlaServiceSettings>>().Value.BaseUrl,
                sp => sp.GetService<IOptions<ApplicationInsightsSettings>>().Value
            );
            
            // TODO: ATLAS-327: Inject settings
            services.RegisterDonorReader(sp => sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["DonorImportSql"]);
        }

        public static void RegisterMatchingAlgorithmDonorManagement(this IServiceCollection services)
        {
            services.RegisterSettingsForMatchingDonorManagement();
            services.RegisterMatchingAlgorithmServices();
            services.RegisterDataServices();
            services.RegisterDonorManagementServices();
            services.RegisterHlaMetadataDictionary(
                sp => sp.GetService<IOptions<AzureStorageSettings>>().Value.ConnectionString,
                sp => sp.GetService<IOptions<WmdaSettings>>().Value.WmdaFileUri,
                sp => sp.GetService<IOptions<HlaServiceSettings>>().Value.ApiKey,
                sp => sp.GetService<IOptions<HlaServiceSettings>>().Value.BaseUrl,
                sp => sp.GetService<IOptions<ApplicationInsightsSettings>>().Value
            );
        }

        private static void RegisterMatchingAlgorithmServices(this IServiceCollection services)
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
            services.AddApplicationInsightsTelemetryWorkerService();
            services.AddScoped<ILogger>(sp => new SearchRequestAwareLogger(
                sp.GetService<ISearchRequestContext>(),
                sp.GetService<TelemetryClient>(),
                sp.GetService<IOptions<ApplicationInsightsSettings>>().Value.LogLevel.ToLogLevel()));

            services.RegisterLifeTimeScopedCacheTypes();

            services.AddScoped<ActiveTransientSqlConnectionStringProvider>();
            services.AddScoped<DormantTransientSqlConnectionStringProvider>();
            services.AddScoped<IActiveDatabaseProvider, ActiveDatabaseProvider>();
            services.AddScoped<IAzureDatabaseNameProvider, AzureDatabaseNameProvider>();

            services.AddScoped<IDonorScoringService, DonorScoringService>();
            services.AddScoped<IDonorService, DonorService>();
            services.AddScoped<IDonorHlaExpanderFactory, DonorHlaExpanderFactory>();

            services.AddScoped<ISearchService, SearchService>();
            services.AddScoped<IFailedDonorsNotificationSender, FailedDonorsNotificationSender>();
            services.AddScoped<IDonorInfoConverter, DonorInfoConverter>();
            services.AddScoped<IDonorImporter, DonorImporter>();
            services.AddScoped<IHlaProcessor, HlaProcessor>();
            services.AddScoped<IDataRefreshOrchestrator, DataRefreshOrchestrator>();
            services.AddScoped<IDataRefreshService, DataRefreshService>();
            services.AddScoped<IDataRefreshNotificationSender, DataRefreshNotificationSender>();
            services.AddScoped<IDataRefreshCleanupService, DataRefreshCleanupService>();

            // Matching Services
            services.AddScoped<IDonorMatchingService, DonorMatchingService>();
            services.AddScoped<IDatabaseDonorMatchingService, DatabaseDonorMatchingService>();
            services.AddScoped<IDonorMatchCalculator, DonorMatchCalculator>();
            services.AddScoped<IMatchFilteringService, MatchFilteringService>();
            services.AddScoped<IMatchCriteriaAnalyser, MatchCriteriaAnalyser>();
            services.AddScoped<IDatabaseFilteringAnalyser, DatabaseFilteringAnalyser>();

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

            services.RegisterCommonGeneticServices();

            services.AddScoped<IActiveHlaNomenclatureVersionAccessor, ActiveHlaNomenclatureVersionAccessor>();

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
            services.AddScoped<ISearchRunner, SearchRunner>();
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

            services.AddScoped<INotificationsClient, NotificationsClient>();

            services.AddScoped<IScoringCache, ScoringCache>();
        }

        private static void RegisterDataServices(this IServiceCollection services)
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

        private static void RegisterDonorManagementServices(this IServiceCollection services)
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

        private static void RegisterSharedSettings(this IServiceCollection services)
        {
            services.RegisterOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterOptions<AzureStorageSettings>("AzureStorage");
            services.RegisterOptions<MessagingServiceBusSettings>("MessagingServiceBus");
            services.RegisterOptions<HlaServiceSettings>("Client:HlaService");
            services.RegisterOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");
        }

        private static void RegisterSettingsForMatchingDonorManagement(this IServiceCollection services)
        {
            services.RegisterSharedSettings();
            services.RegisterOptions<DonorManagementSettings>("MessagingServiceBus:DonorManagement");
        }

        private static void RegisterSettingsForMatchingAlgorithm(this IServiceCollection services)
        {
            services.RegisterSharedSettings();
            services.RegisterOptions<WmdaSettings>("Wmda");
            services.RegisterOptions<AzureAuthenticationSettings>("AzureManagement:Authentication");
            services.RegisterOptions<AzureAppServiceManagementSettings>("AzureManagement:AppService");
            services.RegisterOptions<AzureDatabaseManagementSettings>("AzureManagement:Database");
            services.RegisterOptions<DataRefreshSettings>("DataRefresh");
        }
    }
}