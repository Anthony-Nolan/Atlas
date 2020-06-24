using System;
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
            // Required for logger registration as we must declare a dependency on TelemetryClient.
            // Safe to call multiple times if consumers also want to call this for their own logger implementations.
            services.AddApplicationInsightsTelemetryWorkerService();
            
            // Ensure settings available when creating Logger
            services.AddScoped(fetchInsightsSettings);
            
            // If ILogger has already been registered, then either, it's already been done here
            // (in which case there's no need to repeat), or someone's already registered a more
            // *specific* logger, in which case we actively want to avoid over-writing that registration.
            services.TryAddScoped<ILogger, Logger>();
        }
    }
}