using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Caching;
using Atlas.Common.Debugging;
using Atlas.Common.FeatureManagement;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Matching.Services;
using Atlas.Common.Notifications;
using Atlas.Common.ServiceBus;
using Atlas.Common.ServiceBus.BatchReceiving;
using Atlas.Common.ServiceBus.DependencyInjection;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.DependencyInjection;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Services.DonorUpdates;
using Atlas.HlaMetadataDictionary.ExternalInterface.DependencyInjection;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Clients.AzureManagement;
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
using Atlas.MatchingAlgorithm.Services.DataRefresh.DonorImport;
using Atlas.MatchingAlgorithm.Services.DataRefresh.HlaProcessing;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
using Atlas.MatchingAlgorithm.Services.Debug;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Services.Search.Matching;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Aggregation;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.AntigenMatching;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Confidence;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Ranking;
using Atlas.MatchingAlgorithm.Services.Utility;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using System;
using Atlas.SearchTracking.Common.Clients;
using Atlas.SearchTracking.Settings.ServiceBus;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.MatchingAlgorithm.DependencyInjection
{
    public static class ServiceConfiguration
    {
        /// <summary>
        /// Register everything needed to perform ongoing donor management of the matching algorithm's data store.
        /// </summary>
        public static void RegisterDonorManagement(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings,
            Func<IServiceProvider, DonorManagementSettings> fetchDonorManagementSettings,
            Func<IServiceProvider, HlaMetadataDictionarySettings> fetchHlaMetadataDictionarySettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacDictionarySettings,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, NotificationsServiceBusSettings> fetchNotificationsServiceBusSettings,
            Func<IServiceProvider, string> fetchPersistentSqlConnectionString,
            Func<IServiceProvider, string> fetchTransientASqlConnectionString,
            Func<IServiceProvider, string> fetchTransientBSqlConnectionString,
            Func<IServiceProvider, string> fetchDonorImportSqlConnectionString
        )
        {
            services.RegisterCommonServices(
                fetchApplicationInsightsSettings,
                fetchAzureStorageSettings,
                fetchMessagingServiceBusSettings,
                fetchNotificationsServiceBusSettings,
                fetchPersistentSqlConnectionString,
                fetchTransientASqlConnectionString,
                fetchTransientBSqlConnectionString
            );

            services.RegisterCommonImportServices();

            services.RegisterHlaMetadataDictionary(fetchHlaMetadataDictionarySettings, fetchApplicationInsightsSettings, fetchMacDictionarySettings);

            services.RegisterDonorManagementServices(fetchDonorManagementSettings, fetchMessagingServiceBusSettings, fetchDonorImportSqlConnectionString);
        }

        /// <summary>
        /// Register everything needed to perform a full data refresh of the matching algorithm's data store.
        /// </summary>
        public static void RegisterDataRefresh(
            this IServiceCollection services,
            Func<IServiceProvider, AzureAuthenticationSettings> fetchAzureAuthenticationSettings,
            Func<IServiceProvider, AzureDatabaseManagementSettings> fetchAzureDatabaseManagementSettings,
            Func<IServiceProvider, DataRefreshSettings> fetchDataRefreshSettings,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings,
            Func<IServiceProvider, HlaMetadataDictionarySettings> fetchHlaMetadataDictionarySettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacDictionarySettings,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, NotificationsServiceBusSettings> fetchNotificationsServiceBusSettings,
            Func<IServiceProvider, DonorManagementSettings> fetchDonorManagementSettings,
            Func<IServiceProvider, string> fetchPersistentSqlConnectionString,
            Func<IServiceProvider, string> fetchTransientASqlConnectionString,
            Func<IServiceProvider, string> fetchTransientBSqlConnectionString,
            Func<IServiceProvider, string> fetchDonorImportSqlConnectionString)
        {
            services.RegisterCommonServices(
                fetchApplicationInsightsSettings,
                fetchAzureStorageSettings,
                fetchMessagingServiceBusSettings,
                fetchNotificationsServiceBusSettings,
                fetchPersistentSqlConnectionString,
                fetchTransientASqlConnectionString,
                fetchTransientBSqlConnectionString
            );

            services.RegisterCommonImportServices();

            services.RegisterHlaMetadataDictionary(fetchHlaMetadataDictionarySettings, fetchApplicationInsightsSettings, fetchMacDictionarySettings);

            services.RegisterDonorReader(fetchDonorImportSqlConnectionString);

            services.RegisterDataRefreshServices(
                fetchAzureAuthenticationSettings,
                fetchAzureDatabaseManagementSettings,
                fetchDataRefreshSettings,
                fetchDonorManagementSettings,
                fetchMessagingServiceBusSettings,
                fetchDonorImportSqlConnectionString
            );
        }

        public static void RegisterDebugServices(
            this IServiceCollection services,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings,
            Func<IServiceProvider, AzureAuthenticationSettings> fetchAzureAuthenticationSettings
            )
        {
            var serviceKey = typeof(MessagingServiceBusSettings);
            services.RegisterServiceBusAsKeyedServices(
                key: serviceKey,
                sp => fetchMessagingServiceBusSettings(sp).ConnectionString
                );

            services.AddScoped<IServiceBusPeeker<MatchingResultsNotification>, MatchingResultNotificationsPeeker>(sp =>
            {
                var settings = fetchMessagingServiceBusSettings(sp);
                return new MatchingResultNotificationsPeeker(
                    sp.GetRequiredKeyedService<IMessageReceiverFactory>(serviceKey),
                    settings.SearchResultsTopic,
                    settings.SearchResultsDebugSubscription);
            });

            services.RegisterDebugLogger(fetchApplicationInsightsSettings);
            services.AddScoped<IBlobDownloader, BlobDownloader>(sp =>
            {
                var settings = fetchAzureStorageSettings(sp);
                return new BlobDownloader(settings.ConnectionString, sp.GetService<IDebugLogger>());
            });
            services.AddScoped<IDebugResultsDownloader, DebugResultsDownloader>();

            // Register azure App.Insights API client and configure EntraId's Client Secret authentication
            // Another azure clients can be added here and they will share authentication configuration
            services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.UseCredential(sp =>
                {
                    var authSetting = fetchAzureAuthenticationSettings(sp);

                    return new ClientSecretCredential(
                        tenantId: authSetting.TenantId,
                        clientId: authSetting.ClientId,
                        clientSecret: authSetting.ClientSecret);
                });

                clientBuilder.AddLogsQueryClient();
            });

            services.AddTransient<IHlaExpansionFailuresService, HlaExpansionFailuresService>();
        }

        /// <summary>
        /// Register everything needed to perform searches.
        /// </summary>
        public static void RegisterSearch(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings,
            Func<IServiceProvider, HlaMetadataDictionarySettings> fetchHlaMetadataDictionarySettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacDictionarySettings,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, SearchTrackingServiceBusSettings> fetchSearchTrackingServiceBusSettings,
            Func<IServiceProvider, NotificationsServiceBusSettings> fetchNotificationsServiceBusSettings,
            Func<IServiceProvider, MatchingConfigurationSettings> fetchMatchingConfigurationSettings,
            Func<IServiceProvider, string> fetchPersistentSqlConnectionString,
            Func<IServiceProvider, string> fetchTransientASqlConnectionString,
            Func<IServiceProvider, string> fetchTransientBSqlConnectionString,
            Func<IServiceProvider, string> fetchDonorImportSqlConnectionString)
        {
            services.RegisterCommonServices(
                fetchApplicationInsightsSettings,
                fetchAzureStorageSettings,
                fetchMessagingServiceBusSettings,
                fetchNotificationsServiceBusSettings,
                fetchPersistentSqlConnectionString,
                fetchTransientASqlConnectionString,
                fetchTransientBSqlConnectionString
            );

            services.RegisterDonorReader(fetchDonorImportSqlConnectionString);

            services.RegisterHlaMetadataDictionary(fetchHlaMetadataDictionarySettings, fetchApplicationInsightsSettings, fetchMacDictionarySettings);

            services.RegisterSearchServices(fetchMatchingConfigurationSettings);

            services.MakeSettingsAvailableForUse(fetchSearchTrackingServiceBusSettings);
        }

        /// <summary>
        /// Register everything needed to perform searches for Matching Algorithm.
        /// </summary>
        public static void RegisterSearchForMatchingAlgorithm(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings,
            Func<IServiceProvider, HlaMetadataDictionarySettings> fetchHlaMetadataDictionarySettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacDictionarySettings,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, SearchTrackingServiceBusSettings> fetchSearchTrackingServiceBusSettings,
            Func<IServiceProvider, NotificationsServiceBusSettings> fetchNotificationsServiceBusSettings,
            Func<IServiceProvider, MatchingConfigurationSettings> fetchMatchingConfigurationSettings,
            Func<IServiceProvider, string> fetchPersistentSqlConnectionString,
            Func<IServiceProvider, string> fetchTransientASqlConnectionString,
            Func<IServiceProvider, string> fetchTransientBSqlConnectionString,
            Func<IServiceProvider, string> fetchDonorImportSqlConnectionString)
        {
            services.RegisterSearch(
                fetchApplicationInsightsSettings,
                fetchAzureStorageSettings,
                fetchHlaMetadataDictionarySettings,
                fetchMacDictionarySettings,
                fetchMessagingServiceBusSettings,
                fetchSearchTrackingServiceBusSettings,
                fetchNotificationsServiceBusSettings,
                fetchMatchingConfigurationSettings,
                fetchPersistentSqlConnectionString,
                fetchTransientASqlConnectionString,
                fetchTransientBSqlConnectionString,
                fetchDonorImportSqlConnectionString
            );

            services.RegisterMatchingAlgorithmSpecificServices(fetchAzureStorageSettings);
        }


        /// <summary>
        /// Register services only needed for Data Refresh, and not for matching or donor management
        /// </summary>
        private static void RegisterDataRefreshServices(
            this IServiceCollection services,
            Func<IServiceProvider, AzureAuthenticationSettings> fetchAzureAuthenticationSettings,
            Func<IServiceProvider, AzureDatabaseManagementSettings> fetchAzureDatabaseManagementSettings,
            Func<IServiceProvider, DataRefreshSettings> fetchDataRefreshSettings,
            Func<IServiceProvider, DonorManagementSettings> fetchDonorManagementSettings,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, string> fetchDonorImportSqlConnectionString)
        {
            // The last step of the Data Refresh is applying a backlog of donor updates - so it needs to know how to perform queued donor updates as well.
            services.RegisterDonorManagementServices(fetchDonorManagementSettings, fetchMessagingServiceBusSettings, fetchDonorImportSqlConnectionString);

            services.RegisterSettingsForDataRefresh(
                fetchAzureAuthenticationSettings,
                fetchAzureDatabaseManagementSettings,
                fetchDataRefreshSettings,
                fetchMessagingServiceBusSettings
            );

            // Azure interaction services
            services.AddScoped<IThreadSleeper, ThreadSleeper>();
            services.AddScoped<IAzureDatabaseManagementClient, AzureDatabaseManagementClient>();
            services.AddScoped<IAzureAuthenticationClient, AzureAuthenticationClient>();
            services.AddScoped<IAzureDatabaseManager, AzureDatabaseManager>();
            services.AddScoped<IAzureDatabaseNameProvider, AzureDatabaseNameProvider>();

            // Data Refresh services
            services.AddScoped<IDataRefreshRequester, DataRefreshRequester>();
            services.AddScoped<IDataRefreshOrchestrator, DataRefreshOrchestrator>();
            services.AddScoped<IDataRefreshRunner, DataRefreshRunner>();
            services.AddScoped<IDataRefreshSupportNotificationSender, DataRefreshSupportNotificationSender>();
            services.AddScoped<IDataRefreshCompletionNotifier, DataRefreshCompletionNotifier>();
            services.AddScoped<IDataRefreshCleanupService, DataRefreshCleanupService>();
            services.AddScoped<IDataRefreshServiceBusClient, DataRefreshServiceBusClient>();

            services.AddScoped<IHlaProcessor, HlaProcessor>();
            services.AddScoped<IDonorImporter, DonorImporter>();
        }

        /// <summary>
        /// Register services only needed for ongoing donor management
        /// </summary>
        private static void RegisterDonorManagementServices(
            this IServiceCollection services,
            Func<IServiceProvider, DonorManagementSettings> fetchDonorManagementSettings,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, string> fetchDonorImportSqlConnectionString)
        {
            var serviceKey = typeof(MessagingServiceBusSettings);
            services.RegisterDonorImportServices(fetchDonorImportSqlConnectionString);

            services.AddScoped<IDonorManagementService, DonorManagementService>();
            services.AddScoped<ISearchableDonorUpdateConverter, SearchableDonorUpdateConverter>();

            services.RegisterServiceBusAsKeyedServices(
                serviceKey,
                connectionStringAccessor: sp => fetchMessagingServiceBusSettings(sp).ConnectionString
                );

            services.AddScoped<IMessageProcessorForDbADonorUpdates, DonorUpdateMessageProcessor>(sp =>
            {
                var messagingSettings = fetchMessagingServiceBusSettings(sp);
                var donorManagementSettings = fetchDonorManagementSettings(sp);
                var factory = sp.GetRequiredKeyedService<IMessageReceiverFactory>(serviceKey);
                var messageReceiver = new ServiceBusMessageReceiver<SearchableDonorUpdate>(
                    factory, donorManagementSettings.Topic, donorManagementSettings.SubscriptionForDbA, donorManagementSettings.BatchSize * 2);
                return new DonorUpdateMessageProcessor(messageReceiver);
            });

            services.AddScoped<IMessageProcessorForDbBDonorUpdates, DonorUpdateMessageProcessor>(sp =>
            {
                var messagingSettings = fetchMessagingServiceBusSettings(sp);
                var donorManagementSettings = fetchDonorManagementSettings(sp);
                var factory = sp.GetRequiredKeyedService<IMessageReceiverFactory>(serviceKey);
                var messageReceiver = new ServiceBusMessageReceiver<SearchableDonorUpdate>(
                    factory, donorManagementSettings.Topic, donorManagementSettings.SubscriptionForDbB, donorManagementSettings.BatchSize * 2);
                return new DonorUpdateMessageProcessor(messageReceiver);
            });

            services.AddScoped<IDonorUpdateProcessor, DonorUpdateProcessor>(sp =>
            {
                var messageReceiverServiceForDbA = sp.GetService<IMessageProcessorForDbADonorUpdates>();
                var messageReceiverServiceForDbB = sp.GetService<IMessageProcessorForDbBDonorUpdates>();
                var refreshHistory = sp.GetService<IDataRefreshHistoryRepository>();
                var managementService = sp.GetService<IDonorManagementService>();
                var updateConverter = sp.GetService<ISearchableDonorUpdateConverter>();
                var hlaVersionAccessor = sp.GetService<IActiveHlaNomenclatureVersionAccessor>();
                var logger = sp.GetService<IMatchingAlgorithmImportLogger>();
                var loggingContext = sp.GetService<MatchingAlgorithmImportLoggingContext>();
                var settings = fetchDonorManagementSettings(sp);
                var donorReader = sp.GetService<IDonorReader>();
                var donorUpdatesSaver = sp.GetService<IDonorUpdatesSaver>();

                return new DonorUpdateProcessor(
                    messageReceiverServiceForDbA,
                    messageReceiverServiceForDbB,
                    refreshHistory,
                    managementService,
                    updateConverter,
                    hlaVersionAccessor,
                    settings,
                    logger,
                    loggingContext,
                    donorReader,
                    donorUpdatesSaver);
            });
        }

        /// <summary>
        /// Register services only needed for running searches
        /// </summary>
        private static void RegisterSearchServices(
            this IServiceCollection services,
            Func<IServiceProvider, MatchingConfigurationSettings> fetchMatchingConfigurationSettings)
        {
            services.RegisterSettingsForMatching(
                fetchMatchingConfigurationSettings
            );

            services.AddScoped<ISearchService, SearchService>();
            services.AddScoped<IMatchCriteriaMapper, MatchCriteriaMapper>();
            services.AddScoped<IMatchingFailureNotificationSender, MatchingFailureNotificationSender>();

            services.AddApplicationInsightsTelemetryWorkerService();
            services.AddScoped<MatchingAlgorithmSearchLoggingContext>();
            services.AddScoped<IMatchingAlgorithmSearchLogger, MatchingAlgorithmSearchLogger>();
            services.AddScoped<MatchingAlgorithmImportLoggingContext>();
            services.AddScoped<IMatchingAlgorithmImportLogger, MatchingAlgorithmImportLogger>();

            // Matching Services
            services.AddScoped<IMatchingService, MatchingService>();
            services.AddScoped<IDonorMatchingService, DonorMatchingService>();
            services.AddScoped<IPerLocusDonorMatchingService, PerLocusDonorMatchingService>();
            services.AddScoped<IMatchFilteringService, MatchFilteringService>();
            services.AddScoped<IMatchCriteriaAnalyser, MatchCriteriaAnalyser>();

            // Scoring Services
            services.AddScoped<IMatchScoringService, MatchScoringService>();
            services.AddScoped<IDonorScoringService, DonorScoringService>();
            services.AddScoped<IGradingService, GradingService>();
            services.AddScoped<IConfidenceService, ConfidenceService>();
            services.AddScoped<IConfidenceCalculator, ConfidenceCalculator>();
            services.AddScoped<IAntigenMatchingService, AntigenMatchingService>();
            services.AddScoped<IAntigenMatchCalculator, AntigenMatchCalculator>();
            services.AddScoped<IRankingService, RankingService>();
            services.AddScoped<IMatchScoreCalculator, MatchScoreCalculator>();
            services.AddScoped<IScoringRequestService, ScoringRequestService>();
            services.AddScoped<IScoreResultAggregator, ScoreResultAggregator>();
            services.AddScoped<IScoringCache, ScoringCache>();
            services.AddScoped<IDpb1TceGroupMatchCalculator, Dpb1TceGroupMatchCalculator>();

            // Also used for dispatching searches, registered independently in ProjectInterfaceOrchestrationConfiguration.cs
            services.AddScoped<ISearchServiceBusClient, SearchServiceBusClient>();
            services.AddScoped<ISearchTrackingServiceBusClient, SearchTrackingServiceBusClient>();
            services.AddScoped<ISearchDispatcher, SearchDispatcher>();
            services.AddScoped<ISearchRunner, SearchRunner>();

            // Repositories
            services.AddScoped<IScoringWeightingRepository, ScoringWeightingRepository>();

            services.RegisterFeatureManager();

        }

        /// <summary>
        /// Feature management, leave it configured even if there is no active feature flags in use
        /// </summary>
        /// <param name="services">Services collection</param>
        private static void RegisterFeatureManager(this IServiceCollection services)
        {
            services.AddAzureAppConfiguration();
            services.AddFeatureManagement();

            services.AddScoped<IAtlasFeatureManager, AtlasFeatureManager>();
        }

        /// <summary>
        /// Register services only needed for running searches
        /// </summary>
        private static void RegisterMatchingAlgorithmSpecificServices(
            this IServiceCollection services,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings)
        {
            services.AddScoped<ISearchResultsBlobStorageClient, SearchResultsBlobStorageClient>(sp =>
            {
                var settings = fetchAzureStorageSettings(sp);
                var logger = sp.GetService<IMatchingAlgorithmSearchLogger>();
                return new SearchResultsBlobStorageClient(settings.ConnectionString, logger);
            });
        }

        /// <summary>
        /// Register any services common to both import pathways - i.e. ongoing donor management and full data refresh
        /// </summary>
        private static void RegisterCommonImportServices(this IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetryWorkerService();
            services.AddScoped<MatchingAlgorithmImportLoggingContext>();
            services.AddScoped<IMatchingAlgorithmImportLogger, MatchingAlgorithmImportLogger>();

            services.AddScoped<IDonorService, DonorService>();
            services.AddScoped<IDonorHlaExpanderFactory, DonorHlaExpanderFactory>();

            services.AddScoped<IFailedDonorsNotificationSender, FailedDonorsNotificationSender>();
            services.AddScoped<IDonorInfoConverter, DonorInfoConverter>();
        }

        /// <summary>
        /// Register services needed in multiple usages of the matching component - across matching, data refresh, and ongoing donor management
        /// </summary>
        private static void RegisterCommonServices(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, NotificationsServiceBusSettings> fetchNotificationsServiceBusSettings,
            Func<IServiceProvider, string> fetchPersistentSqlConnectionString,
            Func<IServiceProvider, string> fetchTransientASqlConnectionString,
            Func<IServiceProvider, string> fetchTransientBSqlConnectionString
        )
        {
            services.RegisterCommonSettings(
                fetchApplicationInsightsSettings,
                fetchAzureStorageSettings,
                fetchMessagingServiceBusSettings,
                fetchNotificationsServiceBusSettings
            );

            services.AddScoped(sp => new ConnectionStrings
            {
                Persistent = fetchPersistentSqlConnectionString(sp),
                TransientA = fetchTransientASqlConnectionString(sp),
                TransientB = fetchTransientBSqlConnectionString(sp),
            });

            services.AddSingleton<IMemoryCache, MemoryCache>(sp => new MemoryCache(new MemoryCacheOptions()));

            services.AddSingleton(sp => AutoMapperConfig.CreateMapper());

            services.AddApplicationInsightsTelemetryWorkerService();

            services.RegisterLifeTimeScopedCacheTypes();

            services.AddScoped<ActiveTransientSqlConnectionStringProvider>();
            services.AddScoped<DormantTransientSqlConnectionStringProvider>();
            services.AddScoped<StaticallyChosenTransientSqlConnectionStringProviderFactory>();
            services.AddScoped<IActiveDatabaseProvider, ActiveDatabaseProvider>();

            services.RegisterCommonGeneticServices();
            services.RegisterCommonMatchingServices();

            services.AddScoped<IActiveHlaNomenclatureVersionAccessor, ActiveHlaNomenclatureVersionAccessor>();

            services.RegisterNotificationSender(
                OptionsReaderFor<NotificationsServiceBusSettings>(),
                OptionsReaderFor<ApplicationInsightsSettings>()
            );

            services.AddScoped<IActiveRepositoryFactory, ActiveRepositoryFactory>();
            services.AddScoped<IDormantRepositoryFactory, DormantRepositoryFactory>();
            services.AddScoped<IStaticallyChosenDatabaseRepositoryFactory, StaticallyChosenDatabaseRepositoryFactory>();

            // Persistent storage
            services.AddScoped(sp => new ContextFactory().Create(fetchPersistentSqlConnectionString(sp)));
            services.AddScoped<IDataRefreshHistoryRepository, DataRefreshHistoryRepository>();
        }

        private static void RegisterDonorImportServices(
            this IServiceCollection services,
            Func<IServiceProvider, string> fetchDonorImportSqlConnectionString
        )
        {
            services.RegisterDonorReader(fetchDonorImportSqlConnectionString);
            services.RegisterDonorUpdateServices(fetchDonorImportSqlConnectionString);
        }

        #region Settings

        private static void RegisterCommonSettings(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, NotificationsServiceBusSettings> fetchNotificationsServiceBusSettings)
        {
            services.MakeSettingsAvailableForUse(fetchApplicationInsightsSettings);
            services.MakeSettingsAvailableForUse(fetchAzureStorageSettings);
            services.MakeSettingsAvailableForUse(fetchMessagingServiceBusSettings);
            services.MakeSettingsAvailableForUse(fetchNotificationsServiceBusSettings);
        }

        private static void RegisterSettingsForMatching(
            this IServiceCollection services,
            Func<IServiceProvider, MatchingConfigurationSettings> fetchMatchingConfigurationSettings)
        {
            services.MakeSettingsAvailableForUse(fetchMatchingConfigurationSettings);
        }

        private static void RegisterSettingsForDataRefresh(
            this IServiceCollection services,
            Func<IServiceProvider, AzureAuthenticationSettings> fetchAzureAuthenticationSettings,
            Func<IServiceProvider, AzureDatabaseManagementSettings> fetchAzureDatabaseManagementSettings,
            Func<IServiceProvider, DataRefreshSettings> fetchDataRefreshSettings,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings)
        {
            services.MakeSettingsAvailableForUse(fetchAzureAuthenticationSettings);
            services.MakeSettingsAvailableForUse(fetchAzureDatabaseManagementSettings);
            services.MakeSettingsAvailableForUse(fetchDataRefreshSettings);
            services.MakeSettingsAvailableForUse(fetchMessagingServiceBusSettings);
        }

        #endregion
    }
}