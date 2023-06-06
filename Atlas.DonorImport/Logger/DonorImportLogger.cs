using Atlas.Common.ApplicationInsights;
using Microsoft.ApplicationInsights;

namespace Atlas.DonorImport.Logger
{
    public interface IDonorImportLogger<TLoggingContext> : ILogger
        where TLoggingContext : DonorImportLoggingContext
    {
    }

    public class DonorImportLogger<TLoggingContext> : ContextAwareLogger<TLoggingContext>, IDonorImportLogger<TLoggingContext>
        where TLoggingContext : DonorImportLoggingContext
    {
        public DonorImportLogger(TLoggingContext loggingContext, TelemetryClient client, ApplicationInsightsSettings applicationInsightsSettings)
            : base(loggingContext, client, applicationInsightsSettings)
        {
        }
    }
}
