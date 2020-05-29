using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Data.Context;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Settings.Azure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Atlas.MatchPrediction.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterMatchPredictionServices(this IServiceCollection services)
        {
            services.RegisterSettings();
            services.RegisterAtlasLogger(sp => sp.GetService<IOptions<ApplicationInsightsSettings>>().Value);
            services.RegisterServices();
            services.RegisterDatabaseServices();
            services.RegisterClientServices();
        }

        private static void RegisterSettings(this IServiceCollection services)
        {
            services.RegisterOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterOptions<AzureStorageSettings>("AzureStorage");
            services.RegisterOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");
        }

        private static void RegisterDatabaseServices(this IServiceCollection services)
        {
            services.AddDbContext<MatchPredictionContext>((sp, options) =>
            {
                var connString = GetSqlConnectionString(sp);
                options.UseSqlServer(connString);
            });

            services.AddScoped<IHaplotypeFrequencySetRepository, HaplotypeFrequencySetRepository>();
            services.AddScoped<IHaplotypeFrequenciesRepository, HaplotypeFrequenciesRepository>(sp =>
                new HaplotypeFrequenciesRepository(GetSqlConnectionString(sp))
            );
        }

        private static void RegisterClientServices(this IServiceCollection services)
        {
            services.AddScoped<INotificationsClient, NotificationsClient>();
        }

        private static void RegisterServices(this IServiceCollection services)
        {

            services.AddScoped<IFrequencySetMetadataExtractor, FrequencySetMetadataExtractor>();
            services.AddScoped<IFrequencySetImporter, FrequencySetImporter>();
            services.AddScoped<IFrequencyCsvReader, FrequencyCsvReader>();
            services.AddScoped<IFrequencySetService, FrequencySetService>();

            services.AddScoped<IGenotypeLikelihoodService, GenotypeLikelihoodService>();
            services.AddScoped<IGenotypeImputer, GenotypeImputer>();
        }

        private static string GetSqlConnectionString(IServiceProvider sp)
        {
            return sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["Sql"];
        }
    }
}