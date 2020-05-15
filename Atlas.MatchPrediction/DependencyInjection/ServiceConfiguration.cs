using System.ComponentModel;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Services;
using Atlas.MatchPrediction.Settings.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Atlas.MatchPrediction.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterMatchPredictionTypes(this IServiceCollection services)
        {
            services.AddScoped<IHaplotypeFrequencySetService, HaplotypeFrequencySetService>();
            services.AddScoped<IHaplotypeFrequencySetImportRepository, HaplotypeFrequencySetImportRepository>();
        }

        public static void RegisterSettingsForFunction(this IServiceCollection services)
        {
            services.ManuallyRegisterSettings<AzureStorageSettings>(("AzureStorage"));
        }

        private static void ManuallyRegisterSettings<TSettings>(this IServiceCollection services, string configPrefix = "") where TSettings : class, new()
        {
            services.AddSingleton<IOptions<TSettings>>(sp =>
            {
                var config = sp.GetService<IConfiguration>();
                return new OptionsWrapper<TSettings>(BuildSettings<TSettings>(config, configPrefix));
            });
        }

        private static TSettings BuildSettings<TSettings>(IConfiguration config, string configPrefix) where TSettings : class, new()
        {
            var settings = new TSettings();

            var properties = typeof(TSettings).GetProperties();
            foreach (var property in properties)
            {
                var stringValue = config.GetSection($"{configPrefix}:{property.Name}")?.Value;
                var converterForPropertyType = TypeDescriptor.GetConverter(property.PropertyType);
                var typedValue = converterForPropertyType.ConvertFrom(stringValue);
                property.SetValue(settings, typedValue);
            }

            return settings;
        }
    }
}
