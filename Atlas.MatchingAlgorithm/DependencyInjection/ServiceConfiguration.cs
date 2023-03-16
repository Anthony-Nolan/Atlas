using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Matching.Services;
using Atlas.Common.Notifications;
using Atlas.Common.ServiceBus.BatchReceiving;
using Atlas.DonorImport.ExternalInterface.DependencyInjection;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.DependencyInjection;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
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
using Atlas.MatchingAlgorithm.Services.DataRefresh.DonorImport;
using Atlas.MatchingAlgorithm.Services.DataRefresh.HlaProcessing;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Services.Search.Matching;
using Atlas.MatchingAlgorithm.Services.Search.NonHlaFiltering;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Aggregation;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Confidence;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Ranking;
using Atlas.MatchingAlgorithm.Services.Utility;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
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
            Func<IServiceProvider, string> fetchTransientBSqlConnectionString
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

            services.RegisterDonorManagementServices(fetchDonorManagementSettings, fetchMessagingServiceBusSettings);
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
                fetchMessagingServiceBusSettings
            );
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
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings)
        {
            // The last step of the Data Refresh is applying a backlog of donor updates - so it needs to know how to perform queued donor updates as well. 
            services.RegisterDonorManagementServices(fetchDonorManagementSettings, fetchMessagingServiceBusSettings);

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
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings)
        {
            services.AddScoped<IDonorManagementService, DonorManagementService>();
            services.AddScoped<ISearchableDonorUpdateConverter, SearchableDonorUpdateConverter>();

            services.AddSingleton<IMessageReceiverFactory, MessageReceiverFactory>(sp =>
                new MessageReceiverFactory(fetchMessagingServiceBusSettings(sp).ConnectionString)
            );

            services.AddScoped<IMessageProcessorForDbADonorUpdates, DonorUpdateMessageProcessor>(sp =>
            {
                var settings = fetchDonorManagementSettings(sp);
                var factory = sp.GetService<IMessageReceiverFactory>();
                var messageReceiver = new ServiceBusMessageReceiver<SearchableDonorUpdate>(factory, settings.Topic, settings.SubscriptionForDbA);
                return new DonorUpdateMessageProcessor(messageReceiver);
            });

            services.AddScoped<IMessageProcessorForDbBDonorUpdates, DonorUpdateMessageProcessor>(sp =>
            {
                var settings = fetchDonorManagementSettings(sp);
                var factory = sp.GetService<IMessageReceiverFactory>();
                var messageReceiver = new ServiceBusMessageReceiver<SearchableDonorUpdate>(factory, settings.Topic, settings.SubscriptionForDbB);
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

                return new DonorUpdateProcessor(
                    messageReceiverServiceForDbA,
                    messageReceiverServiceForDbB,
                    refreshHistory,
                    managementService,
                    updateConverter,
                    hlaVersionAccessor,
                    settings,
                    logger,
                    loggingContext);
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
            services.AddScoped<IDonorDetailsResultFilterer, DonorDetailsResultFilterer>();
            services.AddScoped<IMatchCriteriaMapper, MatchCriteriaMapper>();
            services.AddScoped<IMatchingFailureNotificationSender, MatchingFailureNotificationSender>();

            services.AddApplicationInsightsTelemetryWorkerService();
            services.AddScoped<MatchingAlgorithmSearchLoggingContext>();
            services.AddScoped<IMatchingAlgorithmSearchLogger, MatchingAlgorithmSearchLogger>();

            // Matching Services
            services.AddScoped<IMatchingService, MatchingService>();
            services.AddScoped<IDonorMatchingService, DonorMatchingService>();
            services.AddScoped<IPerLocusDonorMatchingService, PerLocusDonorMatchingService>();
            services.AddScoped<IMatchFilteringService, MatchFilteringService>();
            services.AddScoped<IMatchCriteriaAnalyser, MatchCriteriaAnalyser>();
            services.AddScoped<IDatabaseFilteringAnalyser, DatabaseFilteringAnalyser>();

            // Scoring Services
            services.AddScoped<IMatchScoringService, MatchScoringService>();
            services.AddScoped<IDonorScoringService, DonorScoringService>();
            services.AddScoped<IGradingService, GradingService>();
            services.AddScoped<IConfidenceService, ConfidenceService>();
            services.AddScoped<IConfidenceCalculator, ConfidenceCalculator>();
            services.AddScoped<IRankingService, RankingService>();
            services.AddScoped<IMatchScoreCalculator, MatchScoreCalculator>();
            services.AddScoped<IScoringRequestService, ScoringRequestService>();
            services.AddScoped<IScoreResultAggregator, ScoreResultAggregator>();
            services.AddScoped<IScoringCache, ScoringCache>();
            services.AddScoped<IDpb1TceGroupMatchCalculator, Dpb1TceGroupMatchCalculator>();

            // Also used for dispatching searches, registered independently in ProjectInterfaceOrchestrationConfiguration.cs
            services.AddScoped<ISearchServiceBusClient, SearchServiceBusClient>();
            services.AddScoped<ISearchDispatcher, SearchDispatcher>();
            services.AddScoped<ISearchRunner, SearchRunner>();
            services.AddScoped<IResultsBlobStorageClient, ResultsBlobStorageClient>();

            // Repositories
            services.AddScoped<IScoringWeightingRepository, ScoringWeightingRepository>();
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