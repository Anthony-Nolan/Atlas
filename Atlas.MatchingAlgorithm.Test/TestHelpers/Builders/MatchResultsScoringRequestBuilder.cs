using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchResults;
using AutoFixture.Dsl;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;

internal static class MatchResultsScoringRequestBuilder
{
    public static IPostprocessComposer<MatchResultsScoringRequest> New =>
        FixtureBuilder.For<MatchResultsScoringRequest>()
            .With(x => x.PatientHla, new PhenotypeInfo<string>().ToPhenotypeInfoTransfer())
            .With(x => x.MatchResults, new List<MatchResult>())
            .With(x => x.ScoringCriteria, ScoringCriteriaBuilder.New.Build());

    public static IPostprocessComposer<MatchResultsScoringRequest> ScoreAtAllLoci =>
        New.With(x => x.ScoringCriteria, ScoringCriteriaBuilder.ScoreAllLoci.Build());

    public static IPostprocessComposer<MatchResultsScoringRequest> ScoreDefaultMatchAtAllLoci =>
        ScoreAtAllLoci.With(x => x.MatchResults, new[] {new MatchResultBuilder().Build()});
}