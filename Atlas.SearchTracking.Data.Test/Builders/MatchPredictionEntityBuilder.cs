using Atlas.SearchTracking.Data.Models;
using LochNessBuilder;

namespace Atlas.SearchTracking.Data.Test.Builders
{
    public class MatchPredictionEntityBuilder
    {
        public static Builder<SearchRequestMatchPrediction> New => Builder<SearchRequestMatchPrediction>.New;

        public static Builder<SearchRequestMatchPrediction> Default => Builder<SearchRequestMatchPrediction>.New
            .With(m => m.Id, 1)
            .With(m => m.SearchRequestId, 1)
            .With(m => m.InitiationTimeUtc, new DateTime(2021, 1, 1))
            .With(m => m.StartTimeUtc, new DateTime(2021, 1, 1));

        public static Builder<SearchRequestMatchPrediction> UpdateTimings => Default
            .With(m => m.PrepareBatches_StartTimeUtc, new DateTime(2022, 12, 31))
            .With(m => m.PrepareBatches_EndTimeUtc, new DateTime(2022, 12, 31))
            .With(m => m.AlgorithmCore_RunningBatches_StartTimeUtc, new DateTime(2022, 12, 31))
            .With(m => m.AlgorithmCore_RunningBatches_EndTimeUtc, new DateTime(2022, 12, 31))
            .With(m => m.PersistingResults_StartTimeUtc, new DateTime(2022, 12, 31))
            .With(m => m.PersistingResults_EndTimeUtc, new DateTime(2022, 12, 31));

        public static Builder<SearchRequestMatchPrediction> Completed => Default
            .With(m => m.CompletionTimeUtc, new DateTime(2022, 1, 1))
            .With(m => m.IsSuccessful, true);
    }
}
