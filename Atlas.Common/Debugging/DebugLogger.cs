using Atlas.Common.ApplicationInsights;
using Microsoft.ApplicationInsights;

namespace Atlas.Common.Debugging
{
    /// <summary>
    /// Basic logger for debug purposes.
    /// </summary>
    public interface IDebugLogger : ILogger
    {
    }

    public class DebugLogger : Logger, IDebugLogger
    {
        /// <inheritdoc />
        public DebugLogger(TelemetryClient client, ApplicationInsightsSettings applicationInsightsSettings) : base(client, applicationInsightsSettings)
        {
        }
    }
}