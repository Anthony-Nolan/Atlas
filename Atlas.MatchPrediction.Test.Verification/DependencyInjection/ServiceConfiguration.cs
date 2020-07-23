using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Services;
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
            services.RegisterDatabaseServices(fetchMatchPredictionVerificationSqlConnectionString);
            services.RegisterHaplotypeFrequenciesReader(fetchMatchPredictionSqlConnectionString);
            services.RegisterServices();
        }

        private static void RegisterDatabaseServices(this IServiceCollection services, Func<IServiceProvider, string> fetchSqlConnectionString)
        {
            services.AddTransient<INormalisedPoolRepository, NormalisedPoolRepository>(sp =>
                new NormalisedPoolRepository(fetchSqlConnectionString(sp))
            );
        }

        private static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<INormalisedPoolGenerator, NormalisedPoolGenerator>();
        }
    }
}