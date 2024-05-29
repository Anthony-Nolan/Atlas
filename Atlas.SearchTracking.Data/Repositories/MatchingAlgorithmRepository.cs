using Atlas.SearchTracking.Data.Context;
using Atlas.SearchTracking.Data.Models;
using Atlas.SearchTracking.Enums;
using Atlas.SearchTracking.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.SearchTracking.Data.Repositories
{
    public interface IMatchingAlgorithmRepository
    {
       Task Create(MatchingAlgorithmAttemptStartedEvent matchingAlgorithmStartedEvent);

       Task Completed(int searchRequestId, int attemptNumber, MatchingAlgorithmCompletedEvent matchingAlgorithmCompletedEvent);

       Task UpdateTimingInformation(int searchRequestId, int attemptNumber, SearchTrackingEventType eventType);
    }

    public class MatchingAlgorithmRepository : IMatchingAlgorithmRepository
    {
        private readonly SearchTrackingContext context;

        private DbSet<SearchRequestMatchingAlgorithmAttemptTiming> MatchingAlgorithmAttempts => context.SearchRequestMatchingAlgorithmAttemptTimings;

        public MatchingAlgorithmRepository(SearchTrackingContext context)
        {
            this.context = context;
        }

        public async Task Create(MatchingAlgorithmAttemptStartedEvent matchingAlgorithmStartedEvent)
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

        public async Task Completed(int searchRequestId, int attemptNumber, MatchingAlgorithmCompletedEvent matchingAlgorithmCompletedEvent)
        {
            var matchingAlgorithmAttempt = await MatchingAlgorithmAttempts.FindAsync(searchRequestId, attemptNumber);

            if (matchingAlgorithmAttempt == null)
            {
                throw new Exception($"Search request with id {searchRequestId} and {attemptNumber} not found");
            }

            matchingAlgorithmAttempt.CompletionTimeUtc = matchingAlgorithmCompletedEvent.CompletionTimeUtc;

            await context.SaveChangesAsync();
        }

        public async Task UpdateTimingInformation(int searchRequestId, int attemptNumber, SearchTrackingEventType eventType)
        {
            var matchingAlgorithmAttempt = await MatchingAlgorithmAttempts.FindAsync(searchRequestId, attemptNumber);

            if (matchingAlgorithmAttempt == null)
            {
                throw new Exception($"Search request with id {searchRequestId} and {attemptNumber} not found");
            }

            var timingProperty = SearchTrackingConstants.MatchingAlgorithmColumnMappings[eventType];

            matchingAlgorithmAttempt.GetType().GetProperty(timingProperty)?.SetValue(matchingAlgorithmAttempt, DateTime.UtcNow);

            await context.SaveChangesAsync();
        }
    }
}