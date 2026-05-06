using System.Collections.Generic;

namespace Atlas.Common.ApplicationInsights
{
    public class SearchLoggingContext : LoggingContext
    {
        private string searchRequestId;

        public string SearchRequestId
        {
            get => searchRequestId;
            set
            {
                searchRequestId = value;
                SearchRequestContext.SearchRequestId = value;
            }
        }

        public override Dictionary<string, string> PropertiesToLog()
        {
            return new Dictionary<string, string>
            {
                {nameof(SearchRequestId), SearchRequestId}
            };
        }
    }
}
