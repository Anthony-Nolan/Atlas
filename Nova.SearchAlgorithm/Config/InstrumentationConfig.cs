using Microsoft.ApplicationInsights.Extensibility;
using Owin;

namespace Nova.SearchAlgorithm.Config
{
    public static class InstrumentationConfig
    {
        public static void SetUpInstrumentation(this IAppBuilder app, string instrumentationKey)
        {
            if (instrumentationKey != null)
            {
                TelemetryConfiguration.Active.InstrumentationKey = instrumentationKey;
            }
        }
    }
}