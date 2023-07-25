using Atlas.Common.ApplicationInsights;
using Atlas.Common.Matching.Services;
using Atlas.Common.Notifications;
using Atlas.Common.ServiceBus;
using Atlas.HlaMetadataDictionary.ExternalInterface.DependencyInjection;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Data.Context;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.ResultsUpload;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Extensions.DependencyInjection;
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
            Func<IServiceProvider, MatchPredictionAlgorithmSettings> fetchMatchPredictionAlgorithmSettings,
            Func<IServiceProvider, NotificationsServiceBusSettings> fetchNotificationsServiceBusSettings,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings,
            Func<IServiceProvider, string> fetchSqlConnectionString
        )
        {
            services.RegisterSettings(fetchNotificationsServiceBusSettings, fetchAzureStorageSettings, fetchMatchPredictionAlgorithmSettings);
            services.RegisterAtlasLogger(fetchApplicationInsightsSettings);
            services.RegisterServices();
            services.RegisterDatabaseServices(fetchSqlConnectionString);
            services.RegisterClientServices();
            services.RegisterCommonMatchingServices();
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
            services.AddScoped(typeof(IMessageBatchPublisher<>), typeof(MessageBatchPublisher<>));

            // services for requesting a match prediction
            services.AddScoped<IMatchPredictionValidator, MatchPredictionValidator>();
            services.AddScoped<IMatchPredictionRequestDispatcher, MatchPredictionRequestDispatcher>();
            services.AddScoped<IMessageBatchPublisher<IdentifiedMatchPredictionRequest>, MessageBatchPublisher<IdentifiedMatchPredictionRequest>>(sp =>
            {
                var serviceBusSettings = messagingServiceBusSettings(sp);
                var matchPredictionRequestsSettings = matchPredictionRequestSettings(sp);
                return new MessageBatchPublisher<IdentifiedMatchPredictionRequest>(serviceBusSettings.ConnectionString, matchPredictionRequestsSettings.RequestsTopic);
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
                return new MessageBatchPublisher<MatchPredictionResultLocation>(serviceBusSettings.ConnectionString, matchPredictionRequestsSettings.ResultsTopic);
            });
        }

        private static void RegisterSettings(
            this IServiceCollection services,
            Func<IServiceProvider, NotificationsServiceBusSettings> fetchNotificationsServiceBusSettings,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings,
            Func<IServiceProvider, MatchPredictionAlgorithmSettings> fetchMatchPredictionAlgorithmSettings)
        {
            services.MakeSettingsAvailableForUse(fetchNotificationsServiceBusSettings);
            services.MakeSettingsAvailableForUse(fetchAzureStorageSettings);
            services.MakeSettingsAvailableForUse(fetchMatchPredictionAlgorithmSettings);
        }

        private static void RegisterDatabaseServices(this IServiceCollection services, Func<IServiceProvider, string> fetchSqlConnectionString)
        {
            services.AddTransient<IHaplotypeFrequencySetRepository, HaplotypeFrequencySetRepository>(sp =>
                new HaplotypeFrequencySetRepository(fetchSqlConnectionString(sp), new ContextFactory())
            );
            services.AddTransient<IHaplotypeFrequenciesRepository, HaplotypeFrequenciesRepository>(sp =>
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

        private static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<MatchProbabilityLoggingContext>();
            services.AddScoped(typeof(IMatchPredictionLogger<>), typeof(MatchPredictionLogger<>));

            services.AddScoped<IMatchPredictionAlgorithm, MatchPredictionAlgorithm>();
            services.AddScoped<IDonorInputBatcher, DonorInputBatcher>();

            services.AddScoped<IFrequencySetImporter, FrequencySetImporter>();
            services.AddScoped<IFrequencyFileParser, FrequencyFileParser>();
            services.AddScoped<IFrequencySetValidator, FrequencySetValidator>();
            services.AddScoped<IHaplotypeFrequencyService, HaplotypeFrequencyService>();
            services.AddScoped<IFrequencyConsolidator, FrequencyConsolidator>();

            services.AddScoped<IGenotypeLikelihoodService, GenotypeLikelihoodService>();
            services.AddScoped<IUnambiguousGenotypeExpander, UnambiguousGenotypeExpander>();
            services.AddScoped<IGenotypeLikelihoodCalculator, GenotypeLikelihoodCalculator>();
            services.AddScoped<IGenotypeAlleleTruncater, GenotypeAlleleTruncater>();
            
            services.AddScoped<ICompressedPhenotypeExpander, CompressedPhenotypeExpander>();
            services.AddScoped<ICompressedPhenotypeConverter, CompressedPhenotypeConverter>();

            services.AddScoped<IMatchCalculationService, MatchCalculationService>();

            services.AddScoped<IMatchProbabilityService, MatchProbabilityService>();
            services.AddScoped<IGenotypeImputationService, GenotypeImputationService>();
            services.AddScoped<IMatchProbabilityCalculator, MatchProbabilityCalculator>();
            services.AddScoped<IGenotypeConverter, GenotypeConverter>();

            services.AddScoped<ISearchDonorResultUploader, SearchDonorResultUploader>();
        }
    }
}