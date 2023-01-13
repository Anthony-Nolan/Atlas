using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchResults;
using LochNessBuilder;
using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders
{
    [Builder]
    internal static class MatchResultsScoringRequestBuilder
    {
        public static Builder<MatchResultsScoringRequest> New =>
            Builder<MatchResultsScoringRequest>.New
                .With(x => x.PatientHla, new PhenotypeInfo<string>().ToPhenotypeInfoTransfer())
                .With(x => x.MatchResults, new List<MatchResult>())
                .With(x => x.ScoringCriteria, ScoringCriteriaBuilder.New.Build());

        public static Builder<MatchResultsScoringRequest> ScoreAtAllLoci =>
            New.With(x => x.ScoringCriteria, ScoringCriteriaBuilder.ScoreAllLoci.Build());

        public static Builder<MatchResultsScoringRequest> ScoreDefaultMatchAtAllLoci =>
            ScoreAtAllLoci.With(x => x.MatchResults, new[] {new MatchResultBuilder().Build()});
    }
}
