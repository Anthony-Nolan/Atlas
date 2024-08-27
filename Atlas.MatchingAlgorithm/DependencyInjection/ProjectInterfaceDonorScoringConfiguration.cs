using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.DependencyInjection;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchingAlgorithm.Data.Persistent.Context;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Aggregation;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.AntigenMatching;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Confidence;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Ranking;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MatchingAlgorithm.DependencyInjection
{
    /// <summary>
    /// Contains registrations necessary to set up a project-project interface to access
    /// the donor scoring feature (without ranking).
    /// </summary>
    public static class ProjectInterfaceDonorScoringConfiguration
    {
        public static void RegisterMatchingAlgorithmScoring(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, HlaMetadataDictionarySettings> fetchHlaMetadataDictionarySettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacDictionarySettings,
            Func<IServiceProvider, SearchRelatedMetadataServiceSettings> fetchSearchRelatedMetadataServiceSettings,
            Func<IServiceProvider, string> fetchPersistentSqlConnectionString)
        {
            services.RegisterSettings(
                fetchApplicationInsightsSettings,
                fetchHlaMetadataDictionarySettings,
                fetchMacDictionarySettings);

            services.RegisterDataServices(fetchPersistentSqlConnectionString);

            services.RegisterServices(
                fetchHlaMetadataDictionarySettings,
                fetchApplicationInsightsSettings,
                fetchMacDictionarySettings,
                fetchSearchRelatedMetadataServiceSettings,
                fetchPersistentSqlConnectionString);
        }

        private static void RegisterSettings(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, HlaMetadataDictionarySettings> fetchHlaMetadataDictionarySettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacDictionarySettings)
        {
            services.MakeSettingsAvailableForUse(fetchApplicationInsightsSettings);
            services.MakeSettingsAvailableForUse(fetchHlaMetadataDictionarySettings);
            services.MakeSettingsAvailableForUse(fetchMacDictionarySettings);
        }

        private static void RegisterDataServices(this IServiceCollection services, Func<IServiceProvider, string> fetchPersistentSqlConnectionString)
        {
            services.AddScoped(sp => new ContextFactory().Create(fetchPersistentSqlConnectionString(sp)));
            services.AddScoped<IDataRefreshHistoryRepository, DataRefreshHistoryRepository>();
            services.AddScoped<IScoringWeightingRepository, ScoringWeightingRepository>();
        }

        private static void RegisterServices(
            this IServiceCollection services,
            Func<IServiceProvider, HlaMetadataDictionarySettings> fetchHlaMetadataDictionarySettings,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacDictionarySettings,
            Func<IServiceProvider, SearchRelatedMetadataServiceSettings> fetchSearchRelatedMetadataServiceSettings,
            Func<IServiceProvider, string> fetchPersistentSqlConnectionString
            )
        {
            services.AddScoped(sp => new ConnectionStrings
            {
                Persistent = fetchPersistentSqlConnectionString(sp)
            });

            services.AddSingleton<IMemoryCache, MemoryCache>(sp => new MemoryCache(new MemoryCacheOptions()));
            services.RegisterLifeTimeScopedCacheTypes();
            services.AddScoped<IScoringCache, ScoringCache>();

            services.AddScoped<IDonorScoringService, DonorScoringService>();
            services.AddScoped<IGradingService, GradingService>();
            services.AddScoped<IConfidenceService, ConfidenceService>();
            services.AddScoped<IConfidenceCalculator, ConfidenceCalculator>();
            services.AddScoped<IAntigenMatchingService, AntigenMatchingService>();
            services.AddScoped<IAntigenMatchCalculator, AntigenMatchCalculator>();
            services.AddScoped<IMatchScoreCalculator, MatchScoreCalculator>();
            services.AddScoped<IScoringRequestService, ScoringRequestService>();
            services.AddScoped<IScoreResultAggregator, ScoreResultAggregator>();
            services.AddScoped<IDpb1TceGroupMatchCalculator, Dpb1TceGroupMatchCalculator>();

            services.RegisterCommonGeneticServices();
            services.AddScoped<IActiveHlaNomenclatureVersionAccessor, ActiveHlaNomenclatureVersionAccessor>();
            services.RegisterHlaMetadataDictionary(
                fetchHlaMetadataDictionarySettings, fetchApplicationInsightsSettings, fetchMacDictionarySettings, fetchSearchRelatedMetadataServiceSettings);
        }
    }
}