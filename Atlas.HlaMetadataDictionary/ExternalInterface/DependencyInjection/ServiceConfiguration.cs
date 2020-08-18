using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.AlleleNames;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.MatchedHlaConversion;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Services.HlaConversion;
using Atlas.HlaMetadataDictionary.WmdaDataAccess;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.DependencyInjection;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterHlaMetadataDictionary(
            this IServiceCollection services,
            Func<IServiceProvider, HlaMetadataDictionarySettings> fetchHlaMetadataDictionarySettings,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacDictionarySettings)
        {
            services.MakeSettingsAvailableForUse(fetchHlaMetadataDictionarySettings);

            services.RegisterServices();
            services.RegisterLifeTimeScopedCacheTypes();
            services.RegisterCommonGeneticServices();
            services.AddScoped<IHlaMetadataDictionaryFactory, HlaMetadataDictionaryFactory>();
            services.AddScoped<IHlaMetadataCacheControl, HlaMetadataCacheControl>();
            services.RegisterStorageTypes();
            services.RegisterTypesRelatedToDictionaryRecreation();
            services.RegisterServices();
            services.RegisterAtlasLogger(fetchApplicationInsightsSettings);
            services.RegisterMacDictionary(fetchApplicationInsightsSettings, fetchMacDictionarySettings);
        }

        private static void RegisterStorageTypes(this IServiceCollection services)
        {
            services.AddSingleton<ICloudTableFactory, CloudTableFactory>();
            services.AddSingleton<ITableReferenceRepository, TableReferenceRepository>();

            services.AddScoped<IGGroupToPGroupMetadataRepository, GGroupToPGroupMetadataRepository>();
            services.AddScoped<IHlaMatchingMetadataRepository, HlaMatchingMetadataRepository>();
            services.AddScoped<IHlaScoringMetadataRepository, HlaScoringMetadataRepository>();
            services.AddScoped<IAlleleNamesMetadataRepository, AlleleNamesMetadataRepository>();
            services.AddScoped<IDpb1TceGroupsMetadataRepository, Dpb1TceGroupsMetadataRepository>();
            services.AddScoped<IAlleleGroupsMetadataRepository, AlleleGroupsMetadataRepository>();
        }

        private static void RegisterTypesRelatedToDictionaryRecreation(this IServiceCollection services)
        {
            services.AddScoped<IWmdaDataRepository, WmdaDataRepository>();
            services.AddScoped<IWmdaFileReader, WmdaFileDownloader>();
            services.AddScoped<IWmdaHlaNomenclatureVersionAccessor, WmdaHlaNomenclatureVersionAccessor>();

            services.AddScoped<IAlleleNameHistoriesConsolidator, AlleleNameHistoriesConsolidator>();
            services.AddScoped<IAlleleNamesFromHistoriesExtractor, AlleleNamesFromHistoriesExtractor>();
            services.AddScoped<IAlleleNameVariantsExtractor, AlleleNameVariantsExtractor>();
            services.AddScoped<IReservedAlleleNamesExtractor, ReservedAlleleNamesExtractor>();

            services.AddScoped<IGGroupToPGroupService, GGroupToPGroupService>();
            services.AddScoped<IAlleleNamesService, AlleleNamesService>();
            services.AddScoped<IHlaMatchPreCalculationService, HlaMatchPreCalculationService>();
            services.AddScoped<IDpb1TceGroupsService, Dpb1TceGroupsService>();
            services.AddScoped<IAlleleGroupsService, AlleleGroupsService>();
            services.AddScoped<IHlaToMatchingMetaDataConverter, HlaToMatchingMetaDataConverter>();
            services.AddScoped<IHlaToScoringMetaDataConverter, HlaToScoringMetaDataConverter>();

            services.AddScoped<IRecreateHlaMetadataService, RecreateHlaMetadataService>();
        }

        private static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<IHlaConverter, HlaConverter>();
            services.AddScoped<IHlaNameToTwoFieldAlleleConverter, HlaNameToTwoFieldAlleleConverter>();
            services.AddScoped<IHlaNameToPGroupConverter, HlaNameToPGroupConverter>();
            services.AddScoped<IGGroupToPGroupMetadataService, GGroupToPGroupMetadataService>();
            services.AddScoped<IAlleleNamesMetadataService, AlleleNamesMetadataService>();
            services.AddScoped<IAlleleGroupExpander, AlleleGroupExpander>();
            services.AddScoped<IHlaMetadataGenerationOrchestrator, HlaMetadataGenerationOrchestrator>();
            services.AddScoped<ILocusHlaMatchingMetadataService, LocusHlaMatchingMetadataService>();
            services.AddScoped<IHlaMatchingMetadataService, HlaMatchingMetadataService>();
            services.AddScoped<IHlaScoringMetadataService, HlaScoringMetadataService>();
            services.AddScoped<IDpb1TceGroupMetadataService, Dpb1TceGroupMetadataService>();
        }
    }
}