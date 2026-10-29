using Atlas.SearchTracking.Common.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Atlas.SearchTracking.Common.DependencyInjection;

public static class SearchTrackingTelemetryRegistration
{
    /// <summary>
    /// Keeps the search tracking events that the app sends to Application Insights out of sampling. Call it in each app that sends
    /// them with <see cref="SearchTrackingEventLoggerExtensions.SendSearchTrackingEvent{TEvent}"/>.
    /// </summary>
    public static IServiceCollection RegisterSearchTrackingEventTelemetry(this IServiceCollection services)
    {
        // TryAddEnumerable, because an app can call this from more than one registration.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITelemetryInitializer, SearchTrackingEventTelemetryInitializer>());

        return services;
    }
}
