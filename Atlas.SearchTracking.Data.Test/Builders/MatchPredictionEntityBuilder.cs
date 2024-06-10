using Atlas.SearchTracking.Data.Models;
using LochNessBuilder;

namespace Atlas.SearchTracking.Data.Test.Builders
{
    public class MatchPredictionEntityBuilder
    {
        public static Builder<SearchRequestMatchPredictionTiming> New => Builder<SearchRequestMatchPredictionTiming>.New;

        public static Builder<SearchRequestMatchPredictionTiming> Default => Builder<SearchRequestMatchPredictionTiming>.New
            .With(m => m.Id, 1)
            .With(m => m.SearchRequestId, 1)
            .With(m => m.InitiationTimeUtc, new DateTime(2021, 1, 1))
            .With(m => m.StartTimeUtc, new DateTime(2021, 1, 1));

        public static Builder<SearchRequestMatchPredictionTiming> UpdateTimings => Default
            .With(m => m.PrepareBatches_StartTimeUtc, new DateTime(2022, 12, 31))
            .With(m => m.PrepareBatches_EndTimeUtc, new DateTime(2022, 12, 31))
            .With(m => m.AlgorithmCore_RunningBatches_StartTimeUtc, new DateTime(2022, 12, 31))
            .With(m => m.AlgorithmCore_RunningBatches_EndTimeUtc, new DateTime(2022, 12, 31))
            .With(m => m.PersistingResults_StartTimeUtc, new DateTime(2022, 12, 31))
            .With(m => m.PersistingResults_EndTimeUtc, new DateTime(2022, 12, 31));

        public static Builder<SearchRequestMatchPredictionTiming> Completed => Default
            .With(m => m.CompletionTimeUtc, new DateTime(2022, 1, 1));
    }
}
