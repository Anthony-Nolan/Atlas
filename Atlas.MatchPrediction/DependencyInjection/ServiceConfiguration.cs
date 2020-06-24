using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Matching.Services;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface.DependencyInjection;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchPrediction.Data.Context;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Services;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Settings.Azure;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.MatchPrediction.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterMatchPredictionServices(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings,
            Func<IServiceProvider, HlaMetadataDictionarySettings> fetchHlaMetadataDictionarySettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacDictionarySettings,
            Func<IServiceProvider, NotificationsServiceBusSettings> fetchNotificationsServiceBusSettings
        )
        {
            services.RegisterSettings(fetchAzureStorageSettings, fetchNotificationsServiceBusSettings);
            services.RegisterAtlasLogger(fetchApplicationInsightsSettings);
            services.RegisterServices();
            services.RegisterDatabaseServices();
            services.RegisterClientServices();
            services.RegisterCommonMatchingServices();
            services.RegisterHlaMetadataDictionary(
                fetchHlaMetadataDictionarySettings,
                fetchApplicationInsightsSettings,
                fetchMacDictionarySettings
            );
        }

        private static void RegisterSettings(
            this IServiceCollection services,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings,
            Func<IServiceProvider, NotificationsServiceBusSettings> fetchNotificationsServiceBusSettings)
        {
            services.AddScoped(fetchAzureStorageSettings);
            services.AddScoped(fetchNotificationsServiceBusSettings);
        }

        private static void RegisterDatabaseServices(this IServiceCollection services)
        {
            services.AddDbContext<MatchPredictionContext>((sp, options) =>
            {
                var connString = GetSqlConnectionString(sp);
                options.UseSqlServer(connString);
            });

            services.AddScoped<IHaplotypeFrequencySetRepository, HaplotypeFrequencySetRepository>();
            services.AddScoped<IHaplotypeFrequenciesRepository, HaplotypeFrequenciesRepository>(sp =>
                new HaplotypeFrequenciesRepository(GetSqlConnectionString(sp))
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
            services.AddScoped<IFrequencySetMetadataExtractor, FrequencySetMetadataExtractor>();
            services.AddScoped<IFrequencySetImporter, FrequencySetImporter>();
            services.AddScoped<IFrequencyCsvReader, FrequencyCsvReader>();
            services.AddScoped<IFrequencySetService, FrequencySetService>();

            services.AddScoped<IGenotypeLikelihoodService, GenotypeLikelihoodService>();
            services.AddScoped<IUnambiguousGenotypeExpander, UnambiguousGenotypeExpander>();
            services.AddScoped<IGenotypeLikelihoodCalculator, GenotypeLikelihoodCalculator>();
            services.AddScoped<IGenotypeAlleleTruncater, GenotypeAlleleTruncater>();

            services.AddScoped<IAmbiguousPhenotypeExpander, AmbiguousPhenotypeExpander>();
            services.AddScoped<ICompressedPhenotypeExpander, CompressedPhenotypeExpander>();

            services.AddScoped<IMatchCalculationService, MatchCalculationService>();

            services.AddScoped<IMatchProbabilityService, MatchProbabilityService>();

            services.AddScoped<ILocusHlaConverter, LocusHlaConverter>();
        }

        private static string GetSqlConnectionString(IServiceProvider sp)
        {
            return sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["Sql"];
        }
    }
}