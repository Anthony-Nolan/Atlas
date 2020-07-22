using Atlas.Common.ApplicationInsights;
using Microsoft.ApplicationInsights;

namespace Atlas.MatchPrediction.ApplicationInsights
{
    internal interface IMatchPredictionLogger : ILogger
    {
    }

    internal class MatchPredictionLogger : ContextAwareLogger<MatchPredictionLoggingContext>, IMatchPredictionLogger
    {
        /// <inheritdoc />
        public MatchPredictionLogger(
            MatchPredictionLoggingContext loggingContext,
            TelemetryClient client,
            ApplicationInsightsSettings applicationInsightsSettings) : base(loggingContext, client, applicationInsightsSettings)
        {
        }
    }
}