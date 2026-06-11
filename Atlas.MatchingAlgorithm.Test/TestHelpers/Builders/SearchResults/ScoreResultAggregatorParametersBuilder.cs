using Atlas.MatchingAlgorithm.Services.Search.Scoring.Aggregation;
using AutoFixture.Dsl;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using static EnumStringValues.EnumExtensions;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchResults;

public static class ScoreResultAggregatorParametersBuilder
{
    public static IPostprocessComposer<ScoreResultAggregatorParameters> New =>
        FixtureBuilder.For<ScoreResultAggregatorParameters>()
            .With(x => x.ScoreResult, new ScoreResultBuilder().Build())
            .With(x => x.ScoredLoci, EnumerateValues<Locus>().ToList())
            .With(x => x.LociToExclude, new List<Locus>());
}