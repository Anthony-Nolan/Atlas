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

        Task<SearchRequestMatchPrediction> GetSearchRequestMatchPredictionById(int id);
    }

    public class MatchPredictionRepository : IMatchPredictionRepository
    {
        private readonly ISearchTrackingContext context;

        private DbSet<SearchRequestMatchPrediction> MatchPredictionTimings => context.SearchRequestMatchPredictions;
        private DbSet<SearchRequest> SearchRequests => context.SearchRequests;

        public MatchPredictionRepository(ISearchTrackingContext context)
        {
            this.context = context;
        }

        public async Task TrackStartedEvent(MatchPredictionStartedEvent matchPredictionStartedEvent)
        {
            var id = await GetSearchRequestIdByIdentifier(matchPredictionStartedEvent.SearchIdentifier);

            var matchPrediction = new SearchRequestMatchPrediction
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
            var id = await GetSearchRequestIdByIdentifier(matchPredictionCompletedEvent.SearchIdentifier);

            var matchPrediction = await GetRequiredMatchPredictionTiming(id);

            matchPrediction.CompletionTimeUtc = matchPredictionCompletedEvent.CompletionTimeUtc;
            await context.SaveChangesAsync();
        }

        public async Task TrackTimingEvent(MatchPredictionTimingEvent matchPredictionTimingEvent, SearchTrackingEventType eventType)
        {
            var id = await GetSearchRequestIdByIdentifier(matchPredictionTimingEvent.SearchIdentifier);

            var matchPrediction = await GetRequiredMatchPredictionTiming(id);
            var timingProperty = SearchTrackingConstants.MatchPredictionColumnMappings[eventType];

            matchPrediction.GetType().GetProperty(timingProperty)?.SetValue(matchPrediction, matchPredictionTimingEvent.TimeUtc);
            await context.SaveChangesAsync();
        }

        public async Task<SearchRequestMatchPrediction> GetSearchRequestMatchPredictionById(int id)
        {
            var matchPrediction = await MatchPredictionTimings
                .FirstOrDefaultAsync(x => x.SearchRequestId == id);

            if (matchPrediction == null)
            {
                throw new Exception($"Match prediction timing for search id { id } not found");
            }

            return matchPrediction;
        }

        private async Task<SearchRequestMatchPrediction> GetRequiredMatchPredictionTiming(int searchRequestId)
        {
            var matchPrediction = await MatchPredictionTimings
                .FirstOrDefaultAsync(x => x.SearchRequestId == searchRequestId);

            if (matchPrediction == null)
            {
                throw new Exception($"Match prediction timing for search id { searchRequestId } not found");
            }

            return matchPrediction;
        }

        private async Task<int> GetSearchRequestIdByIdentifier(Guid searchIdentifier)
        {
            var searchRequest = await SearchRequests.FirstOrDefaultAsync(x => x.SearchIdentifier == searchIdentifier);

            if (searchRequest == null)
            {
                throw new Exception($"Search request with identifier {searchIdentifier} not found");
            }

            return searchRequest.Id;
        }
    }
}
