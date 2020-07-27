using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using Atlas.MatchPrediction.Test.Verification.Data.Context;

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
            services.RegisterDatabaseServices(fetchMatchPredictionVerificationSqlConnectionString);
            services.RegisterHaplotypeFrequenciesReader(fetchMatchPredictionSqlConnectionString);
            services.RegisterServices();
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

        private static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<INormalisedPoolGenerator, NormalisedPoolGenerator>();
            services.AddScoped<IGenotypeSimulator, GenotypeSimulator>();
            services.AddScoped<IRandomNumberPairGenerator, RandomNumberPairGenerator>();
            services.AddScoped<ITestHarnessGenerator, TestHarnessGenerator>();
        }
    }
}