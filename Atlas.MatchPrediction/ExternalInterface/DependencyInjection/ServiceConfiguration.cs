using Atlas.Common.ApplicationInsights;
using Atlas.Common.Matching.Services;
using Atlas.Common.Notifications;
using Atlas.Common.ServiceBus;
using Atlas.Common.ServiceBus.DependencyInjection;
using Atlas.HlaMetadataDictionary.ExternalInterface.DependencyInjection;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Data.Context;
using Atlas.MatchPrediction.Data.Repositories;
using Microsoft.Extensions.Logging;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.ResultsUpload;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using Atlas.MatchPrediction.Services.HlaConversion;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.MatchPrediction.ExternalInterface.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterMatchPredictionAlgorithm(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, HlaMetadataDictionarySettings> fetchHlaMetadataDictionarySettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacDictionarySettings,
            Func<IServiceProvider, NotificationsServiceBusSettings> fetchNotificationsServiceBusSettings,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings,
            Func<IServiceProvider, GenotypeImputationSettings> fetchGenotypeImputationSettings,
            Func<IServiceProvider, string> fetchSqlConnectionString
        )
        {
            services.RegisterGenotypeSetPipeline(
                fetchApplicationInsightsSettings,
                fetchHlaMetadataDictionarySettings,
                fetchMacDictionarySettings,
                fetchGenotypeImputationSettings,
                fetchSqlConnectionString);

            services.RegisterSettings(fetchNotificationsServiceBusSettings, fetchAzureStorageSettings);
            services.RegisterSearchServices();
            services.RegisterClientServices();
            services.RegisterCommonMatchingServices();
        }

        /// <summary>
        /// Registers the genotype set pipeline alone: <see cref="IGenotypeSetService"/> (imputation, truncation and
        /// conversion to the matching algorithm's P groups) and the haplotype frequency set lookup,
        /// <see cref="IHaplotypeFrequencyLookupService"/>. This is what a host needs to precompute donor genotype sets,
        /// without the rest of match prediction.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Needs no Service Bus, notification or blob storage settings: nothing on this path sends a notification or
        /// uploads a result. Frequency set import, which does, is registered by
        /// <see cref="RegisterMatchPredictionAlgorithm"/> only.
        /// </para>
        ///
        /// <para>
        /// <b>The host must also register <c>IOptions&lt;HaplotypeFrequencySetCacheSettings&gt;</c></b>, as every match
        /// prediction host already does (e.g. <c>RegisterAsOptions&lt;HaplotypeFrequencySetCacheSettings&gt;</c>).
        /// It is read through <c>IOptions</c> by the frequency set cache, so it is not passed in here.
        /// </para>
        ///
        /// <para>
        /// Safe to call as well as <see cref="RegisterMatchPredictionAlgorithm"/>, which calls it: the services it adds
        /// are added only if not already registered, so a host that registers both gets one of each.
        /// </para>
        /// </remarks>
        public static void RegisterGenotypeSetPipeline(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, HlaMetadataDictionarySettings> fetchHlaMetadataDictionarySettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacDictionarySettings,
            Func<IServiceProvider, GenotypeImputationSettings> fetchGenotypeImputationSettings,
            Func<IServiceProvider, string> fetchSqlConnectionString)
        {
            services.MakeSettingsAvailableForUse(fetchGenotypeImputationSettings);
            services.RegisterAtlasLogger(fetchApplicationInsightsSettings);
            services.RegisterGenotypeSetPipelineServices();
            services.RegisterDatabaseServices(fetchSqlConnectionString);
            services.RegisterHlaMetadataDictionary(
                fetchHlaMetadataDictionarySettings,
                fetchApplicationInsightsSettings,
                fetchMacDictionarySettings
            );
        }

        public static void RegisterMatchPredictionValidator(this IServiceCollection services)
        {
            services.AddScoped<IMatchPredictionValidator, MatchPredictionValidator>();
        }

        public static void RegisterHaplotypeFrequenciesReader(
            this IServiceCollection services,
            Func<IServiceProvider, string> fetchMatchPredictionDatabaseConnectionString)
        {
            services.AddScoped<IHaplotypeFrequencySetReader, HaplotypeFrequencySetReader>();
            services.AddScoped<IFrequencySetValidator, FrequencySetValidator>();
            services.RegisterFrequencyFileReader();

            services.AddScoped<IHaplotypeFrequencySetReadRepository>(sp =>
                new HaplotypeFrequencySetReadRepository(fetchMatchPredictionDatabaseConnectionString(sp))
            );
        }

        public static void RegisterFrequencyFileReader(this IServiceCollection services)
        {
            services.AddScoped<IFrequencyFileParser, FrequencyFileParser>();
        }

        public static void RegisterMatchPredictionRequester(
            this IServiceCollection services,
            Func<IServiceProvider, MessagingServiceBusSettings> messagingServiceBusSettings,
            Func<IServiceProvider, MatchPredictionRequestsSettings> matchPredictionRequestSettings)
        {
            var serviceKey = typeof(MessagingServiceBusSettings);
            services.RegisterServiceBusAsKeyedServices(serviceKey,sp => messagingServiceBusSettings(sp).ConnectionString);
            services.MakeSettingsAvailableForUse(matchPredictionRequestSettings);

            // services for requesting a match prediction
            services.AddScoped<IMatchPredictionValidator, MatchPredictionValidator>();
            services.AddScoped<IMatchPredictionRequestDispatcher, MatchPredictionRequestDispatcher>();
            services.AddScoped<IMessageBatchPublisher<IdentifiedMatchPredictionRequest>, MessageBatchPublisher<IdentifiedMatchPredictionRequest>>(sp =>
            {
                var serviceBusSettings = messagingServiceBusSettings(sp);
                var matchPredictionRequestsSettings = matchPredictionRequestSettings(sp);
                var logger = sp.GetService<IAtlasLogger>();
                var topicClientFactory = sp.GetRequiredKeyedService<ITopicClientFactory>(typeof(MessagingServiceBusSettings));
                return new MessageBatchPublisher<IdentifiedMatchPredictionRequest>(topicClientFactory, matchPredictionRequestsSettings.RequestsTopic,
                    serviceBusSettings.SendRetryCount, serviceBusSettings.SendRetryCooldownSeconds, logger);
            });

            // services for running a match prediction request
            services.AddScoped<IMatchPredictionRequestRunner, MatchPredictionRequestRunner>();
            services.AddScoped<MatchPredictionRequestLoggingContext>();
            services.AddScoped<IMatchPredictionRequestResultUploader, MatchPredictionRequestResultUploader>();
            services.RegisterMatchPredictionResultsLocationPublisher(messagingServiceBusSettings, matchPredictionRequestSettings);
        }

        public static void RegisterMatchPredictionResultsLocationPublisher(
            this IServiceCollection services,
            Func<IServiceProvider, MessagingServiceBusSettings> messagingServiceBusSettings,
            Func<IServiceProvider, MatchPredictionRequestsSettings> matchPredictionRequestSettings)
        {
            services.AddScoped<IMessageBatchPublisher<MatchPredictionResultLocation>, MessageBatchPublisher<MatchPredictionResultLocation>>(sp =>
            {
                var serviceBusSettings = messagingServiceBusSettings(sp);
                var matchPredictionRequestsSettings = matchPredictionRequestSettings(sp);
                var logger = sp.GetService<IAtlasLogger>();
                var topicClientFactory = sp.GetRequiredKeyedService<ITopicClientFactory>(typeof(MessagingServiceBusSettings));
                return new MessageBatchPublisher<MatchPredictionResultLocation>(topicClientFactory, matchPredictionRequestsSettings.ResultsTopic,
                    serviceBusSettings.SendRetryCount, serviceBusSettings.SendRetryCooldownSeconds, logger);
            });
        }

        /// <summary>
        /// Registers <see cref="ISessionMessagePublisher{T}"/> for <see cref="ParallelMatchPredictionBatchResult"/>
        /// so the ACA Worker can publish batch results to the session-enabled
        /// <c>parallel-match-prediction-results</c> Service Bus topic.
        /// </summary>
        public static void RegisterParallelMatchPredictionBatchResultPublisher(
            this IServiceCollection services,
            Func<IServiceProvider, MessagingServiceBusSettings> messagingServiceBusSettings,
            Func<IServiceProvider, MatchPredictionRequestsSettings> matchPredictionRequestSettings)
        {
            services.AddScoped<ISessionMessagePublisher<ParallelMatchPredictionBatchResult>,
                SessionMessagePublisher<ParallelMatchPredictionBatchResult>>(sp =>
            {
                var serviceBusSettings = messagingServiceBusSettings(sp);
                var requestsSettings = matchPredictionRequestSettings(sp);
                var logger = sp.GetRequiredService<ILogger<SessionMessagePublisher<ParallelMatchPredictionBatchResult>>>();
                var topicClientFactory = sp.GetRequiredKeyedService<ITopicClientFactory>(typeof(MessagingServiceBusSettings));
                return new SessionMessagePublisher<ParallelMatchPredictionBatchResult>(
                    topicClientFactory,
                    requestsSettings.ParallelResultsTopic,
                    serviceBusSettings.SendRetryCount,
                    serviceBusSettings.SendRetryCooldownSeconds,
                    logger);
            });
        }

        private static void RegisterSettings(
            this IServiceCollection services,
            Func<IServiceProvider, NotificationsServiceBusSettings> fetchNotificationsServiceBusSettings,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings)
        {
            services.MakeSettingsAvailableForUse(fetchNotificationsServiceBusSettings);
            services.MakeSettingsAvailableForUse(fetchAzureStorageSettings);
        }

        private static void RegisterDatabaseServices(this IServiceCollection services, Func<IServiceProvider, string> fetchSqlConnectionString)
        {
            services.TryAddTransient<IHaplotypeFrequencySetRepository>(sp =>
                new HaplotypeFrequencySetRepository(fetchSqlConnectionString(sp), new ContextFactory())
            );
            services.TryAddTransient<IHaplotypeFrequenciesRepository>(sp =>
                new HaplotypeFrequenciesRepository(fetchSqlConnectionString(sp))
            );
        }

        private static void RegisterClientServices(this IServiceCollection services)
        {
            services.RegisterNotificationSender(
                OptionsReaderFor<NotificationsServiceBusSettings>(),
                OptionsReaderFor<ApplicationInsightsSettings>()
            );
        }

        /// <summary>
        /// The services <see cref="IGenotypeSetService"/> needs, and the frequency set lookup. Added only if not already
        /// registered - see <see cref="RegisterGenotypeSetPipeline"/>.
        /// </summary>
        private static void RegisterGenotypeSetPipelineServices(this IServiceCollection services)
        {
            services.TryAddScoped<MatchProbabilityLoggingContext>();
            services.TryAdd(ServiceDescriptor.Scoped(typeof(IMatchPredictionLogger<>), typeof(MatchPredictionLogger<>)));

            services.TryAddScoped<IHaplotypeFrequencyLookupService, HaplotypeFrequencyLookupService>();
            services.TryAddScoped<IFrequencyConsolidator, FrequencyConsolidator>();
            services.TryAddScoped<IHaplotypeFrequencyCache, HaplotypeFrequencyCache>();
            services.TryAddSingleton<IHaplotypeFrequencySetCacheProvider>(_ =>
                new HaplotypeFrequencySetCacheProvider(new CachingService(new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions())))));

            // Singleton, not scoped like IHaplotypeFrequencyCache above: eviction has to be tracked once for the
            // whole process, since IHaplotypeFrequencySetCacheProvider's underlying cache is itself a singleton
            // shared by every scope. A tracker constructed per-scope would start empty each time and let residency
            // grow unboundedly across scopes - exactly the bug this fix exists to close.
            services.TryAddSingleton<IFrequencySetResidencyTracker>(sp =>
            {
                var cacheSettings = sp.GetRequiredService<IOptions<HaplotypeFrequencySetCacheSettings>>().Value;
                var cacheProvider = sp.GetRequiredService<IHaplotypeFrequencySetCacheProvider>();
                return new FrequencySetResidencyTracker(
                    cacheSettings.MaxCachedFrequencySets,
                    evictedSetId => cacheProvider.Cache.Remove(HaplotypeFrequencyCache.AllFrequenciesCacheKey(evictedSetId))
                );
            });

            services.TryAddScoped<ICompressedPhenotypeExpander, CompressedPhenotypeExpander>();
            services.TryAddScoped<ICompressedPhenotypeConverter, CompressedPhenotypeConverter>();
            services.TryAddScoped<IHlaToTargetCategoryConverter, HlaToTargetCategoryConverter>();
            services.TryAddScoped<ISmallGGroupToPGroupConverter, SmallGGroupToPGroupConverter>();
            services.TryAddScoped<IGGroupToPGroupConverter, GGroupToPGroupConverter>();

            services.TryAddScoped<IGenotypeImputationService, GenotypeImputationService>();
            services.TryAddScoped<IGenotypeSetService, GenotypeSetService>();
            services.TryAddScoped<IGenotypeConverter, GenotypeConverter>();
        }

        /// <summary>
        /// Everything else match prediction registers: the search-time algorithms, match probability, genotype
        /// likelihood, frequency set import and the result uploaders.
        /// </summary>
        private static void RegisterSearchServices(this IServiceCollection services)
        {
            services.AddScoped<IMatchPredictionAlgorithm, MatchPredictionAlgorithm>();
            services.AddScoped<IParallelMatchPredictionAlgorithm, ParallelMatchPredictionAlgorithm>();
            services.AddScoped<IDonorInputBatcher, DonorInputBatcher>();

            services.AddScoped<IFrequencySetImporter, FrequencySetImporter>();
            services.AddScoped<IFrequencyFileParser, FrequencyFileParser>();
            services.AddScoped<IFrequencySetValidator, FrequencySetValidator>();
            services.AddScoped<IHaplotypeFrequencyService, HaplotypeFrequencyService>();

            services.AddScoped<IGenotypeLikelihoodService, GenotypeLikelihoodService>();
            services.AddScoped<IUnambiguousGenotypeExpander, UnambiguousGenotypeExpander>();
            services.AddScoped<IGenotypeLikelihoodCalculator, GenotypeLikelihoodCalculator>();
            services.AddScoped<IGenotypeAlleleTruncater, GenotypeAlleleTruncater>();

            services.AddScoped<IMatchCalculationService, MatchCalculationService>();

            services.AddScoped<IMatchProbabilityService, MatchProbabilityService>();
            services.AddScoped<IGenotypeMatcher, GenotypeMatcher>();
            services.AddScoped<IMatchProbabilityCalculator, MatchProbabilityCalculator>();

            services.AddScoped<ISearchDonorResultUploader, SearchDonorResultUploader>();
            services.AddScoped<IMatchPredictionBatchResultUploader, MatchPredictionBatchResultUploader>();
        }
    }
}