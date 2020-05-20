using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace Atlas.Common.ApplicationInsights
{
    public static class LoggerRegistration
    {
        public static Logger BuildAtlasLogger(string instrumentationKey)
        {
            var telemetryConfig = new TelemetryConfiguration
            {
                InstrumentationKey = instrumentationKey
            };
            return new Logger(new TelemetryClient(telemetryConfig), LogLevel.Info);
        }
    }
}
