using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Common.ServiceBus;
using Atlas.Debug.Client.Models.ServiceBus;
using Atlas.ManualTesting.Models;

namespace Atlas.ManualTesting.Services
{
    public interface ISearchResultNotificationsPeeker
    {
        Task<IEnumerable<string>> GetIdsOfFailedSearches(PeekServiceBusMessagesRequest peekRequest);
        Task<PeekedSearchResultsNotifications> GetSearchResultsNotifications(PeekServiceBusMessagesRequest peekRequest);
        Task<PeekedSearchResultsNotifications> GetNotificationsBySearchRequestId(PeekBySearchRequestIdRequest peekRequest);
    }

    internal class SearchResultNotificationsPeeker : ISearchResultNotificationsPeeker
    {
        private readonly IMessagesPeeker<SearchResultsNotification> messagesReceiver;

        public SearchResultNotificationsPeeker(IMessagesPeeker<SearchResultsNotification> messagesReceiver)
        {
            this.messagesReceiver = messagesReceiver;
        }

        public async Task<IEnumerable<string>> GetIdsOfFailedSearches(PeekServiceBusMessagesRequest peekRequest)
        {
            var notifications = await messagesReceiver.Peek(peekRequest);

            return notifications
                .Where(n => !n.DeserializedBody.WasSuccessful)
                .Select(n => n.DeserializedBody.SearchRequestId)
                .Distinct();
        }

        public async Task<PeekedSearchResultsNotifications> GetSearchResultsNotifications(PeekServiceBusMessagesRequest peekRequest)
        {
            var notifications = await PeekNotifications(peekRequest);

            return BuildResponse(notifications);
        }

        public async Task<PeekedSearchResultsNotifications> GetNotificationsBySearchRequestId(PeekBySearchRequestIdRequest peekRequest)
        {
            var notifications = (await PeekNotifications(peekRequest))
                .Where(n => string.Equals(n.SearchRequestId,  peekRequest.SearchRequestId))
                .ToList();

            return BuildResponse(notifications);
        }

        private async Task<IReadOnlyCollection<SearchResultsNotification>> PeekNotifications(PeekServiceBusMessagesRequest peekRequest)
        {
            return (await messagesReceiver.Peek(peekRequest))
                .Select(m => m.DeserializedBody)
                .ToList();
        }

        private static PeekedSearchResultsNotifications BuildResponse(IReadOnlyCollection<SearchResultsNotification> notifications)
        {
            return new PeekedSearchResultsNotifications
            {
                TotalNotificationCount = notifications.Count,
                WasSuccessfulCount = notifications.Count(n => n.WasSuccessful),
                FailureInfo = notifications
                    .Where(n => !n.WasSuccessful)
                    .GroupBy(n => n.FailureInfo?.Summary)
                    .ToDictionary(grp => grp.Key, grp => grp.Count()),
                UniqueSearchRequestIdCount = notifications.Select(n => n.SearchRequestId).Distinct().Count(),
                SearchTimesInSeconds = ExtractSearchTimes(notifications),
                PeekedNotifications = notifications
            };
        }

        private static SearchTimes ExtractSearchTimes(IReadOnlyCollection<SearchResultsNotification> notifications)
        {
            return new SearchTimes
            {
                MatchingAlgorithm = CalculateSearchTimesInSeconds(notifications, n => n.MatchingAlgorithmTime),
                MatchPrediction = CalculateSearchTimesInSeconds(notifications,n => n.MatchPredictionTime),
                Overall = CalculateSearchTimesInSeconds(notifications, n => n.OverallSearchTime)
            };
        }

        private static SearchTimesInSeconds CalculateSearchTimesInSeconds(IReadOnlyCollection<SearchResultsNotification> notifications,
            Func<SearchResultsNotification, TimeSpan> getTiming)
        {
            var timings = notifications.Select(getTiming)
                .Select(t => t.TotalSeconds)
                .OrderBy(s => s)
                .ToList();

            var midPoint = timings.Count/2;

            return new SearchTimesInSeconds
            {
                AllTimings = timings,
                Mean = timings.Average(),
                Min = timings.Min(),
                Median = timings.Count%2 == 0 ? (timings[midPoint-1] + timings[midPoint+1])/2 : timings[midPoint],
                Max = timings.Max()
            };
        }
    }
}