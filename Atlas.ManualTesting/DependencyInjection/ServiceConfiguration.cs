using System;
using Atlas.Client.Models.Search.Results;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Caching;
using Atlas.Common.ServiceBus.BatchReceiving;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.ManualTesting.Common;
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
            services.AddSingleton<IMessageReceiverFactory, MessageReceiverFactory>(sp =>
                new MessageReceiverFactory(fetchMessagingServiceBusSettings(sp).ConnectionString)
            );
            services.AddSingleton<IDeadLetterReceiverFactory, DeadLetterReceiverFactory>(sp =>
                new DeadLetterReceiverFactory(fetchMessagingServiceBusSettings(sp).ConnectionString)
            );

            services.AddScoped(typeof(IMessagesPeeker<>), typeof(MessagesPeeker<>));
            services.AddScoped(typeof(IDeadLettersPeeker<>), typeof(DeadLettersPeeker<>));

            services.AddScoped<IDeadLettersPeeker<IdentifiedSearchRequest>, DeadLettersPeeker<IdentifiedSearchRequest>>(sp =>
            {
                var factory = sp.GetService<IDeadLetterReceiverFactory>();
                var settings = fetchMatchingSettings(sp);
                return new DeadLettersPeeker<IdentifiedSearchRequest>(factory, settings.RequestsTopic, settings.RequestsSubscription);
            });
            services.AddScoped<IMatchingRequestsPeeker, MatchingRequestsPeeker>();

            services.AddScoped<IMessagesPeeker<SearchResultsNotification>, MessagesPeeker<SearchResultsNotification>>(sp =>
            {
                var factory = sp.GetService<IMessageReceiverFactory>();
                var settings = fetchSearchSettings(sp);
                return new MessagesPeeker<SearchResultsNotification>(factory, settings.ResultsTopic, settings.ResultsSubscription);
            });
            services.AddScoped<ISearchResultNotificationsPeeker, SearchResultNotificationsPeeker>();

            services.AddScoped<IMessagesPeeker<SearchableDonorUpdate>, MessagesPeeker<SearchableDonorUpdate>>(sp =>
            {
                var factory = sp.GetService<IMessageReceiverFactory>();
                var settings = fetchDonorManagementSettings(sp);
                return new MessagesPeeker<SearchableDonorUpdate>(factory, settings.Topic, settings.Subscription);
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