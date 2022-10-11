using Atlas.ManualTesting;
using Atlas.ManualTesting.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.ManualTesting
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Stops the Visual Studio debug window from being flooded with not-very-helpful AI telemetry messages!
            TelemetryDebugWriter.IsTracingDisabled = true;

            builder.Services.RegisterServices();
        }
    }
}
