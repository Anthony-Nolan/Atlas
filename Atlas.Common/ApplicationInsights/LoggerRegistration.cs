using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Atlas.Common.ApplicationInsights
{
    public static class LoggerRegistration
    {
        public static void RegisterAtlasLogger(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchInsightsSettings)
        {
            // If ILogger has already been registered, then either, it's already been done here
            // (in which case there's no need to repeat), or someone's already registered a more
            // *specific* logger, in which case we actively want to avoid over-writing that registration.
            services.TryAddScoped<ILogger>(sp =>
            {
                var settings = fetchInsightsSettings(sp);
                return BuildLogger(settings);
            });
        }

        public static Logger BuildLogger(ApplicationInsightsSettings settings)
        {
            var telemetryConfig = new TelemetryConfiguration
            {
                InstrumentationKey = settings.InstrumentationKey
            };

            return new Logger(new TelemetryClient(telemetryConfig), settings.LogLevel.ToLogLevel());
        }
    }
}
