using Atlas.Common.ApplicationInsights;
using Microsoft.ApplicationInsights;

namespace Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging
{
    public interface IMatchingAlgorithmImportLogger : IAtlasLogger
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