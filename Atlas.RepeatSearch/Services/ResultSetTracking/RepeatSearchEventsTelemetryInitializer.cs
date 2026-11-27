using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Atlas.RepeatSearch.Services.ResultSetTracking;

/// <summary>
/// Keeps the "Original search results stored" and "Expired repeat search result sets deleted" events out of sampling: the worker's
/// TelemetryClient samples events, and the <c>excludedTypes</c> of host.json apply only to the host process. The samplers of the SDK do not
/// sample an item whose sampling percentage is set already, and initializers run before them.
/// </summary>
internal class RepeatSearchEventsTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is EventTelemetry
            {
                Name: OriginalSearchResultSetTracker.StoredEventName or ExpiredResultSetCleaner.DeletedEventName
            } eventTelemetry)
        {
            ((ISupportSampling)eventTelemetry).SamplingPercentage = 100;
        }
    }
}
