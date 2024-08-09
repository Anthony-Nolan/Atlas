using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using Atlas.SearchTracking.Data.Context;
using Atlas.SearchTracking.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.SearchTracking.Data.Repositories
{
    public interface ISearchRequestMatchingAlgorithmAttemptTimingRepository
    {
       Task TrackStartedEvent(MatchingAlgorithmAttemptStartedEvent matchingAlgorithmStartedEvent);

       Task TrackCompletedEvent(MatchingAlgorithmCompletedEvent matchingAlgorithmCompletedEvent);

       Task TrackTimingEvent(MatchingAlgorithmAttemptTimingEvent matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType eventType);

       Task<SearchRequestMatchingAlgorithmAttemptTiming?> GetSearchRequestMatchingAlgorithmAttemptTimingById(int id);
    }

    public class SearchRequestMatchingAlgorithmAttemptTimingRepository : ISearchRequestMatchingAlgorithmAttemptTimingRepository
    {
        private readonly ISearchTrackingContext context;

        private DbSet<SearchRequestMatchingAlgorithmAttemptTiming> MatchingAlgorithmAttemptTimings => context.SearchRequestMatchingAlgorithmAttemptTimings;
        private DbSet<SearchRequest> SearchRequests => context.SearchRequests;

        public SearchRequestMatchingAlgorithmAttemptTimingRepository(ISearchTrackingContext context)
        {
            this.context = context;
        }

        public async Task TrackStartedEvent(MatchingAlgorithmAttemptStartedEvent matchingAlgorithmStartedEvent)
        {
            var id = await GetSearchRequestIdByGuid(matchingAlgorithmStartedEvent.SearchRequestId);

            var matchingAlgorithmAttempt = new SearchRequestMatchingAlgorithmAttemptTiming
            {
                SearchRequestId = id,
                AttemptNumber = matchingAlgorithmStartedEvent.AttemptNumber,
                InitiationTimeUtc = matchingAlgorithmStartedEvent.InitiationTimeUtc,
                StartTimeUtc = matchingAlgorithmStartedEvent.StartTimeUtc
            };

            MatchingAlgorithmAttemptTimings.Add(matchingAlgorithmAttempt);
            await context.SaveChangesAsync();
        }

        public async Task TrackCompletedEvent(MatchingAlgorithmCompletedEvent matchingAlgorithmCompletedEvent)
        {
            var id = await GetSearchRequestIdByGuid(matchingAlgorithmCompletedEvent.SearchRequestId);

            var matchingAlgorithmAttempt = await GetRequiredMatchingAlgorithmAttemptTiming(id, matchingAlgorithmCompletedEvent.AttemptNumber);

            matchingAlgorithmAttempt.CompletionTimeUtc = matchingAlgorithmCompletedEvent.CompletionTimeUtc;
            await context.SaveChangesAsync();
        }

        public async Task TrackTimingEvent(MatchingAlgorithmAttemptTimingEvent matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType eventType)
        {
            var id = await GetSearchRequestIdByGuid(matchingAlgorithmAttemptTimingEvent.SearchRequestId);

            var matchingAlgorithmAttempt = await GetRequiredMatchingAlgorithmAttemptTiming(id, matchingAlgorithmAttemptTimingEvent.AttemptNumber);
            var timingProperty = SearchTrackingConstants.MatchingAlgorithmColumnMappings[eventType];

            matchingAlgorithmAttempt.GetType().GetProperty(timingProperty)?
                .SetValue(matchingAlgorithmAttempt, matchingAlgorithmAttemptTimingEvent.TimeUtc);
            await context.SaveChangesAsync();
        }

        public async Task<SearchRequestMatchingAlgorithmAttemptTiming> GetSearchRequestMatchingAlgorithmAttemptTimingById(int id)
        {
            var matchingAlgorithmAttempt = await MatchingAlgorithmAttemptTimings
                .FirstOrDefaultAsync(x => x.Id == id);

            if (matchingAlgorithmAttempt == null)
            {
                throw new Exception($"Matching algorithm attempt timing with id {id} not found");
            }

            return matchingAlgorithmAttempt;
        }

        private async Task<SearchRequestMatchingAlgorithmAttemptTiming> GetRequiredMatchingAlgorithmAttemptTiming(int searchRequestId, int attemptNumber)
        {
            var matchingAlgorithmAttempt = await MatchingAlgorithmAttemptTimings
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(x => x.SearchRequestId == searchRequestId && x.AttemptNumber == attemptNumber);

            if (matchingAlgorithmAttempt == null)
            {
                throw new Exception($"Matching algorithm attempt timing for search id {searchRequestId} and attempt number {attemptNumber} not found");
            }

            return matchingAlgorithmAttempt;
        }

        private async Task<int> GetSearchRequestIdByGuid(Guid searchRequestId)
        {
            var searchRequest = await SearchRequests.FirstOrDefaultAsync(x => x.SearchRequestId == searchRequestId);

            if (searchRequest == null)
            {
                throw new Exception($"Search request with Guid {searchRequestId} not found");
            }

            return searchRequest.Id;
        }
    }
}