using Atlas.Common.ApplicationInsights;
using Microsoft.ApplicationInsights;

namespace Atlas.MatchPrediction.ApplicationInsights
{
    // ReSharper disable once UnusedTypeParameter
    public interface IMatchPredictionLogger<TLoggingContext> : ILogger
        where TLoggingContext : MatchProbabilityLoggingContext
    {
    }

    public class MatchPredictionLogger<TLoggingContext> : ContextAwareLogger<TLoggingContext>, IMatchPredictionLogger<TLoggingContext>
        where TLoggingContext : MatchProbabilityLoggingContext
    {
        /// <inheritdoc />
        public MatchPredictionLogger(
            TLoggingContext loggingContext,
            TelemetryClient client,
            ApplicationInsightsSettings applicationInsightsSettings) : base(loggingContext, client, applicationInsightsSettings)
        {
        }
    }
}