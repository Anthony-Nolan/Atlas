using Atlas.SearchTracking.Data.Models;
using LochNessBuilder;

namespace Atlas.SearchTracking.Data.Test.Builders
{
    public class MatchingAlgorithmAttemptBuilder
    {
        public static Builder<SearchRequestMatchingAlgorithmAttemptTiming> New => Builder<SearchRequestMatchingAlgorithmAttemptTiming>.New;

        public static Builder<SearchRequestMatchingAlgorithmAttemptTiming> Default => Builder<SearchRequestMatchingAlgorithmAttemptTiming>.New
            .With(m => m.Id, 1)
            .With(m => m.SearchRequestId, 1)
            .With(m => m.AttemptNumber, 2)
            .With(m => m.InitiationTimeUtc, new DateTime(2021, 1, 1))
            .With(m => m.StartTimeUtc, new DateTime(2021, 1, 1));

        public static Builder<SearchRequestMatchingAlgorithmAttemptTiming> UpdateTimings => Default
           .With(m => m.AlgorithmCore_Matching_StartTimeUtc, new DateTime(2022, 12, 31))
           .With(m => m.AlgorithmCore_Matching_EndTimeUtc, new DateTime(2022, 12, 31))
           .With(m => m.AlgorithmCore_Scoring_StartTimeUtc, new DateTime(2022, 12, 31))
           .With(m => m.AlgorithmCore_Scoring_EndTimeUtc, new DateTime(2022, 12, 31))
           .With(m => m.PersistingResults_StartTimeUtc, new DateTime(2022, 12, 31))
           .With(m => m.PersistingResults_EndTimeUtc, new DateTime(2022, 12, 31));

        public static Builder<SearchRequestMatchingAlgorithmAttemptTiming> Completed => Default
            .With(m => m.CompletionTimeUtc, new DateTime(2022, 1, 1));
    }
}
