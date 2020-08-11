using Atlas.Common.ApplicationInsights;
using Microsoft.ApplicationInsights;

namespace Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging
{
    public interface IMatchingAlgorithmImportLogger : ILogger
    {
    }

    public class MatchingAlgorithmImportLogger : ContextAwareLogger<MatchingAlgorithmImportLoggingContext>, IMatchingAlgorithmImportLogger
    {
        public MatchingAlgorithmImportLogger(
            MatchingAlgorithmImportLoggingContext loggingContext,
            TelemetryClient client,
            ApplicationInsightsSettings applicationInsightsSettings) : base(loggingContext, client, applicationInsightsSettings)
        {
        }
    }
}