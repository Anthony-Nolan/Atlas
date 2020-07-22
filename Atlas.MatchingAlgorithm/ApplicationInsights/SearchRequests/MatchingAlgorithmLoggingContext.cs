using System;
using System.Collections.Generic;
using Atlas.Common.ApplicationInsights;

namespace Atlas.MatchingAlgorithm.ApplicationInsights.SearchRequests
{
    public class MatchingAlgorithmLoggingContext : LoggingContext
    {
        private string searchRequestId;

        public string SearchRequestId
        {
            get => searchRequestId;
            set
            {
                if (!string.IsNullOrEmpty(searchRequestId))
                {
                    throw new InvalidOperationException(
                        $"Cannot set {nameof(SearchRequestId)} to '{value}' as it is already set to '{searchRequestId}'.");
                }

                searchRequestId = value;
            }
        }

        /// <inheritdoc />
        public override Dictionary<string, string> PropertiesToLog()
        {
            return new Dictionary<string, string>
            {
                {nameof(SearchRequestId), SearchRequestId}
            };
        }
    }
}