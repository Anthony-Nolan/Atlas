using Atlas.Client.Models.Search.Results;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.ServiceBus;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.ExternalInterface.DependencyInjection;
using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.ManualTesting.Common.Repositories;
using Atlas.ManualTesting.Common.Services;
using Atlas.ManualTesting.Common.Services.Storers;
using Atlas.ManualTesting.Common.Settings;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Atlas.MatchPrediction.Test.Validation.Data.Repositories;
using Atlas.MatchPrediction.Test.Validation.Models;
using Atlas.MatchPrediction.Test.Validation.Services;
using Atlas.MatchPrediction.Test.Validation.Services.Exercise3;
using Atlas.MatchPrediction.Test.Validation.Services.Exercise4;
using Atlas.MatchPrediction.Test.Validation.Settings;
using Microsoft.Extensions.DependencyInjection;
using System;
using Atlas.MatchPrediction.Test.Validation.Data.Repositories.Homework;
using Atlas.MatchPrediction.Test.Validation.Services.Exercise4.Homework;

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

            services.RegisterServices(
                fetchValidationAzureStorageSettings, fetchDataRefreshSettings, fetchMessageServiceBusSettings, fetchValidationSearchSettings);
            
            services.RegisterHaplotypeFrequenciesReader(fetchMatchPredictionSqlConnectionString);
            services.RegisterImportDatabaseTypes(fetchDonorImportSqlConnectionString);
            services.RegisterMatchPredictionResultsLocationPublisher(fetchMessageServiceBusSettings, fetchMatchPredictionRequestSettings);
            
            services.RegisterHomeworkServices(fetchMatchPredictionValidationSqlConnectionString);
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
            services.AddScoped<ISearchRequestsRepository<ValidationSearchRequestRecord>>(sp =>
                new SearchRequestsRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<IMatchedDonorsRepository, MatchedDonorsRepository>(sp =>
                new MatchedDonorsRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<IProcessedResultsRepository<MatchedDonor>, MatchedDonorsRepository>(sp =>
                new MatchedDonorsRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<IProcessedResultsRepository<LocusMatchDetails>, LocusMatchDetailsRepository>(sp =>
                new LocusMatchDetailsRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<IProcessedResultsRepository<MatchedDonorProbability>, MatchProbabilitiesRepository>(sp =>
                new MatchProbabilitiesRepository(fetchSqlConnectionString(sp)));

            services.AddScoped<IMatchPredictionRequestRepository, MatchPredictionRequestRepository>(sp =>
                new MatchPredictionRequestRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<IMatchPredictionResultsRepository, MatchPredictionResultsRepository>(sp =>
                new MatchPredictionResultsRepository(fetchSqlConnectionString(sp)));
        }

        private static void RegisterServices(
            this IServiceCollection services,
            Func<IServiceProvider, ValidationAzureStorageSettings> fetchValidationAzureStorageSettings,
            Func<IServiceProvider, DataRefreshSettings> fetchDataRefreshSettings,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessageServiceBusSettings,
            Func<IServiceProvider, ValidationSearchSettings> fetchValidationSearchSettings)
        {
            services.AddScoped<ISubjectInfoImporter, SubjectInfoImporter>();
            services.AddScoped(typeof(IFileReader<>), typeof(FileReader<>));
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

            services.AddScoped<IResultSetProcessor<SearchResultsNotification>, SearchResultSetProcessor>();
            services.AddScoped<IResultsStorer<SearchResult, MatchedDonor>, SearchResultDonorStorer>();
            services.AddScoped<IResultsStorer<SearchResult, LocusMatchDetails>, SearchLocusDetailsStorer>();
            services.AddScoped<IResultsStorer<SearchResult, MatchedDonorProbability>, MatchedDonorProbabilitiesStorer>();

            services.AddScoped<IMatchPredictionRequester, MatchPredictionRequester>();
            services.AddScoped<IBlobStreamer, BlobStreamer>(sp =>
            {
                var settings = fetchValidationAzureStorageSettings(sp);
                return new BlobStreamer(settings.ConnectionString, sp.GetService<ILogger>());
            });
            services.AddScoped<IMatchPredictionResultsProcessor, MatchPredictionResultsProcessor>();
            services.AddScoped<IMatchPredictionLocationSender, MatchPredictionLocationSender>();

            services.AddScoped<IMessageBatchPublisher<SearchResultsNotification>, MessageBatchPublisher<SearchResultsNotification>>(sp =>
            {
                var messageSettings = fetchMessageServiceBusSettings(sp);
                var searchSettings = fetchValidationSearchSettings(sp);
                return new MessageBatchPublisher<SearchResultsNotification>(messageSettings.ConnectionString, searchSettings.ResultsTopic);
            });

            services.AddScoped<ISearchResultNotificationSender, SearchResultNotificationSender>(sp =>
            {
                var publisher = sp.GetService<IMessageBatchPublisher<SearchResultsNotification>>();
                var storageSettings = fetchValidationAzureStorageSettings(sp);
                return new SearchResultNotificationSender(publisher, storageSettings.SearchResultsBlobContainer);
            });
        }

        private static void RegisterHomeworkServices(
            this IServiceCollection services,
            Func<IServiceProvider, string> fetchSqlConnectionString)
        {
            services.AddScoped<IHomeworkDeletionRepository, HomeworkDeletionRepository>(sp =>
                new HomeworkDeletionRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<IHomeworkSetRepository, HomeworkSetRepository>(sp =>
                new HomeworkSetRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<IPatientDonorPairRepository, PatientDonorPairRepository>(sp =>
                new PatientDonorPairRepository(fetchSqlConnectionString(sp)));

            services.AddScoped<IHomeworkCreator, HomeworkCreator>();
            services.AddScoped<IHomeworkProcessor, HomeworkProcessor>();
            services.AddScoped<IPatientDonorPairProcessor, PatientDonorPairProcessor>();
        }
    }
}