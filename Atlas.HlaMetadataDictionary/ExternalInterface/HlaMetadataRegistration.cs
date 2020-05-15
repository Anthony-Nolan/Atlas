using System;
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
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.HlaMetadataDictionary.ExternalInterface
{
    public static class HlaMetadataRegistration
    {
        public static void RegisterHlaMetadataDictionary(this IServiceCollection services,
            Func<IServiceProvider, string> fetchAzureStorageConnectionString,
            Func<IServiceProvider, string> fetchWmdaHlaNomenclatureFilesUri,
            Func<IServiceProvider, string> fetchHlaClientApiKey,
            Func<IServiceProvider, string> fetchHlaClientBaseUrl,
            Func<IServiceProvider, string> fetchInsightsInstrumentationKey)
        {
            services.AddScoped<IHlaMetadataDictionaryFactory, HlaMetadataDictionaryFactory>();
            services.AddScoped<IWmdaHlaVersionProvider, WmdaHlaVersionProvider>();
            services.RegisterLifeTimeScopedCacheTypes();
            services.RegisterHlaMetadataDictionaryStorageTypes(fetchAzureStorageConnectionString);
            services.RegisterHlaMetadataDictionaryPreCalculationTypes(fetchWmdaHlaNomenclatureFilesUri);
            services.RegisterHlaMetadataDictionaryServices();

            services.RegisterMacDictionaryServices(
                fetchHlaClientApiKey,
                fetchHlaClientBaseUrl,
                fetchInsightsInstrumentationKey
            );
        }

        private static void RegisterHlaMetadataDictionaryStorageTypes(this IServiceCollection services, Func<IServiceProvider, string> fetchAzureStorageConnectionString)
        {
            services.AddSingleton<ICloudTableFactory, CloudTableFactory>(sp =>
            {
                var azureConnectionString = fetchAzureStorageConnectionString(sp);
                return new CloudTableFactory(azureConnectionString);
            });

            services.AddSingleton<ITableReferenceRepository, TableReferenceRepository>(); //@Reviewer. This seems odd. Why would this want to be Singleton.

            services.AddScoped<IHlaMatchingLookupRepository, HlaMatchingLookupRepository>();
            services.AddScoped<IHlaScoringLookupRepository, HlaScoringLookupRepository>();
            services.AddScoped<IAlleleNamesLookupRepository, AlleleNamesLookupRepository>();
            services.AddScoped<IDpb1TceGroupsLookupRepository, Dpb1TceGroupsLookupRepository>();
        }

        private static void RegisterHlaMetadataDictionaryPreCalculationTypes(this IServiceCollection services, Func<IServiceProvider, string> fetchWmdaHlaNomenclatureFilesUri)
        {
            services.AddScoped<IWmdaDataRepository, WmdaDataRepository>();

            services.AddScoped<IWmdaFileReader, WmdaFileDownloader>(sp =>
            {
                var wmdaHlaNomenclatureFilesUri = fetchWmdaHlaNomenclatureFilesUri(sp);
                return new WmdaFileDownloader(wmdaHlaNomenclatureFilesUri);
            });

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

        private static void RegisterHlaMetadataDictionaryServices(this IServiceCollection services)
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