using System;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.Common.ApplicationInsights;

public static class LoggerRegistration
{
    public static void RegisterAtlasLogger(
        this IServiceCollection services,
        Func<IServiceProvider, ApplicationInsightsSettings> fetchInsightsSettings)
    {
        // Required for logger registration as we must declare a dependency on TelemetryClient.
        // Safe to call multiple times if consumers also want to call this for their own logger implementations.
        services.AddApplicationInsightsTelemetryWorkerService();
            
        services.MakeSettingsAvailableForUse(fetchInsightsSettings);
            
        // If IAtlasLogger has already been registered, then either, it's already been done here
        // (in which case there's no need to repeat), or someone's already registered a more
        // *specific* logger, in which case we actively want to avoid over-writing that registration.
        services.TryAddScoped<IAtlasLogger, AtlasLogger>();

        // Ensure telemetry is flushed on graceful shutdown to prevent data loss.
        // TryAddEnumerable prevents duplicate registrations when RegisterAtlasLogger is called
        // multiple times in the same host (e.g. via chained library registrations), which would
        // otherwise cause multiple 2-second shutdown delays.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, TelemetryFlushService>());

        // Stamp SearchRequestId on all telemetry items for end-to-end correlation in Application Insights.
        // TryAddEnumerable prevents duplicate initializer registrations for the same reason as above.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITelemetryInitializer, SearchRequestTelemetryInitializer>());
    }
}