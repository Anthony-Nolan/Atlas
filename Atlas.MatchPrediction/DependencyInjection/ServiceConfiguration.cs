using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Settings.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Atlas.MatchPrediction.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterFunctionsAppSettings(this IServiceCollection services)
        {
            services.RegisterOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterOptions<AzureStorageSettings>("AzureStorage");
            services.RegisterOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");
        }

        public static void RegisterMatchPredictionServices(this IServiceCollection services)
        {
            services.AddScoped<ILogger>(sp =>
            {
                var settings = sp.GetService<IOptions<ApplicationInsightsSettings>>().Value;
                return LoggerRegistration.BuildLogger(settings.InstrumentationKey);
            });

            services.AddScoped<INotificationsClient, NotificationsClient>(sp =>
            {
                var settings = sp.GetService<IOptions<NotificationsServiceBusSettings>>().Value;
                return new NotificationsClient(settings);
            });

            services.AddScoped<IHaplotypeFrequencySetMetaDataService, HaplotypeFrequencySetMetaDataService>();
            services.AddScoped<IHaplotypeFrequencySetImportService, HaplotypeFrequencySetImportService>();
            services.AddScoped<IHaplotypeFrequencySetImportRepository, HaplotypeFrequencySetImportRepository>();
            services.AddScoped<IFailedImportNotificationSender, FailedImportNotificationSender>();
        }
    }
}
