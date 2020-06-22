using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.AlleleNames;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval.MatchedHlaConversion;
using Atlas.HlaMetadataDictionary.Services.HlaConversion;
using Atlas.HlaMetadataDictionary.WmdaDataAccess;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.HlaMetadataDictionary.ExternalInterface
{
    public static class ServiceConfiguration
    {
        public static void RegisterHlaMetadataDictionary(this IServiceCollection services,
            Func<IServiceProvider, string> fetchAzureStorageConnectionString,
            Func<IServiceProvider, string> fetchWmdaHlaNomenclatureFilesUri,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, MacImportSettings> fetchMacImportSettings)
        {
            services.RegisterLifeTimeScopedCacheTypes();
            services.RegisterCommonGeneticServices();
            services.AddScoped<IHlaMetadataDictionaryFactory, HlaMetadataDictionaryFactory>();
            services.AddScoped<IHlaMetadataCacheControl, HlaMetadataCacheControl>();
            services.RegisterStorageTypes(fetchAzureStorageConnectionString);
            services.RegisterTypesRelatedToDictionaryRecreation(fetchWmdaHlaNomenclatureFilesUri);
            services.RegisterServices();
            services.RegisterAtlasLogger(fetchApplicationInsightsSettings);
            services.RegisterMacDictionary(
                fetchApplicationInsightsSettings,
                fetchMacImportSettings);
        }

        private static void RegisterStorageTypes(this IServiceCollection services, Func<IServiceProvider, string> fetchAzureStorageConnectionString)
        {
            services.AddSingleton<ICloudTableFactory, CloudTableFactory>(sp =>
            {
                var azureConnectionString = fetchAzureStorageConnectionString(sp);
                return new CloudTableFactory(azureConnectionString);
            });
            services.AddSingleton<ITableReferenceRepository, TableReferenceRepository>();

            services.AddScoped<IHlaMatchingMetadataRepository, HlaMatchingMetadataRepository>();
            services.AddScoped<IHlaScoringMetadataRepository, HlaScoringMetadataRepository>();
            services.AddScoped<IAlleleNamesMetadataRepository, AlleleNamesMetadataRepository>();
            services.AddScoped<IDpb1TceGroupsMetadataRepository, Dpb1TceGroupsMetadataRepository>();
        }

        private static void RegisterTypesRelatedToDictionaryRecreation(this IServiceCollection services, Func<IServiceProvider, string> fetchWmdaHlaNomenclatureFilesUri)
        {
            services.AddScoped<IWmdaDataRepository, WmdaDataRepository>();

            services.AddScoped<IWmdaFileReader, WmdaFileDownloader>(sp =>
            {
                var wmdaHlaNomenclatureFilesUri = fetchWmdaHlaNomenclatureFilesUri(sp);
                return new WmdaFileDownloader(wmdaHlaNomenclatureFilesUri);
            });
            services.AddScoped<IWmdaHlaNomenclatureVersionAccessor, WmdaHlaNomenclatureVersionAccessor>();

            services.AddScoped<IAlleleNameHistoriesConsolidator, AlleleNameHistoriesConsolidator>();
            services.AddScoped<IAlleleNamesFromHistoriesExtractor, AlleleNamesFromHistoriesExtractor>();
            services.AddScoped<IAlleleNameVariantsExtractor, AlleleNameVariantsExtractor>();
            services.AddScoped<IReservedAlleleNamesExtractor, ReservedAlleleNamesExtractor>();

            services.AddScoped<IAlleleNamesService, AlleleNamesService>();
            services.AddScoped<IHlaMatchPreCalculationService, HlaMatchPreCalculationService>();
            services.AddScoped<IDpb1TceGroupsService, Dpb1TceGroupsService>();
            services.AddScoped<IHlaToMatchingMetaDataConverter, HlaToMatchingMetaDataConverter>();
            services.AddScoped<IHlaToScoringMetaDataConverter, HlaToScoringMetaDataConverter>();

            services.AddScoped<IRecreateHlaMetadataService, RecreateHlaMetadataService>();
        }

        private static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<IHlaConverter, HlaConverter>();
            services.AddScoped<IHlaNameToTwoFieldAlleleConverter, HlaNameToTwoFieldAlleleConverter>();
            services.AddScoped<IAlleleNamesMetadataService, AlleleNamesMetadataService>();
            services.AddScoped<IHlaMetadataService, HlaMetadataService>();
            services.AddScoped<ILocusHlaMatchingMetadataService, LocusHlaMatchingMetadataService>();
            services.AddScoped<IHlaMatchingMetadataService, HlaMatchingMetadataService>();
            services.AddScoped<IHlaScoringMetadataService, HlaScoringMetadataService>();
            services.AddScoped<IDpb1TceGroupMetadataService, Dpb1TceGroupMetadataService>();
        }
    }
}