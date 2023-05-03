using System.Collections.Generic;

namespace Atlas.Common.ApplicationInsights
{
    public class SearchLoggingContext : LoggingContext
    {
        public string SearchRequestId { get; set; }

        public override Dictionary<string, string> PropertiesToLog()
        {
            return new Dictionary<string, string>
            {
                {nameof(SearchRequestId), SearchRequestId}
            };
        }
    }
}
