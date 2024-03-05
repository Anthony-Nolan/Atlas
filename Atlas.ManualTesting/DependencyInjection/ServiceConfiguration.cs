using Atlas.Client.Models.Search.Results;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Caching;
using Atlas.Common.Debugging;
using Atlas.Common.ServiceBus;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Utils.Extensions;
using Atlas.Debug.Client;
using Atlas.Debug.Client.Models.Settings;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.ManualTesting.Common.Services;
using Atlas.ManualTesting.Services;
using Atlas.ManualTesting.Services.HaplotypeFrequencySet;
using Atlas.ManualTesting.Services.Scoring;
using Atlas.ManualTesting.Services.ServiceBus;
using Atlas.ManualTesting.Services.WmdaConsensusResults;
using Atlas.ManualTesting.Services.WmdaConsensusResults.Scorers;
using Atlas.ManualTesting.Settings;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.ManualTesting.DependencyInjection
{
    internal static class ServiceConfiguration
    {
        public static void RegisterServices(this IServiceCollection services)
        {
            services.RegisterSettings();
            services.RegisterServices(
                OptionsReaderFor<MessagingServiceBusSettings>(),
                OptionsReaderFor<MatchingSettings>(),
                OptionsReaderFor<SearchSettings>(),
                OptionsReaderFor<DonorManagementSettings>(),
                OptionsReaderFor<AzureStorageSettings>()
            );
            services.RegisterDatabaseServices(ConnectionStringReader("ActiveMatchingSql"), ConnectionStringReader("DonorImportSql"));
            services.RegisterLifeTimeScopedCacheTypes();
            services.RegisterDebugClients(
                OptionsReaderFor<DonorImportHttpFunctionSettings>(),
                OptionsReaderFor<MatchingAlgorithmHttpFunctionSettings>(),
                OptionsReaderFor<TopLevelHttpFunctionSettings>(),
                OptionsReaderFor<PublicApiHttpFunctionSettings>());
        }

        private static void RegisterSettings(this IServiceCollection services)
        {
            services.RegisterAsOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
            services.RegisterAsOptions<MessagingServiceBusSettings>("MessagingServiceBus");
            services.RegisterAsOptions<MatchingSettings>("Matching");
            services.RegisterAsOptions<DonorManagementSettings>("Matching:DonorManagement");
            services.RegisterAsOptions<ScoringSettings>("Scoring");
            services.RegisterAsOptions<SearchSettings>("Search");
            services.RegisterAsOptions<AzureStorageSettings>("AzureStorage");
            services.RegisterAsOptions<DonorImportHttpFunctionSettings>("Debug:DonorImport");
            services.RegisterAsOptions<MatchingAlgorithmHttpFunctionSettings>("Debug:Matching");
            services.RegisterAsOptions<TopLevelHttpFunctionSettings>("Debug:TopLevel");
            services.RegisterAsOptions<PublicApiHttpFunctionSettings>("Debug:PublicApi");
        }

        private static void RegisterServices(
            this IServiceCollection services,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, MatchingSettings> fetchMatchingSettings,
            Func<IServiceProvider, SearchSettings> fetchSearchSettings,
            Func<IServiceProvider, DonorManagementSettings> fetchDonorManagementSettings,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings
        )
        {
            services.AddSingleton<IMessageReceiverFactory, MessageReceiverFactory>();
            services.AddSingleton<IDeadLetterReceiverFactory, DeadLetterReceiverFactory>();

            services.AddScoped(typeof(IMessagesPeeker<>), typeof(MessagesPeeker<>));
            services.AddScoped(typeof(IDeadLettersPeeker<>), typeof(DeadLettersPeeker<>));

            services.AddScoped<IDeadLettersPeeker<IdentifiedSearchRequest>, DeadLettersPeeker<IdentifiedSearchRequest>>(sp =>
            {
                var factory = sp.GetService<IDeadLetterReceiverFactory>();
                var messagingSettings = fetchMessagingServiceBusSettings(sp);
                var searchSettings = fetchMatchingSettings(sp);
                return new DeadLettersPeeker<IdentifiedSearchRequest>(
                    factory, messagingSettings.ConnectionString, searchSettings.RequestsTopic, searchSettings.RequestsSubscription);
            });
            services.AddScoped<IMatchingRequestsPeeker, MatchingRequestsPeeker>();

            services.AddScoped<IMessagesPeeker<SearchResultsNotification>, MessagesPeeker<SearchResultsNotification>>(sp =>
            {
                var factory = sp.GetService<IMessageReceiverFactory>();
                var messagingSettings = fetchMessagingServiceBusSettings(sp);
                var searchSettings = fetchSearchSettings(sp);
                return new MessagesPeeker<SearchResultsNotification>(
                    factory, messagingSettings.ConnectionString, searchSettings.ResultsTopic, searchSettings.ResultsSubscription);
            });
            services.AddScoped<ISearchResultNotificationsPeeker, SearchResultNotificationsPeeker>();

            services.AddScoped<IMessagesPeeker<SearchableDonorUpdate>, MessagesPeeker<SearchableDonorUpdate>>(sp =>
            {
                var factory = sp.GetService<IMessageReceiverFactory>();
                var settings = fetchDonorManagementSettings(sp);
                var messagingSettings = fetchMessagingServiceBusSettings(sp);
                return new MessagesPeeker<SearchableDonorUpdate>(
                    factory, messagingSettings.ConnectionString, settings.Topic, settings.Subscription);
            });
            services.AddScoped<ISearchableDonorUpdatesPeeker, SearchableDonorUpdatesPeeker>();

            services.AddScoped<IDonorStoresInspector, DonorStoresInspector>();

            services.AddScoped(typeof(IFileReader<>), typeof(FileReader<>));
            services.AddScoped<IWmdaExerciseOneScorer, WmdaExerciseOneScorer>();
            services.AddScoped<IWmdaExerciseTwoScorer, WmdaExerciseTwoScorer>();
            services.AddScoped<IScoreBatchRequester, ScoreBatchRequester>();
            services.AddScoped<IWmdaResultsTotalMismatchComparer, WmdaResultsTotalMismatchComparer>();
            services.AddScoped<IWmdaResultsAntigenMismatchComparer, WmdaResultsAntigenMismatchComparer>();
            services.AddScoped<IConvertHlaRequester, ConvertHlaRequester>();
            services.AddScoped<IWmdaDiscrepantResultsWriter, WmdaDiscrepantResultsWriter>();

            services.AddScoped<IWmdaDiscrepantAlleleResultsReporter, WmdaDiscrepantResultsReporter>(sp =>
            {
                var resultsComparer = sp.GetService<IWmdaResultsTotalMismatchComparer>();
                var cacheProvider = sp.GetService<ITransientCacheProvider>();
                var hlaConverter = sp.GetService<IConvertHlaRequester>();
                return new WmdaDiscrepantResultsReporter(resultsComparer, cacheProvider, hlaConverter, TargetHlaCategory.PGroup);
            });

            services.AddScoped<IWmdaDiscrepantAntigenResultsReporter, WmdaDiscrepantResultsReporter>(sp =>
            {
                var resultsComparer = sp.GetService<IWmdaResultsAntigenMismatchComparer>();
                var cacheProvider = sp.GetService<ITransientCacheProvider>();
                var hlaConverter = sp.GetService<IConvertHlaRequester>();
                return new WmdaDiscrepantResultsReporter(resultsComparer, cacheProvider, hlaConverter, TargetHlaCategory.Serology);
            });

            services.AddSingleton<ILogger, FileBasedLogger>();

            services.AddSingleton<IBlobDownloader>(sp =>
            {
                var storageSettings = fetchAzureStorageSettings(sp);
                var logger = sp.GetService<ILogger>();
                return new BlobDownloader(storageSettings.ConnectionString, logger);
            });

            services.AddScoped<ISearchOutcomesProcessor, SearchOutcomesProcessor>();

            services.AddScoped<IHaplotypeFrequencySetTransformer, HaplotypeFrequencySetTransformer>();
            services.AddScoped<ITransformedSetWriter, TransformedSetWriter>();
            services.RegisterFrequencyFileReader();
        }

        private static void RegisterDatabaseServices(
            this IServiceCollection services, 
            Func<IServiceProvider, string> fetchActiveMatchingSqlConnectionString,
            Func<IServiceProvider, string> fetchDonorImportSqlConnectionString)
        {
            services.AddScoped<IActiveMatchingDatabaseConnectionStringProvider, ActiveMatchingDatabaseConnectionStringProvider>(sp =>
                new ActiveMatchingDatabaseConnectionStringProvider(fetchActiveMatchingSqlConnectionString(sp)));
            services.AddScoped<IDonorReadRepository, DonorReadRepository>(sp =>
                new DonorReadRepository(fetchDonorImportSqlConnectionString(sp)));
        }
    }
}