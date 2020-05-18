using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace Atlas.Common.ApplicationInsights
{
    public static class LoggerRegistration
    {
        public static Logger BuildNovaLogger(string instrumentationKey)
        {
            var telemetryConfig = new TelemetryConfiguration
            {
                InstrumentationKey = instrumentationKey
            };
            var logger = new Logger(new TelemetryClient(telemetryConfig), LogLevel.Info);
            return logger;
        }
    }
}
