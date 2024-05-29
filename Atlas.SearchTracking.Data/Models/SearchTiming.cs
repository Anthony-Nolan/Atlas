using Atlas.SearchTracking.Enums;
namespace Atlas.SearchTracking.Data.Models
{
    public class SearchTiming
    {
        public static Dictionary<MatchingAlgorithmTimingEventType, string> EventDictionary { get; set; }

        static SearchTiming()
        {
            EventDictionary = new Dictionary<MatchingAlgorithmTimingEventType, string>
            {
                { MatchingAlgorithmTimingEventType.Started, "AlgorithmCore_Scoring_StartTimeUtc" },
                { MatchingAlgorithmTimingEventType.Completed, "AlgorithmCore_Scoring_EndTimeUtc" },
                { MatchingAlgorithmTimingEventType.Started, "AlgorithmCore_Matching_StartTimeUtc" },
                { MatchingAlgorithmTimingEventType.Completed, "AlgorithmCore_Matching_EndTimeUtc" },
                { MatchingAlgorithmTimingEventType.Started, "PersistingResults_StartTimeUtc" },
                { MatchingAlgorithmTimingEventType.Completed, "PersistingResults_EndTimeUtc" },
                { MatchingAlgorithmTimingEventType.Started, "CompletionTimeUtc" },
                { MatchingAlgorithmTimingEventType.Started, "PrepareBatches_StartTimeUtc" },
                { MatchingAlgorithmTimingEventType.Completed, "PrepareBatches_EndTimeUtc" },
                { MatchingAlgorithmTimingEventType.Started, "AlgorithmCore_RunningBatches_StartTimeUtc" },
                { MatchingAlgorithmTimingEventType.Completed, "AlgorithmCore_RunningBatches_EndTimeUtc" }
            };
        }
    }
}
