using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Atlas.Common.ApplicationInsights
{
    /// <summary>
    /// Keeps the <see cref="ResultsPipelineTimings"/> events out of sampling (ATL-526 experiment, not for release). The samplers of the
    /// SDK do not sample an item whose sampling percentage is set already, and initializers run before them.
    /// </summary>
    public class ResultsPipelineTimingsTelemetryInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry is EventTelemetry eventTelemetry && eventTelemetry.Name == ResultsPipelineTimings.EventName)
            {
                ((ISupportSampling)eventTelemetry).SamplingPercentage = 100;
            }
        }
    }
}
