using Atlas.SearchTracking.Data.Context;
using Atlas.SearchTracking.Data.Models;
using Atlas.SearchTracking.Enums;
using Atlas.SearchTracking.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.SearchTracking.Data.Repositories
{
    public interface IMatchPredictionRepository
    {
        Task Create(MatchPredictionStartedEvent matchPredictionStartedEvent);

        Task UpdateTimingInformation(int searchRequestId, MatchingAlgorithmTimingEventType eventType);
    }

    public class MatchPredictionRepository : IMatchPredictionRepository
    {
        private readonly SearchTrackingContext context;

        private DbSet<SearchRequestMatchPredictionTiming> MatchingAlgorithmPrediction => context.SearchRequestMatchPredictionTimings;

        public MatchPredictionRepository(SearchTrackingContext context)
        {
            this.context = context;
        }

        public async Task Create(MatchPredictionStartedEvent matchPredictionStartedEvent)
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

        public async Task UpdateTimingInformation(int searchRequestId, MatchingAlgorithmTimingEventType eventType)
        {
            var matchPrediction = await MatchingAlgorithmPrediction
                .FirstOrDefaultAsync(x => x.SearchRequestId == searchRequestId);

            if (matchPrediction == null)
            {
                throw new Exception($"Search request with id {searchRequestId} not found");
            }

            var timingProperty = SearchTiming.EventDictionary[eventType];

            matchPrediction.GetType().GetProperty(timingProperty)?.SetValue(matchPrediction, DateTime.UtcNow);

            await context.SaveChangesAsync();
        }
    }
}
