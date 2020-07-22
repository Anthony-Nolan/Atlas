using System;

namespace Atlas.MatchingAlgorithm.ApplicationInsights.SearchRequests
{
    public interface ISearchRequestLoggingContext
    {
        string SearchRequestId { get; set; }
    }

    public class SearchRequestLoggingContext : ISearchRequestLoggingContext
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
    }
}
