using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Atlas.RepeatSearch.Services.ResultSetTracking;

/// <summary>
/// Keeps the "Original search results stored" events out of sampling: the worker's TelemetryClient samples events, and the
/// <c>excludedTypes</c> of host.json apply only to the host process. The samplers of the SDK do not sample an item whose sampling
/// percentage is set already, and initializers run before them.
/// </summary>
internal class OriginalSearchResultsStoredTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is EventTelemetry eventTelemetry && eventTelemetry.Name == OriginalSearchResultSetTracker.StoredEventName)
        {
            ((ISupportSampling)eventTelemetry).SamplingPercentage = 100;
        }
    }
}
