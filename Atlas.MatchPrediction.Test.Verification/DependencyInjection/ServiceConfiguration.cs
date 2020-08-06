using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MatchPrediction.Test.Verification.Data.Context;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Services;
using Atlas.MatchPrediction.Test.Verification.Services.GenotypeSimulation;
using Atlas.MatchPrediction.Test.Verification.Settings;
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
            Func<IServiceProvider, string> fetchMatchPredictionSqlConnectionString
        )
        {
            services.RegisterSettings();
            services.RegisterDatabaseServices(fetchMatchPredictionVerificationSqlConnectionString);
            services.RegisterServices(fetchMatchPredictionSqlConnectionString);
            services.RegisterHaplotypeFrequenciesReader(fetchMatchPredictionSqlConnectionString);
        }

        private static void RegisterSettings(this IServiceCollection services)
        {
            services.RegisterAsOptions<MatchPredictionAzureStorageSettings>("AzureStorage");
        }

        private static void RegisterDatabaseServices(this IServiceCollection services, Func<IServiceProvider, string> fetchSqlConnectionString)
        {
            services.AddScoped<INormalisedPoolRepository, NormalisedPoolRepository>(sp =>
                new NormalisedPoolRepository(fetchSqlConnectionString(sp)));
            services.AddScoped<ISimulantsRepository, SimulantsRepository>(sp =>
                new SimulantsRepository(fetchSqlConnectionString(sp)));

            services.AddScoped(sp => new ContextFactory().Create(fetchSqlConnectionString(sp)));
            services.AddScoped<ITestHarnessRepository, TestHarnessRepository>();
        }

        private static void RegisterServices(
            this IServiceCollection services, 
            Func<IServiceProvider, string> fetchMatchPredictionSqlConnectionString)
        {
            services.AddScoped<IHaplotypeFrequenciesReader, HaplotypeFrequenciesReader>();
            services.AddScoped<IFrequencySetStreamer, FrequencySetStreamer>();

            services.AddScoped<INormalisedPoolGenerator, NormalisedPoolGenerator>(sp =>
                {
                    var reader = sp.GetService<IHaplotypeFrequenciesReader>();
                    var repo = sp.GetService<INormalisedPoolRepository>();
                    var dataSource = new SqlConnectionStringBuilder(fetchMatchPredictionSqlConnectionString(sp)).DataSource;
                    return new NormalisedPoolGenerator(reader, repo, dataSource);
                });
            services.AddScoped<IGenotypeSimulator, GenotypeSimulator>();
            services.AddScoped<IRandomNumberPairGenerator, RandomNumberPairGenerator>();
            services.AddScoped<ISimulantsGenerator, SimulantsGenerator>();
            services.AddScoped<ITestHarnessGenerator, TestHarnessGenerator>();
        }
    }
}