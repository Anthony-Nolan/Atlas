using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using Atlas.SearchTracking.Data.Context;
using Atlas.SearchTracking.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.SearchTracking.Data.Repositories
{
    public interface ISearchRequestMatchingAlgorithmAttemptsRepository
    {
       Task TrackStartedEvent(MatchingAlgorithmAttemptStartedEvent matchingAlgorithmStartedEvent);

       Task TrackCompletedEvent(MatchingAlgorithmCompletedEvent matchingAlgorithmCompletedEvent);

       Task TrackTimingEvent(MatchingAlgorithmAttemptTimingEvent matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType eventType);

       Task<SearchRequestMatchingAlgorithmAttempts?> GetSearchRequestMatchingAlgorithmAttemptsById(int id);
    }

    public class SearchRequestMatchingAlgorithmAttemptsRepository : ISearchRequestMatchingAlgorithmAttemptsRepository
    {
        private readonly ISearchTrackingContext context;

        private DbSet<SearchRequestMatchingAlgorithmAttempts> MatchingAlgorithmAttempts => context.SearchRequestMatchingAlgorithmAttempts;
        private DbSet<SearchRequest> SearchRequests => context.SearchRequests;

        public SearchRequestMatchingAlgorithmAttemptsRepository(ISearchTrackingContext context)
        {
            this.context = context;
        }

        public async Task TrackStartedEvent(MatchingAlgorithmAttemptStartedEvent matchingAlgorithmStartedEvent)
        {
            var id = await GetSearchRequestIdByGuid(matchingAlgorithmStartedEvent.SearchRequestId);

            var matchingAlgorithmAttempt = new SearchRequestMatchingAlgorithmAttempts
            {
                SearchRequestId = id,
                AttemptNumber = matchingAlgorithmStartedEvent.AttemptNumber,
                InitiationTimeUtc = matchingAlgorithmStartedEvent.InitiationTimeUtc,
                StartTimeUtc = matchingAlgorithmStartedEvent.StartTimeUtc
            };

            MatchingAlgorithmAttempts.Add(matchingAlgorithmAttempt);
            await context.SaveChangesAsync();
        }

        public async Task TrackCompletedEvent(MatchingAlgorithmCompletedEvent matchingAlgorithmCompletedEvent)
        {
            var id = await GetSearchRequestIdByGuid(matchingAlgorithmCompletedEvent.SearchIdentifier);

            var matchingAlgorithmAttempt = await GetRequiredMatchingAlgorithmAttemptTiming(id, matchingAlgorithmCompletedEvent.AttemptNumber);

            matchingAlgorithmAttempt.CompletionTimeUtc = matchingAlgorithmCompletedEvent.CompletionTimeUtc;
            matchingAlgorithmAttempt.IsSuccessful = matchingAlgorithmCompletedEvent.CompletionDetails.IsSuccessful;
            matchingAlgorithmAttempt.FailureInfo_Type = matchingAlgorithmCompletedEvent.CompletionDetails.FailureInfo?.Type;
            matchingAlgorithmAttempt.FailureInfo_Message = matchingAlgorithmCompletedEvent.CompletionDetails.FailureInfo?.Message;
            matchingAlgorithmAttempt.FailureInfo_ExceptionStacktrace =
                matchingAlgorithmCompletedEvent.CompletionDetails.FailureInfo?.ExceptionStacktrace;

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

        public async Task<SearchRequestMatchingAlgorithmAttempts> GetSearchRequestMatchingAlgorithmAttemptsById(int id)
        {
            var matchingAlgorithmAttempt = await MatchingAlgorithmAttempts
                .FirstOrDefaultAsync(x => x.Id == id);

            if (matchingAlgorithmAttempt == null)
            {
                throw new Exception($"Matching algorithm attempt timing with id {id} not found");
            }

            return matchingAlgorithmAttempt;
        }

        private async Task<SearchRequestMatchingAlgorithmAttempts> GetRequiredMatchingAlgorithmAttemptTiming(int searchRequestId, int attemptNumber)
        {
            var matchingAlgorithmAttempt = await MatchingAlgorithmAttempts
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
            var searchRequest = await SearchRequests.FirstOrDefaultAsync(x => x.SearchIdentifier == searchRequestId);

            if (searchRequest == null)
            {
                throw new Exception($"Search request with Guid {searchRequestId} not found");
            }

            return searchRequest.Id;
        }
    }
}