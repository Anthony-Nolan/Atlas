using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Caching;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.ExternalInterface.DependencyInjection;
using Atlas.HlaMetadataDictionary.ExternalInterface.DependencyInjection;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.ManualTesting.Common.Repositories;
using Atlas.ManualTesting.Common.Services;
using Atlas.ManualTesting.Common.Services.Storers;
using Atlas.ManualTesting.Common.Settings;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MatchPrediction.Test.Verification.Data.Context;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Services;
using Atlas.MatchPrediction.Test.Verification.Services.GenotypeSimulation;
using Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers;
using Atlas.MatchPrediction.Test.Verification.Services.SimulantGeneration;
using Atlas.MatchPrediction.Test.Verification.Services.Verification;
using Atlas.MatchPrediction.Test.Verification.Services.Verification.Compilation;
using Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing;
using Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing.Storers;
using Atlas.MatchPrediction.Test.Verification.Settings;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.DependencyInjection;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Atlas.MatchPrediction.Test.Verification.DependencyInjection
{
    internal static class ServiceConfiguration
    {
        public static void RegisterVerificationServices(
            this IServiceCollection services,
            Func<IServiceProvider, string> fetchMatchPredictionVerificationSqlConnectionString,
            Func<IServiceProvider, string> fetchMatchPredictionSqlConnectionString,
            Func<IServiceProvider, string> fetchDonorImportSqlConnectionString,
            Func<IServiceProvider, VerificationAzureStorageSettings> fetchVerificationAzureStorageSettings,
            Func<IServiceProvider, DataRefreshSettings> fetchDataRefreshSettings,
            Func<IServiceProvider, HlaMetadataDictionarySettings> fetchHlaMetadataDictionarySettings,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacDictionarySettings,
            Func<IServiceProvider, MacDownloadSettings> fetchMacDownloadSettings,
            Func<IServiceProvider, SearchRelatedMetadataServiceSettings> fetchSearchRelatedMetadataServiceSettings
        )
        {
            services.RegisterSettings();
            services.RegisterDatabaseServices(fetchMatchPredictionVerificationSqlConnectionString);
            services.RegisterServices(
                fetchMatchPredictionSqlConnectionString, fetchVerificationAzureStorageSettings, fetchDataRefreshSettings);
            services.RegisterLifeTimeScopedCacheTypes();
            services.RegisterHaplotypeFrequenciesReader(fetchMatchPredictionSqlConnectionString);

            services.RegisterHlaMetadataDictionary(
                fetchHlaMetadataDictionarySettings,
                fetchApplicationInsightsSettings,
                fetchMacDictionarySettings,
                fetchSearchRelatedMetadataServiceSettings
            );

            services.RegisterMacFetcher(fetchApplicationInsightsSettings, fetchMacDownloadSettings);
            services.RegisterImportDatabaseTypes(fetchDonorImportSqlConnectionString);
        }

        private static void RegisterSettings(this IServiceCollection services)
        {
            services.RegisterAsOptions<VerificationSearchSettings>("Search");
        }

        private static void RegisterDatabaseServices(this IServiceCollection services, Func<IServiceProvider, string> fetchSqlConnectionString)
        {
            services.AddScoped<IExpandedMacsRepository, ExpandedMacsRepository>(sp =>
                new ExpandedMacsRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<INormalisedPoolRepository, NormalisedPoolRepository>(sp =>
                new NormalisedPoolRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<ISimulantsRepository, SimulantsRepository>(sp =>
                new SimulantsRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<ISearchRequestsRepository<VerificationSearchRequestRecord>, SearchRequestsRepository>(sp =>
                new SearchRequestsRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<IMatchedDonorsRepository, MatchedDonorsRepository>(sp =>
                new MatchedDonorsRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<IProcessedResultsRepository<MatchedDonor>, MatchedDonorsRepository>(sp =>
                new MatchedDonorsRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<IProcessedResultsRepository<LocusMatchDetails>, LocusMatchDetailsRepository>(sp =>
                new LocusMatchDetailsRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<IProcessedResultsRepository<MatchedDonorProbability>, MatchProbabilitiesRepository>(sp =>
                new MatchProbabilitiesRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<IVerificationResultsRepository, VerificationResultsRepository>(sp =>
                new VerificationResultsRepository(fetchSqlConnectionString(sp)));

            services.AddScoped(sp => new ContextFactory().Create(fetchSqlConnectionString(sp)));
            services.AddScoped<ITestHarnessRepository, TestHarnessRepository>();
            services.AddScoped<ITestDonorExportRepository, TestDonorExportRepository>();
            services.AddScoped<IVerificationRunRepository, VerificationRunRepository>();
        }

        private static void RegisterServices(
            this IServiceCollection services, 
            Func<IServiceProvider, string> fetchMatchPredictionSqlConnectionString,
            Func<IServiceProvider, VerificationAzureStorageSettings> fetchVerificationAzureStorageSettings,
            Func<IServiceProvider, DataRefreshSettings> fetchDataRefreshSettings)
        {
            services.AddScoped<IBlobStreamer, BlobStreamer>(sp =>
            {
                var settings = fetchVerificationAzureStorageSettings(sp);
                return new BlobStreamer(settings.ConnectionString, sp.GetService<ILogger>());
            });

            services.AddScoped<IMacExpander, MacExpander>();

            services.AddScoped<ITestHarnessGenerator, TestHarnessGenerator>();
            services.AddScoped<IHaplotypeFrequenciesReader, HaplotypeFrequenciesReader>();
            services.AddScoped<INormalisedPoolGenerator, NormalisedPoolGenerator>(sp =>
                {
                    var reader = sp.GetService<IHaplotypeFrequenciesReader>();
                    var repo = sp.GetService<INormalisedPoolRepository>();
                    var dataSource = new SqlConnectionStringBuilder(fetchMatchPredictionSqlConnectionString(sp)).DataSource;
                    return new NormalisedPoolGenerator(reader, repo, dataSource);
                });
            services.AddScoped<IGenotypeSimulator, GenotypeSimulator>();
            services.AddScoped<IRandomNumberGenerator, RandomNumberGenerator>();
            services.AddScoped<IGenotypeSimulantsGenerator, GenotypeSimulantsGenerator>();
            services.AddScoped<IMaskedSimulantsGenerator, MaskedSimulantsGenerator>();
            services.AddScoped<ILocusHlaMasker, LocusHlaMasker>();
            services.AddScoped<IHlaDeleter, HlaDeleter>();
            services.AddScoped<ITwoFieldBuilder, TwoFieldBuilder>();
            services.AddScoped<IHlaConverter, HlaConverter>();
            services.AddScoped<IMacBuilder, MacBuilder>();
            services.AddScoped<IExpandedMacCache, ExpandedMacCache>();
            services.AddScoped<IXxCodeBuilder, XxCodeBuilder>();

            services.AddScoped<IVerificationAtlasPreparer, VerificationAtlasPreparer>(sp =>
            {
                var simRepo = sp.GetService<ISimulantsRepository>();
                var harnessRepo = sp.GetService<ITestHarnessRepository>();
                var testDonorExporter = sp.GetService<ITestDonorExporter>();
                var testDonorExportRepo = sp.GetService<ITestDonorExportRepository>();
                var requestUrl = fetchDataRefreshSettings(sp).RequestUrl;
                return new VerificationAtlasPreparer(simRepo, harnessRepo, testDonorExporter, testDonorExportRepo, requestUrl);
            });

            services.AddScoped<ITestDonorExporter, TestDonorExporter>();

            services.AddScoped<IVerificationRunner, VerificationRunner>();
            services.AddScoped<IGenotypeSimulantsInfoCache, GenotypeSimulantsInfoCache>();
            services.AddScoped<IResultSetProcessor<MatchingResultsNotification>, MatchingResultSetProcessor>();
            services.AddScoped<IResultSetProcessor<SearchResultsNotification>, SearchResultSetProcessor>();
            services.AddScoped<IResultsStorer<MatchingAlgorithmResult, MatchedDonor>, MatchingResultDonorStorer>();
            services.AddScoped<IResultsStorer<SearchResult, MatchedDonor>, SearchResultDonorStorer>();
            services.AddScoped<IResultsStorer<MatchingAlgorithmResult, LocusMatchDetails>, MatchingLocusDetailsStorer>();
            services.AddScoped<IResultsStorer<SearchResult, LocusMatchDetails>, SearchLocusDetailsStorer>();
            services.AddScoped(typeof(IMismatchedDonorsStorer<>), typeof(MismatchedDonorsStorer<>));
            services.AddScoped<IResultsStorer<SearchResult, MatchedDonorProbability>, MatchedDonorProbabilitiesStorer>();
            services.AddScoped<IVerificationResultsWriter, VerificationResultsWriter>();
            services.AddScoped<IVerificationResultsCompiler, VerificationResultsCompiler>();
            services.AddScoped<IActualVersusExpectedResultsCompiler, ActualVersusExpectedResultsCompiler>();
            services.AddScoped<IProbabilityBinCalculator, ProbabilityBinCalculator>();
        }
    }
}