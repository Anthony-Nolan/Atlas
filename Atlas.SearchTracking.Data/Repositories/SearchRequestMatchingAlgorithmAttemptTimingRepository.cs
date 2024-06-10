using Atlas.SearchTracking.Data.Context;
using Atlas.SearchTracking.Data.Models;
using Atlas.SearchTracking.Enums;
using Atlas.SearchTracking.Models;
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
        private readonly SearchTrackingContext context;

        private DbSet<SearchRequestMatchingAlgorithmAttemptTiming> MatchingAlgorithmAttemptTimings => context.SearchRequestMatchingAlgorithmAttemptTimings;

        public SearchRequestMatchingAlgorithmAttemptTimingRepository(SearchTrackingContext context)
        {
            this.context = context;
        }

        public async Task TrackStartedEvent(MatchingAlgorithmAttemptStartedEvent matchingAlgorithmStartedEvent)
        {
            var matchingAlgorithmAttempt = new SearchRequestMatchingAlgorithmAttemptTiming
            {
                SearchRequestId = matchingAlgorithmStartedEvent.SearchRequestId,
                AttemptNumber = matchingAlgorithmStartedEvent.AttemptNumber,
                InitiationTimeUtc = matchingAlgorithmStartedEvent.InitiationTimeUtc,
                StartTimeUtc = matchingAlgorithmStartedEvent.StartTimeUtc
            };

            MatchingAlgorithmAttemptTimings.Add(matchingAlgorithmAttempt);
            await context.SaveChangesAsync();
        }

        public async Task TrackCompletedEvent(MatchingAlgorithmCompletedEvent matchingAlgorithmCompletedEvent)
        {
            var matchingAlgorithmAttempt = await GetRequiredMatchingAlgorithmAttemptTiming(
                matchingAlgorithmCompletedEvent.SearchRequestId, matchingAlgorithmCompletedEvent.AttemptNumber);

            matchingAlgorithmAttempt.CompletionTimeUtc = matchingAlgorithmCompletedEvent.CompletionTimeUtc;
            await context.SaveChangesAsync();
        }

        public async Task TrackTimingEvent(MatchingAlgorithmAttemptTimingEvent matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType eventType)
        {
            var matchingAlgorithmAttempt = await GetRequiredMatchingAlgorithmAttemptTiming(
                matchingAlgorithmAttemptTimingEvent.SearchRequestId, matchingAlgorithmAttemptTimingEvent.AttemptNumber);
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
                .FirstOrDefaultAsync(x => x.SearchRequestId == searchRequestId && x.AttemptNumber == attemptNumber);

            if (matchingAlgorithmAttempt == null)
            {
                throw new Exception($"Matching algorithm attempt timing for search id {searchRequestId} and attempt number {attemptNumber} not found");
            }

            return matchingAlgorithmAttempt;
        }
    }
}