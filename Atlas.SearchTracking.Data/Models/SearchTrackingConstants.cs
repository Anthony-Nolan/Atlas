using Atlas.SearchTracking.Common.Enums;

namespace Atlas.SearchTracking.Data.Models
{
    internal static class SearchTrackingConstants
    {
        public static Dictionary<SearchTrackingEventType, string> MatchPredictionColumnMappings { get; set; }
        public static Dictionary<SearchTrackingEventType, string> MatchingAlgorithmColumnMappings { get; set; }

        static SearchTrackingConstants()
        {
            MatchPredictionColumnMappings = new Dictionary<SearchTrackingEventType, string>
            {
                { SearchTrackingEventType.MatchPredictionPersistingResultsEnded, nameof(SearchRequestMatchPredictionTiming.PersistingResults_EndTimeUtc) },
                { SearchTrackingEventType.MatchPredictionPersistingResultsStarted, nameof(SearchRequestMatchPredictionTiming.PersistingResults_StartTimeUtc) },
                { SearchTrackingEventType.MatchPredictionRunningBatchesEnded, nameof(SearchRequestMatchPredictionTiming.AlgorithmCore_RunningBatches_EndTimeUtc) },
                { SearchTrackingEventType.MatchPredictionRunningBatchesStarted, nameof(SearchRequestMatchPredictionTiming.AlgorithmCore_RunningBatches_StartTimeUtc) },
                { SearchTrackingEventType.MatchPredictionBatchPreparationEnded, nameof(SearchRequestMatchPredictionTiming.PrepareBatches_EndTimeUtc) },
                { SearchTrackingEventType.MatchPredictionBatchPreparationStarted, nameof(SearchRequestMatchPredictionTiming.PrepareBatches_StartTimeUtc) }
            };

            MatchingAlgorithmColumnMappings = new Dictionary<SearchTrackingEventType, string>
            {
                { SearchTrackingEventType.MatchingAlgorithmPersistingResultsEnded, nameof(SearchRequestMatchingAlgorithmAttemptTiming.PersistingResults_EndTimeUtc) },
                { SearchTrackingEventType.MatchingAlgorithmPersistingResultsStarted, nameof(SearchRequestMatchingAlgorithmAttemptTiming.PersistingResults_StartTimeUtc) },
                { SearchTrackingEventType.MatchingAlgorithmCoreScoringEnded, nameof(SearchRequestMatchingAlgorithmAttemptTiming.AlgorithmCore_Scoring_EndTimeUtc) },
                { SearchTrackingEventType.MatchingAlgorithmCoreScoringStarted, nameof(SearchRequestMatchingAlgorithmAttemptTiming.AlgorithmCore_Scoring_StartTimeUtc) },
                { SearchTrackingEventType.MatchingAlgorithmCoreMatchingEnded, nameof(SearchRequestMatchingAlgorithmAttemptTiming.AlgorithmCore_Matching_EndTimeUtc) },
                { SearchTrackingEventType.MatchingAlgorithmCoreMatchingStarted, nameof(SearchRequestMatchingAlgorithmAttemptTiming.AlgorithmCore_Matching_StartTimeUtc) }
            };
        }
    }
}