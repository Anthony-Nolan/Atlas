using System.Collections.Generic;
using Atlas.Common.ApplicationInsights;
using Microsoft.ApplicationInsights;

namespace Atlas.MatchingAlgorithm.ApplicationInsights.SearchRequests
{
    public class SearchRequestAwareLogger : Logger
    {
        private const string SearchRequestIdPropName = "SearchRequestId";

        private readonly ISearchRequestLoggingContext loggingContext;

        public SearchRequestAwareLogger(
            ISearchRequestLoggingContext loggingContext,
            TelemetryClient client, 
            ApplicationInsightsSettings applicationInsightsSettings) : base(client, applicationInsightsSettings)
        {
            this.loggingContext = loggingContext;
        }

        public override void SendEvent(EventModel eventModel)
        {
            var searchRequestId = loggingContext.SearchRequestId;

            if (!string.IsNullOrEmpty(searchRequestId))
            {
                eventModel.Properties.Add(SearchRequestIdPropName, searchRequestId);
            }

            base.SendEvent(eventModel);
        }

        public override void SendTrace(string message, LogLevel messageLogLevel, Dictionary<string, string> props)
        {
            props ??= new Dictionary<string, string>();

            var searchRequestId = loggingContext.SearchRequestId;
            if (!string.IsNullOrEmpty(searchRequestId))
            {
                props.Add(SearchRequestIdPropName, searchRequestId);
            }

            base.SendTrace(message, messageLogLevel, props);
        }
    }
}
