using Atlas.SearchTracking.Data.Context;
using Atlas.SearchTracking.Data.Models;
using Atlas.SearchTracking.Enums;
using Atlas.SearchTracking.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.SearchTracking.Data.Repositories
{
    public interface IMatchPredictionRepository
    {
        Task TrackStartedEvent(MatchPredictionStartedEvent matchPredictionStartedEvent);

        Task TrackCompletedEvent(MatchPredictionCompletedEvent matchPredictionCompletedEvent);

        Task TrackTimingEvent(MatchPredictionTimingEvent matchPredictionTimingEvent, SearchTrackingEventType eventType);
    }

    public class MatchPredictionRepository : IMatchPredictionRepository
    {
        private readonly SearchTrackingContext context;

        private DbSet<SearchRequestMatchPredictionTiming> MatchingAlgorithmPrediction => context.SearchRequestMatchPredictionTimings;

        public MatchPredictionRepository(SearchTrackingContext context)
        {
            this.context = context;
        }

        public async Task TrackStartedEvent(MatchPredictionStartedEvent matchPredictionStartedEvent)
        {
            var matchPrediction = new SearchRequestMatchPredictionTiming()
            {
                SearchRequestId = matchPredictionStartedEvent.SearchRequestId,
                InitiationTimeUtc = matchPredictionStartedEvent.InitiationTimeUtc,
                StartTimeUtc = matchPredictionStartedEvent.StartTimeUtc
            };

            MatchingAlgorithmPrediction.Add(matchPrediction);
            await context.SaveChangesAsync();
        }

        public async Task TrackCompletedEvent(MatchPredictionCompletedEvent matchPredictionCompletedEvent)
        {
            var matchPrediction = await MatchingAlgorithmPrediction
                .FirstOrDefaultAsync(x => x.SearchRequestId == matchPredictionCompletedEvent.SearchRequestId);

            if (matchPrediction == null)
            {
                throw new Exception($"Search request with id {matchPredictionCompletedEvent.SearchRequestId} not found");
            }

            matchPrediction.CompletionTimeUtc = matchPredictionCompletedEvent.CompletionTimeUtc;

            await context.SaveChangesAsync();
        }

        public async Task TrackTimingEvent(MatchPredictionTimingEvent matchPredictionTimingEvent, SearchTrackingEventType eventType)
        {
            var matchPrediction = await MatchingAlgorithmPrediction
                .FirstOrDefaultAsync(x => x.SearchRequestId == matchPredictionTimingEvent.SearchRequestId);

            if (matchPrediction == null)
            {
                throw new Exception($"Search request with id {matchPredictionTimingEvent.SearchRequestId} not found");
            }

            var timingProperty = SearchTrackingConstants.MatchPredictionColumnMappings[eventType];

            matchPrediction.GetType().GetProperty(timingProperty)?.SetValue(matchPrediction, matchPredictionTimingEvent.TimeUtc);

            await context.SaveChangesAsync();
        }
    }
}
