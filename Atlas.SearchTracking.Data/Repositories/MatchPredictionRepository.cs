using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using Atlas.SearchTracking.Data.Context;
using Atlas.SearchTracking.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.SearchTracking.Data.Repositories
{
    public interface IMatchPredictionRepository
    {
        Task TrackStartedEvent(MatchPredictionStartedEvent matchPredictionStartedEvent);

        Task TrackCompletedEvent(MatchPredictionCompletedEvent matchPredictionCompletedEvent);

        Task TrackTimingEvent(MatchPredictionTimingEvent matchPredictionTimingEvent, SearchTrackingEventType eventType);

        Task<SearchRequestMatchPredictionTiming> GetSearchRequestMatchPredictionById(int id);
    }

    public class MatchPredictionRepository : IMatchPredictionRepository
    {
        private readonly ISearchTrackingContext context;

        private DbSet<SearchRequestMatchPredictionTiming> MatchPredictionTimings => context.SearchRequestMatchPredictionTimings;
        private DbSet<SearchRequest> SearchRequests => context.SearchRequests;

        public MatchPredictionRepository(ISearchTrackingContext context)
        {
            this.context = context;
        }

        public async Task TrackStartedEvent(MatchPredictionStartedEvent matchPredictionStartedEvent)
        {
            var id = await GetSearchRequestIdByGuid(matchPredictionStartedEvent.SearchRequestId);

            var matchPrediction = new SearchRequestMatchPredictionTiming
            {
                SearchRequestId = id,
                InitiationTimeUtc = matchPredictionStartedEvent.InitiationTimeUtc,
                StartTimeUtc = matchPredictionStartedEvent.StartTimeUtc
            };

            MatchPredictionTimings.Add(matchPrediction);
            await context.SaveChangesAsync();
        }

        public async Task TrackCompletedEvent(MatchPredictionCompletedEvent matchPredictionCompletedEvent)
        {
            var id = await GetSearchRequestIdByGuid(matchPredictionCompletedEvent.SearchRequestId);

            var matchPrediction = await GetRequiredMatchPredictionTiming(id);

            matchPrediction.CompletionTimeUtc = matchPredictionCompletedEvent.CompletionTimeUtc;
            await context.SaveChangesAsync();
        }

        public async Task TrackTimingEvent(MatchPredictionTimingEvent matchPredictionTimingEvent, SearchTrackingEventType eventType)
        {
            var id = await GetSearchRequestIdByGuid(matchPredictionTimingEvent.SearchRequestId);

            var matchPrediction = await GetRequiredMatchPredictionTiming(id);
            var timingProperty = SearchTrackingConstants.MatchPredictionColumnMappings[eventType];

            matchPrediction.GetType().GetProperty(timingProperty)?.SetValue(matchPrediction, matchPredictionTimingEvent.TimeUtc);
            await context.SaveChangesAsync();
        }

        public async Task<SearchRequestMatchPredictionTiming> GetSearchRequestMatchPredictionById(int id)
        {
            var matchPrediction = await MatchPredictionTimings
                .FirstOrDefaultAsync(x => x.SearchRequestId == id);

            if (matchPrediction == null)
            {
                throw new Exception($"Match prediction timing for search id { id } not found");
            }

            return matchPrediction;
        }

        private async Task<SearchRequestMatchPredictionTiming> GetRequiredMatchPredictionTiming(int searchRequestId)
        {
            var matchPrediction = await MatchPredictionTimings
                .FirstOrDefaultAsync(x => x.SearchRequestId == searchRequestId);

            if (matchPrediction == null)
            {
                throw new Exception($"Match prediction timing for search id { searchRequestId } not found");
            }

            return matchPrediction;
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
