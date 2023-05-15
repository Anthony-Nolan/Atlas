using System;
using Atlas.Client.Models.Search.Results;
using Atlas.Common.Caching;
using Atlas.Common.ServiceBus.BatchReceiving;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.ManualTesting.Common;
using Atlas.ManualTesting.Services;
using Atlas.ManualTesting.Services.Scoring;
using Atlas.ManualTesting.Services.ServiceBus;
using Atlas.ManualTesting.Services.WmdaConsensusResults;
using Atlas.ManualTesting.Settings;
using Atlas.MatchingAlgorithm.Common.Models;
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
                OptionsReaderFor<DonorManagementSettings>()
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
        }

        private static void RegisterServices(
            this IServiceCollection services,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, MatchingSettings> fetchMatchingSettings,
            Func<IServiceProvider, SearchSettings> fetchSearchSettings,
            Func<IServiceProvider, DonorManagementSettings> fetchDonorManagementSettings
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
            services.AddScoped<IScoreBatchRequester, ScoreBatchRequester>();
            services.AddScoped<IScoreRequestProcessor, ScoreRequestProcessor>();
            services.AddScoped<IWmdaResultsComparer, WmdaResultsComparer>();
            services.AddScoped<IWmdaDiscrepantResultsReporter, WmdaDiscrepantResultsReporter>();
            services.AddScoped<IConvertHlaRequester, ConvertHlaRequester>();
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