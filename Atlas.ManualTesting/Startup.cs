using Atlas.ManualTesting;
using Atlas.ManualTesting.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.ManualTesting
{
    internal static class Startup
    {
        public static void Configure(IServiceCollection services)
        {
            // Stops the Visual Studio debug window from being flooded with not-very-helpful AI telemetry messages!
            TelemetryDebugWriter.IsTracingDisabled = true;

            services.RegisterServices();
        }
    }
}
