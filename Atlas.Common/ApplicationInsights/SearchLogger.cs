using Microsoft.ApplicationInsights;

namespace Atlas.Common.ApplicationInsights
{
    public interface ISearchLogger<TLoggingContext> : ILogger
        where TLoggingContext : SearchLoggingContext
    {
    }


    public class SearchLogger<TLoggingContext> : ContextAwareLogger<TLoggingContext>, ISearchLogger<TLoggingContext>
        where TLoggingContext : SearchLoggingContext
    {
        public SearchLogger(TLoggingContext loggingContext, TelemetryClient client, ApplicationInsightsSettings applicationInsightsSettings)
            : base(loggingContext, client, applicationInsightsSettings)
        {
        }
    }
}
