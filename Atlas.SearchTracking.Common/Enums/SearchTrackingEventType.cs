﻿namespace Atlas.SearchTracking.Common.Enums
{
    public enum SearchTrackingEventType
    {
        SearchRequested,
        MatchingAlgorithmAttemptStarted,
        MatchingAlgorithmCoreMatchingStarted,
        MatchingAlgorithmCoreMatchingEnded,
        MatchingAlgorithmCoreScoringStarted,
        MatchingAlgorithmCoreScoringEnded,
        MatchingAlgorithmPersistingResultsStarted,
        MatchingAlgorithmPersistingResultsEnded,
        MatchingAlgorithmCompleted,
        MatchPredictionStarted,
        MatchPredictionBatchPreparationStarted,
        MatchPredictionBatchPreparationEnded,
        MatchPredictionRunningBatchesStarted,
        MatchPredictionRunningBatchesEnded,
        MatchPredictionPersistingResultsStarted,
        MatchPredictionPersistingResultsEnded,
        MatchPredictionCompleted,
        MatchPredictionResultsSent
    }
}
