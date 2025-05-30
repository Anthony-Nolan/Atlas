﻿using Atlas.SearchTracking.Common.Enums;

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
                { SearchTrackingEventType.MatchPredictionPersistingResultsEnded, nameof(SearchRequestMatchPrediction.PersistingResults_EndTimeUtc) },
                { SearchTrackingEventType.MatchPredictionPersistingResultsStarted, nameof(SearchRequestMatchPrediction.PersistingResults_StartTimeUtc) },
                { SearchTrackingEventType.MatchPredictionRunningBatchesEnded, nameof(SearchRequestMatchPrediction.AlgorithmCore_RunningBatches_EndTimeUtc) },
                { SearchTrackingEventType.MatchPredictionRunningBatchesStarted, nameof(SearchRequestMatchPrediction.AlgorithmCore_RunningBatches_StartTimeUtc) },
                { SearchTrackingEventType.MatchPredictionBatchPreparationEnded, nameof(SearchRequestMatchPrediction.PrepareBatches_EndTimeUtc) },
                { SearchTrackingEventType.MatchPredictionBatchPreparationStarted, nameof(SearchRequestMatchPrediction.PrepareBatches_StartTimeUtc) }
            };

            MatchingAlgorithmColumnMappings = new Dictionary<SearchTrackingEventType, string>
            {
                { SearchTrackingEventType.MatchingAlgorithmPersistingResultsEnded, nameof(SearchRequestMatchingAlgorithmAttempts.PersistingResults_EndTimeUtc) },
                { SearchTrackingEventType.MatchingAlgorithmPersistingResultsStarted, nameof(SearchRequestMatchingAlgorithmAttempts.PersistingResults_StartTimeUtc) },
                { SearchTrackingEventType.MatchingAlgorithmCoreScoringEnded, nameof(SearchRequestMatchingAlgorithmAttempts.AlgorithmCore_Scoring_EndTimeUtc) },
                { SearchTrackingEventType.MatchingAlgorithmCoreScoringStarted, nameof(SearchRequestMatchingAlgorithmAttempts.AlgorithmCore_Scoring_StartTimeUtc) },
                { SearchTrackingEventType.MatchingAlgorithmCoreMatchingEnded, nameof(SearchRequestMatchingAlgorithmAttempts.AlgorithmCore_Matching_EndTimeUtc) },
                { SearchTrackingEventType.MatchingAlgorithmCoreMatchingStarted, nameof(SearchRequestMatchingAlgorithmAttempts.AlgorithmCore_Matching_StartTimeUtc) }
            };
        }
    }
}