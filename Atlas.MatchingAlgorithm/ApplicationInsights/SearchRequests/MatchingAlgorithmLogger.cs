using Atlas.Common.ApplicationInsights;
using Microsoft.ApplicationInsights;

namespace Atlas.MatchingAlgorithm.ApplicationInsights.SearchRequests
{
    public interface IMatchingAlgorithmLogger : ILogger
    {
    }

    public class MatchingAlgorithmLogger : ContextAwareLogger<MatchingAlgorithmLoggingContext>, IMatchingAlgorithmLogger
    {
        public MatchingAlgorithmLogger(
            MatchingAlgorithmLoggingContext loggingContext,
            TelemetryClient client,
            ApplicationInsightsSettings applicationInsightsSettings) : base(loggingContext, client, applicationInsightsSettings)
        {
        }
    }
}