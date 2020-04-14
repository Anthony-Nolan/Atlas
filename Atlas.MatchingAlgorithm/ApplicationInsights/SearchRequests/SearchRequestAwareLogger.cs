using Microsoft.ApplicationInsights;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.ApplicationInsights.EventModels;
using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.ApplicationInsights.SearchRequests
{
    public class SearchRequestAwareLogger : Logger
    {
        private const string SearchRequestIdPropName = "SearchRequestId";

        private readonly ISearchRequestContext context;

        public SearchRequestAwareLogger(
            ISearchRequestContext context,
            TelemetryClient client, 
            LogLevel logLevel) : base(client, logLevel)
        {
            this.context = context;
        }

        public override void SendEvent(EventModel eventModel)
        {
            var searchRequestId = context.SearchRequestId;

            if (!string.IsNullOrEmpty(searchRequestId))
            {
                eventModel.Properties.Add(SearchRequestIdPropName, searchRequestId);
            }

            base.SendEvent(eventModel);
        }

        public override void SendTrace(string message, LogLevel messageLogLevel, Dictionary<string, string> props)
        {
            props = props ?? new Dictionary<string, string>();

            var searchRequestId = context.SearchRequestId;
            if (!string.IsNullOrEmpty(searchRequestId))
            {
                props.Add(SearchRequestIdPropName, searchRequestId);
            }

            base.SendTrace(message, messageLogLevel, props);
        }
    }
}
