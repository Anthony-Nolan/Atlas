using System.Collections.Generic;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using LochNessBuilder;

namespace Atlas.MatchPrediction.Test.Verification.Test.TestHelpers
{
    [Builder]
    internal static class MatchingResultSetBuilder
    {
        public static Builder<OriginalMatchingAlgorithmResultSet> New => Builder<OriginalMatchingAlgorithmResultSet>.New;

        public static Builder<OriginalMatchingAlgorithmResultSet> Empty => New
            .With(x => x.Results, new List<MatchingAlgorithmResult>());

        public static Builder<OriginalMatchingAlgorithmResultSet> WithMatchingResult(this Builder<OriginalMatchingAlgorithmResultSet> builder, int donorId)
        {
            return builder.With(x => x.Results, new[] { BuildMatchingAlgorithmResult(donorId) });
        }

        private static MatchingAlgorithmResult BuildMatchingAlgorithmResult(int donorId)
        {
            return new MatchingAlgorithmResult
            {
                DonorCode = donorId.ToString()
            };
        }
    }
}