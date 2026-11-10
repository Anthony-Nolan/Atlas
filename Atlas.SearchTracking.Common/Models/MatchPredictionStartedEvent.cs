namespace Atlas.SearchTracking.Common.Models
{
    public class MatchPredictionStartedEvent : ISearchTrackingEvent
    {
        public Guid SearchIdentifier { get; set; }
        public Guid? OriginalSearchIdentifier { get; set; }
        public DateTime InitiationTimeUtc { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public bool IsParallelMatchPrediction { get; set; }

        /// <summary>
        /// The method the search asked for: true when it reads precomputed genotype sets, false when it calculates live.
        /// Null from a sender that does not set it.
        /// </summary>
        public bool? IsPrecomputedMatchPrediction { get; set; }
    }
}
