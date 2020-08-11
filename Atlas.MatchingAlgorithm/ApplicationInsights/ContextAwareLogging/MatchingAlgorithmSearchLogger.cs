using Atlas.Common.ApplicationInsights;
using Microsoft.ApplicationInsights;

namespace Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging
{
    public interface IMatchingAlgorithmSearchLogger : ILogger
    {
    }

    public class MatchingAlgorithmSearchLogger : ContextAwareLogger<MatchingAlgorithmSearchLoggingContext>, IMatchingAlgorithmSearchLogger
    {
        public MatchingAlgorithmSearchLogger(
            MatchingAlgorithmSearchLoggingContext loggingContext,
            TelemetryClient client,
            ApplicationInsightsSettings applicationInsightsSettings) : base(loggingContext, client, applicationInsightsSettings)
        {
        }
    }
}