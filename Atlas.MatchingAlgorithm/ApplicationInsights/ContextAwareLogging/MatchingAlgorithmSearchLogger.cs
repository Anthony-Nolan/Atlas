using Atlas.Common.ApplicationInsights;
using Microsoft.ApplicationInsights;

namespace Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;

public interface IMatchingAlgorithmSearchLogger : IAtlasLogger
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