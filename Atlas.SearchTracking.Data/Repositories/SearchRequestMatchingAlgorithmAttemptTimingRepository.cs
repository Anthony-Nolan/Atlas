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
    }

    public class SearchRequestMatchingAlgorithmAttemptTimingRepository : ISearchRequestMatchingAlgorithmAttemptTimingRepository
    {
        private readonly SearchTrackingContext context;

        private DbSet<SearchRequestMatchingAlgorithmAttemptTiming> MatchingAlgorithmAttempts => context.SearchRequestMatchingAlgorithmAttemptTimings;

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

            MatchingAlgorithmAttempts.Add(matchingAlgorithmAttempt);
            await context.SaveChangesAsync();
        }

        public async Task TrackCompletedEvent(MatchingAlgorithmCompletedEvent matchingAlgorithmCompletedEvent)
        {
            var matchingAlgorithmAttempt = await MatchingAlgorithmAttempts
                .FindAsync(matchingAlgorithmCompletedEvent.SearchRequestId, matchingAlgorithmCompletedEvent.AttemptNumber);

            if (matchingAlgorithmAttempt == null)
            {
                throw new Exception($"Search request with id {matchingAlgorithmCompletedEvent.SearchRequestId}" +
                                    $" and {matchingAlgorithmCompletedEvent.AttemptNumber} not found");
            }

            matchingAlgorithmAttempt.CompletionTimeUtc = matchingAlgorithmCompletedEvent.CompletionTimeUtc;

            await context.SaveChangesAsync();
        }

        public async Task TrackTimingEvent(MatchingAlgorithmAttemptTimingEvent matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType eventType)
        {
            var matchingAlgorithmAttempt = await MatchingAlgorithmAttempts
                .FindAsync(matchingAlgorithmAttemptTimingEvent.SearchRequestId, matchingAlgorithmAttemptTimingEvent.AttemptNumber);

            if (matchingAlgorithmAttempt == null)
            {
                throw new Exception($"Search request with id {matchingAlgorithmAttemptTimingEvent.SearchRequestId}" +
                                    $" and {matchingAlgorithmAttemptTimingEvent.AttemptNumber} not found");
            }

            var timingProperty = SearchTrackingConstants.MatchingAlgorithmColumnMappings[eventType];

            matchingAlgorithmAttempt.GetType().GetProperty(timingProperty)?
                .SetValue(matchingAlgorithmAttempt, matchingAlgorithmAttemptTimingEvent.TimeUtc);

            await context.SaveChangesAsync();
        }
    }
}