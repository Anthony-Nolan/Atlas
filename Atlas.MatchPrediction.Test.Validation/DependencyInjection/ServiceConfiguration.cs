using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.ExternalInterface.DependencyInjection;
using Atlas.ManualTesting.Common.Models;
using Atlas.ManualTesting.Common.Repositories;
using Atlas.ManualTesting.Common.Services;
using Atlas.ManualTesting.Common.Settings;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Test.Validation.Data.Repositories;
using Atlas.MatchPrediction.Test.Validation.Models;
using Atlas.MatchPrediction.Test.Validation.Services;
using Atlas.MatchPrediction.Test.Validation.Services.Exercise3;
using Atlas.MatchPrediction.Test.Validation.Services.Exercise4;
using Atlas.MatchPrediction.Test.Validation.Settings;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Atlas.MatchPrediction.Test.Validation.DependencyInjection
{
    internal static class ServiceConfiguration
    {
        public static void RegisterValidationServices(
            this IServiceCollection services,
            Func<IServiceProvider, OutgoingMatchPredictionRequestSettings> fetchOutgoingMatchPredictionRequestSettings,
            Func<IServiceProvider, ValidationAzureStorageSettings> fetchValidationAzureStorageSettings,
            Func<IServiceProvider, DataRefreshSettings> fetchDataRefreshSettings,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessageServiceBusSettings,
            Func<IServiceProvider, MatchPredictionRequestsSettings> fetchMatchPredictionRequestSettings,
            Func<IServiceProvider, ValidationSearchSettings> fetchValidationSearchSettings,
            Func<IServiceProvider, string> fetchMatchPredictionValidationSqlConnectionString,
            Func<IServiceProvider, string> fetchMatchPredictionSqlConnectionString,
            Func<IServiceProvider, string> fetchDonorImportSqlConnectionString)
        {
            services.RegisterSettings(
                fetchOutgoingMatchPredictionRequestSettings,
                fetchValidationAzureStorageSettings,
                fetchValidationSearchSettings);
            services.RegisterDatabaseServices(fetchMatchPredictionValidationSqlConnectionString);
            services.RegisterServices(fetchValidationAzureStorageSettings, fetchDataRefreshSettings);
            services.RegisterHaplotypeFrequenciesReader(fetchMatchPredictionSqlConnectionString);
            services.RegisterImportDatabaseTypes(fetchDonorImportSqlConnectionString);
            services.RegisterMatchPredictionResultsLocationPublisher(fetchMessageServiceBusSettings, fetchMatchPredictionRequestSettings);
        }

        private static void RegisterSettings(
            this IServiceCollection services,
            Func<IServiceProvider, OutgoingMatchPredictionRequestSettings> fetchOutgoingMatchPredictionRequestSettings,
            Func<IServiceProvider, ValidationAzureStorageSettings> fetchValidationAzureStorageSettings,
            Func<IServiceProvider, ValidationSearchSettings> fetchValidationSearchSettings)
        {
            services.MakeSettingsAvailableForUse(fetchOutgoingMatchPredictionRequestSettings);
            services.MakeSettingsAvailableForUse(fetchValidationAzureStorageSettings);
            services.MakeSettingsAvailableForUse(fetchValidationSearchSettings);
        }

        private static void RegisterDatabaseServices(this IServiceCollection services, Func<IServiceProvider, string> fetchSqlConnectionString)
        {
            services.AddScoped<IValidationRepository, ValidationRepository>(sp =>
                new ValidationRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<ISubjectRepository, SubjectRepository>(sp =>
                new SubjectRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<ITestDonorExportRepository>(sp =>
             new TestDonorExportRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<ISearchSetRepository>(sp =>
                new SearchSetRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<ISearchRequestsRepository>(sp =>
                new SearchRequestsRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<IMatchPredictionRequestRepository, MatchPredictionRequestRepository>(sp =>
                new MatchPredictionRequestRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<IMatchPredictionResultsRepository, MatchPredictionResultsRepository>(sp =>
                new MatchPredictionResultsRepository(fetchSqlConnectionString(sp)));
        }

        private static void RegisterServices(
            this IServiceCollection services,
            Func<IServiceProvider, ValidationAzureStorageSettings> fetchValidationAzureStorageSettings,
            Func<IServiceProvider, DataRefreshSettings> fetchDataRefreshSettings)
        {
            services.AddScoped<ISubjectInfoImporter, SubjectInfoImporter>();
            services.AddScoped<IFileReader<ImportedSubject>, FileReader<ImportedSubject>>();
            services.AddScoped<ITestDonorExporter, TestDonorExporter>();
            services.AddScoped<ISearchRequester, SearchRequester>();

            services.AddScoped<IValidationAtlasPreparer, ValidationAtlasPreparer>(sp =>
            {
                var subjectRepo = sp.GetService<ISubjectRepository>();
                var setReader = sp.GetService<IHaplotypeFrequencySetReader>();
                var testDonorExporter = sp.GetService<ITestDonorExporter>();
                var testDonorExportRepo = sp.GetService<ITestDonorExportRepository>();
                var requestUrl = fetchDataRefreshSettings(sp).RequestUrl;
                return new ValidationAtlasPreparer(subjectRepo, setReader, testDonorExporter, testDonorExportRepo, requestUrl);
            });

            services.AddScoped<IMatchPredictionRequester, MatchPredictionRequester>();
            services.AddScoped<IBlobStreamer, BlobStreamer>(sp =>
            {
                var settings = fetchValidationAzureStorageSettings(sp);
                return new BlobStreamer(settings.ConnectionString, sp.GetService<ILogger>());
            });
            services.AddScoped<IResultsProcessor, ResultsProcessor>();
            services.AddScoped<IMessageSender, MessageSender>();
        }
    }
}