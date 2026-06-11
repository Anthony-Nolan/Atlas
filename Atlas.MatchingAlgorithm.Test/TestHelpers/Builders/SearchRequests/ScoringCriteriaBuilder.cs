using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Common.Requests;
using Atlas.Common.Public.Models.GeneticData;
using AutoFixture.Dsl;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using static EnumStringValues.EnumExtensions;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;

internal static class ScoringCriteriaBuilder
{
    public static IPostprocessComposer<ScoringCriteria> New =>
        FixtureBuilder.For<ScoringCriteria>()
            .With(x => x.LociToScore, new List<Locus>())
            .With(x => x.LociToExcludeFromAggregateScore, new List<Locus>());

    public static IPostprocessComposer<ScoringCriteria> ScoreAllLoci =>
        New.With(x => x.LociToScore, EnumerateValues<Locus>().ToList());
}