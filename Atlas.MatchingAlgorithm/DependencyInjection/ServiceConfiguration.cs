using System;
using System.ComponentModel;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Notifications;
using Atlas.Common.NovaHttpClient.Client;
using Atlas.Common.ServiceBus.BatchReceiving;
using Atlas.HlaMetadataDictionary.Data;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using Atlas.HlaMetadataDictionary.Repositories.LookupRepositories;
using Atlas.HlaMetadataDictionary.Services;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.AlleleNames;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval.HlaDataConversion;
using Atlas.MatchingAlgorithm.ApplicationInsights.SearchRequests;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Clients.AzureManagement;
using Atlas.MatchingAlgorithm.Clients.AzureStorage;
using Atlas.MatchingAlgorithm.Clients.Http.DonorService;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Config;
using Atlas.MatchingAlgorithm.Data.Persistent.Context;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Models;
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
using Atlas.MultipleAlleleCodeDictionary;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

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

            services.AddScoped<IAlleleStringSplitterService, AlleleStringSplitterService>();
            services.AddScoped<IHlaCategorisationService, HlaCategorisationService>();

            services.AddScoped<IActiveHlaVersionAccessor, ActiveHlaVersionAccessor>();

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

        public static void RegisterDonorClient(this IServiceCollection services)
        {
            services.AddSingleton(GetDonorServiceClient);
        }

        private static IDonorServiceClient GetDonorServiceClient(IServiceProvider sp)
        { 
            var donorServiceSettings = sp.GetService<IOptions<DonorServiceSettings>>().Value;
            var readDonorsFromFile = donorServiceSettings.ReadDonorsFromFile ?? false;
            var apiKeyProvided = !string.IsNullOrWhiteSpace(donorServiceSettings.ApiKey);

            if(readDonorsFromFile)
            {
                return GetFileBasedDonorServiceClient(sp);
            }

            if(apiKeyProvided)
            {
                return GetRemoteDonorServiceClient(sp);
            }

            throw new InvalidOperationException(
                $"Unable to create a functional {nameof(IDonorServiceClient)} as {nameof(readDonorsFromFile)} was set to false, but no ApiKey was provided for the DonorService.");
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
            var insightsSettings = sp.GetService<IOptions<ApplicationInsightsSettings>>().Value;
            var logger = LoggerRegistration.BuildNovaLogger(insightsSettings.InstrumentationKey);

            return new DonorServiceClient(clientSettings, logger);
        }

        private static IDonorServiceClient GetFileBasedDonorServiceClient(IServiceProvider sp)
        {
            var insightsSettings = sp.GetService<IOptions<ApplicationInsightsSettings>>().Value;
            var logger = LoggerRegistration.BuildNovaLogger(insightsSettings.InstrumentationKey);

            return new FileBasedDonorServiceClient(logger);
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
            services.ManuallyRegisterSettings<HlaServiceSettings>("Client:HlaService");
            services.ManuallyRegisterSettings<DonorManagementSettings>("MessagingServiceBus:DonorManagement");
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
                var stringValue = config.GetSection($"{configPrefix}:{property.Name}")?.Value;
                var converterForPropertyType = TypeDescriptor.GetConverter(property.PropertyType);
                var typedValue = converterForPropertyType.ConvertFrom(stringValue);
                property.SetValue(settings, typedValue);
            }

            return settings;
        }
    }
}