using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Hosting;

namespace Atlas.Common.ApplicationInsights
{
    /// <summary>
    /// Ensures all buffered telemetry is flushed to Application Insights on graceful shutdown.
    /// Without this, Azure Functions may drop telemetry during scale-in or deployment.
    /// </summary>
    public class TelemetryFlushService : IHostedService
    {
        private readonly TelemetryClient telemetryClient;

        public TelemetryFlushService(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            telemetryClient.Flush();

            // Give the channel time to transmit buffered telemetry before the process exits.
            await Task.Delay(2000, cancellationToken).ConfigureAwait(false);
        }
    }
}

