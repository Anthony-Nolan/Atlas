﻿using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Matching.Services;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface.DependencyInjection;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Data.Context;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Settings;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.MatchPrediction.ExternalInterface.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterMatchPredictionServices(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, HlaMetadataDictionarySettings> fetchHlaMetadataDictionarySettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacDictionarySettings,
            Func<IServiceProvider, NotificationsServiceBusSettings> fetchNotificationsServiceBusSettings,
            Func<IServiceProvider, string> fetchSqlConnectionString
        )
        {
            services.RegisterSettings(fetchNotificationsServiceBusSettings);
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

        public static void RegisterHaplotypeFrequenciesReader(
            this IServiceCollection services,
            Func<IServiceProvider, MatchPredictionAzureStorageSettings> fetchAzureStorageSettings,
            Func<IServiceProvider, string> fetchMatchPredictionDatabaseConnectionString)
        {
            services.RegisterHaplotypeFrequenciesReaderServices(fetchAzureStorageSettings);

            services.AddScoped<IHaplotypeFrequenciesReadRepository>(sp =>
                new HaplotypeFrequenciesReadRepository(fetchMatchPredictionDatabaseConnectionString(sp))
            );
        }

        private static void RegisterSettings(
            this IServiceCollection services,
            Func<IServiceProvider, NotificationsServiceBusSettings> fetchNotificationsServiceBusSettings)
        {
            services.MakeSettingsAvailableForUse(fetchNotificationsServiceBusSettings);
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

        private static void RegisterHaplotypeFrequenciesReaderServices(
            this IServiceCollection services,
            Func<IServiceProvider, MatchPredictionAzureStorageSettings> fetchAzureStorageSettings)
        {
            services.AddScoped<IHaplotypeFrequenciesReader, HaplotypeFrequenciesReader>();
            services.AddScoped<IFrequencyCsvReader, FrequencyCsvReader>();

            services.MakeSettingsAvailableForUse(fetchAzureStorageSettings);
            services.AddScoped<IFrequencySetStreamer, FrequencySetStreamer>();
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
            services.AddScoped<MatchPredictionLoggingContext>();
            services.AddScoped<IMatchPredictionLogger, MatchPredictionLogger>();
            
            services.AddScoped<IMatchPredictionAlgorithm, MatchPredictionAlgorithm>();
            
            services.AddScoped<IFrequencySetMetadataExtractor, FrequencySetMetadataExtractor>();
            services.AddScoped<IFrequencySetImporter, FrequencySetImporter>();
            services.AddScoped<IFrequencyCsvReader, FrequencyCsvReader>();
            services.AddScoped<IHaplotypeFrequencyService, HaplotypeFrequencyService>();

            services.AddScoped<IGenotypeLikelihoodService, GenotypeLikelihoodService>();
            services.AddScoped<IUnambiguousGenotypeExpander, UnambiguousGenotypeExpander>();
            services.AddScoped<IGenotypeLikelihoodCalculator, GenotypeLikelihoodCalculator>();
            services.AddScoped<IGenotypeAlleleTruncater, GenotypeAlleleTruncater>();

            services.AddScoped<IAmbiguousPhenotypeExpander, AmbiguousPhenotypeExpander>();
            services.AddScoped<ICompressedPhenotypeExpander, CompressedPhenotypeExpander>();

            services.AddScoped<IMatchCalculationService, MatchCalculationService>();

            services.AddScoped<IMatchProbabilityService, MatchProbabilityService>();
            services.AddScoped<IMatchProbabilityCalculator, MatchProbabilityCalculator>();
        }
    }
}