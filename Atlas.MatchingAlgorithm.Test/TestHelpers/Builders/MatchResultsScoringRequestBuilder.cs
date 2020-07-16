using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchResults;
using LochNessBuilder;
using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders
{
    [Builder]
    public static class MatchResultsScoringRequestBuilder
    {
        public static Builder<MatchResultsScoringRequest> New =>
            Builder<MatchResultsScoringRequest>.New
                .With(x => x.PatientHla, new PhenotypeInfo<string>())
                .With(x => x.MatchResults, new List<MatchResult>())
                .With(x => x.ScoringCriteria, ScoringCriteriaBuilder.New.Build());

        public static Builder<MatchResultsScoringRequest> ScoreAtAllLoci =>
            New.With(x => x.ScoringCriteria, ScoringCriteriaBuilder.ScoreAllLoci.Build());

        public static Builder<MatchResultsScoringRequest> ScoreDefaultMatchAtAllLoci =>
            ScoreAtAllLoci.With(x => x.MatchResults, new[] {new MatchResultBuilder().Build()});
    }
}
