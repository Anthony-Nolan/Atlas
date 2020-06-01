using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.Data;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using Atlas.HlaMetadataDictionary.Repositories.LookupRepositories;
using Atlas.HlaMetadataDictionary.Services;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.AlleleNames;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval.HlaDataConversion;
using Atlas.MultipleAlleleCodeDictionary;
using Atlas.MultipleAlleleCodeDictionary.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.HlaMetadataDictionary.ExternalInterface
{
    public static class ServiceConfiguration
    {
        public static void RegisterHlaMetadataDictionary(this IServiceCollection services,
            Func<IServiceProvider, string> fetchAzureStorageConnectionString,
            Func<IServiceProvider, string> fetchWmdaHlaNomenclatureFilesUri,
            Func<IServiceProvider, string> fetchHlaClientApiKey,
            Func<IServiceProvider, string> fetchHlaClientBaseUrl,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings)
        {
            services.RegisterLifeTimeScopedCacheTypes();
            services.RegisterCommonGeneticServices();
            services.AddScoped<IHlaMetadataDictionaryFactory, HlaMetadataDictionaryFactory>();
            services.AddScoped<IHlaMetadataCacheControl, HlaMetadataCacheControl>();
            services.RegisterStorageTypes(fetchAzureStorageConnectionString);
            services.RegisterTypesRelatedToDictionaryRecreation(fetchWmdaHlaNomenclatureFilesUri);
            services.RegisterServices();
            services.RegisterAtlasLogger(fetchApplicationInsightsSettings);

            services.RegisterMacDictionaryUsageServices(
                fetchHlaClientApiKey,
                fetchHlaClientBaseUrl,
                fetchApplicationInsightsSettings
            );
        }

        private static void RegisterStorageTypes(this IServiceCollection services, Func<IServiceProvider, string> fetchAzureStorageConnectionString)
        {
            services.AddSingleton<ICloudTableFactory, CloudTableFactory>(sp =>
            {
                var azureConnectionString = fetchAzureStorageConnectionString(sp);
                return new CloudTableFactory(azureConnectionString);
            });
            services.AddSingleton<ITableReferenceRepository, TableReferenceRepository>();

            services.AddScoped<IHlaMatchingLookupRepository, HlaMatchingLookupRepository>();
            services.AddScoped<IHlaScoringLookupRepository, HlaScoringLookupRepository>();
            services.AddScoped<IAlleleNamesLookupRepository, AlleleNamesLookupRepository>();
            services.AddScoped<IDpb1TceGroupsLookupRepository, Dpb1TceGroupsLookupRepository>();
        }

        private static void RegisterTypesRelatedToDictionaryRecreation(this IServiceCollection services, Func<IServiceProvider, string> fetchWmdaHlaNomenclatureFilesUri)
        {
            services.AddScoped<IWmdaDataRepository, WmdaDataRepository>();

            services.AddScoped<IWmdaFileReader, WmdaFileDownloader>(sp =>
            {
                var wmdaHlaNomenclatureFilesUri = fetchWmdaHlaNomenclatureFilesUri(sp);
                return new WmdaFileDownloader(wmdaHlaNomenclatureFilesUri);
            });
            services.AddScoped<IWmdaHlaVersionAccessor, WmdaHlaVersionAccessor>();

            services.AddScoped<IAlleleNameHistoriesConsolidator, AlleleNameHistoriesConsolidator>();
            services.AddScoped<IAlleleNamesFromHistoriesExtractor, AlleleNamesFromHistoriesExtractor>();
            services.AddScoped<IAlleleNameVariantsExtractor, AlleleNameVariantsExtractor>();
            services.AddScoped<IReservedAlleleNamesExtractor, ReservedAlleleNamesExtractor>();

            services.AddScoped<IAlleleNamesService, AlleleNamesService>();
            services.AddScoped<IHlaMatchPreCalculationService, HlaMatchPreCalculationService>();
            services.AddScoped<IDpb1TceGroupsService, Dpb1TceGroupsService>();
            services.AddScoped<IHlaMatchingDataConverter, HlaMatchingDataConverter>();
            services.AddScoped<IHlaScoringDataConverter, HlaScoringDataConverter>();

            services.AddScoped<IRecreateHlaMetadataService, RecreateHlaMetadataService>();
        }

        private static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<IAlleleNamesLookupService, AlleleNamesLookupService>();
            services.AddScoped<IHlaLookupResultsService, HlaLookupResultsService>();
            services.AddScoped<ILocusHlaMatchingLookupService, LocusHlaMatchingLookupService>();
            services.AddScoped<IHlaMatchingLookupService, HlaMatchingLookupService>();
            services.AddScoped<IHlaScoringLookupService, HlaScoringLookupService>();
            services.AddScoped<IDpb1TceGroupLookupService, Dpb1TceGroupLookupService>();
        }
    }
}