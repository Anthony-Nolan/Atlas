using Atlas.SearchTracking.Data.Context;
using Atlas.SearchTracking.Data.Models;
using Atlas.SearchTracking.Enums;
using Atlas.SearchTracking.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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

        private DbSet<SearchRequestMatchPredictionTiming> MatchPredictionTimings => context.SearchRequestMatchPredictionTimings;

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

            MatchPredictionTimings.Add(matchPrediction);
            await context.SaveChangesAsync();
        }

        public async Task TrackCompletedEvent(MatchPredictionCompletedEvent matchPredictionCompletedEvent)
        {
            var matchPrediction = await GetRequiredMatchPredictionTiming(matchPredictionCompletedEvent.SearchRequestId);

            matchPrediction.CompletionTimeUtc = matchPredictionCompletedEvent.CompletionTimeUtc;
            await context.SaveChangesAsync();
        }

        public async Task TrackTimingEvent(MatchPredictionTimingEvent matchPredictionTimingEvent, SearchTrackingEventType eventType)
        {
            var matchPrediction = await GetRequiredMatchPredictionTiming(matchPredictionTimingEvent.SearchRequestId);
            var timingProperty = SearchTrackingConstants.MatchPredictionColumnMappings[eventType];

            matchPrediction.GetType().GetProperty(timingProperty)?.SetValue(matchPrediction, matchPredictionTimingEvent.TimeUtc);
            await context.SaveChangesAsync();
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
    }
}
