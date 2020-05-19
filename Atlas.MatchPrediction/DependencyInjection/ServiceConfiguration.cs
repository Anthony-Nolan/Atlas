using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Services;
using Atlas.MatchPrediction.Settings.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MatchPrediction.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterMatchPredictionServices(this IServiceCollection services)
        {
            services.AddScoped<IHaplotypeFrequencySetService, HaplotypeFrequencySetService>();
            services.AddScoped<IHaplotypeFrequencySetImportRepository, HaplotypeFrequencySetImportRepository>();
        }

        public static void RegisterFunctionsAppSettings(this IServiceCollection services)
        {
            services.RegisterOptions<AzureStorageSettings>("AzureStorage");
        }
    }
}
