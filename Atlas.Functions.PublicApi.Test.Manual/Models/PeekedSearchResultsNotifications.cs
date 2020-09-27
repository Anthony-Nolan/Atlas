using System.Collections.Generic;
using Atlas.Client.Models.Search.Results;

namespace Atlas.Functions.PublicApi.Test.Manual.Models
{
    public class PeekedSearchResultsNotifications
    {
        public int TotalNotificationCount { get; set; }
        public int UniqueSearchRequestIdCount { get; set; }
        public int WasSuccessfulCount { get; set; }
        public int WasNotSuccessfulCount => TotalNotificationCount - WasSuccessfulCount;
        public IDictionary<string, int> FailureInfo { get; set; }
        public IEnumerable<SearchResultsNotification> PeekedNotifications { get; set; }
    }
}
