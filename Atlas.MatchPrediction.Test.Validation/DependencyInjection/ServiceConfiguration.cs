using Microsoft.Extensions.DependencyInjection;
using System;
using Atlas.MatchPrediction.Test.Validation.Data.Repositories;
using Atlas.MatchPrediction.Test.Validation.Services;
using Atlas.Common.AzureStorage.Blob;
using Atlas.MatchPrediction.Test.Validation.Models;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.Common.ApplicationInsights;
using Atlas.ManualTesting.Common.SubjectImport;

namespace Atlas.MatchPrediction.Test.Validation.DependencyInjection
{
    internal static class ServiceConfiguration
    {
        public static void RegisterValidationServices(
            this IServiceCollection services,
            Func<IServiceProvider, OutgoingMatchPredictionRequestSettings> fetchOutgoingMatchPredictionRequestSettings,
            Func<IServiceProvider, ValidationAzureStorageSettings> fetchValidationAzureStorageSettings,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessageServiceBusSettings,
            Func<IServiceProvider, MatchPredictionRequestsSettings> fetchMatchPredictionRequestSettings,
            Func<IServiceProvider, string> fetchMatchPredictionValidationSqlConnectionString)
        {
            services.RegisterSettings(fetchOutgoingMatchPredictionRequestSettings, fetchValidationAzureStorageSettings);
            services.RegisterDatabaseServices(fetchMatchPredictionValidationSqlConnectionString);
            services.RegisterServices(fetchValidationAzureStorageSettings);

            services.RegisterMatchPredictionResultsLocationPublisher(fetchMessageServiceBusSettings, fetchMatchPredictionRequestSettings);
        }

        private static void RegisterSettings(
            this IServiceCollection services,
            Func<IServiceProvider, OutgoingMatchPredictionRequestSettings> fetchOutgoingMatchPredictionRequestSettings,
            Func<IServiceProvider, ValidationAzureStorageSettings> fetchValidationAzureStorageSettings)
        {
            services.MakeSettingsAvailableForUse(fetchOutgoingMatchPredictionRequestSettings);
            services.MakeSettingsAvailableForUse(fetchValidationAzureStorageSettings);
        }

        private static void RegisterDatabaseServices(this IServiceCollection services, Func<IServiceProvider, string> fetchSqlConnectionString)
        {
            services.AddScoped<IValidationRepository, ValidationRepository>(sp =>
                new ValidationRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<ISubjectRepository, SubjectRepository>(sp =>
                new SubjectRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<IMatchPredictionRequestRepository, MatchPredictionRequestRepository>(sp =>
                new MatchPredictionRequestRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<IMatchPredictionResultsRepository, MatchPredictionResultsRepository>(sp =>
                new MatchPredictionResultsRepository(fetchSqlConnectionString(sp)));
        }

        private static void RegisterServices(
            this IServiceCollection services,
            Func<IServiceProvider, ValidationAzureStorageSettings> fetchValidationAzureStorageSettings)
        {
            services.AddScoped<ISubjectInfoImporter, SubjectInfoImporter>();
            services.AddScoped<ISubjectInfoReader, SubjectInfoReader>();
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