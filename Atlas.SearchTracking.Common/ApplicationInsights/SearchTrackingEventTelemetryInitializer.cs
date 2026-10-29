using Atlas.SearchTracking.Common.Enums;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Atlas.SearchTracking.Common.ApplicationInsights;

/// <summary>
/// Keeps the search tracking events out of sampling. They are the records of the searches, so an event that sampling drops is a
/// lost record. The samplers of the SDK do not sample an item whose sampling percentage is set already, and initializers run before
/// them.
/// </summary>
internal class SearchTrackingEventTelemetryInitializer : ITelemetryInitializer
{
    private static readonly HashSet<string> EventNames = Enum.GetNames<SearchTrackingEventType>().ToHashSet();

    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is EventTelemetry eventTelemetry && EventNames.Contains(eventTelemetry.Name))
        {
            ((ISupportSampling)eventTelemetry).SamplingPercentage = 100;
        }
    }
}
