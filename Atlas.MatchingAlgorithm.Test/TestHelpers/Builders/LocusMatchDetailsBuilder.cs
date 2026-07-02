using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Composer = AutoFixture.Dsl.IPostprocessComposer<Atlas.MatchingAlgorithm.Common.Models.SearchResults.LocusMatchDetails>;


namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;

internal static class LocusMatchDetailsBuilder
{
    internal static Composer New => FixtureBuilder.For<Atlas.MatchingAlgorithm.Common.Models.SearchResults.LocusMatchDetails>();

    internal static Composer WithDoubleMatch(this Composer builder)
    {
        return builder.WithDoubleDirectMatch();
    }

    internal static Composer WithSingleMatch(this Composer builder)
    {
        return builder.With(
            d => d.PositionPairs,
            new[] {(LocusPosition.One, LocusPosition.Two)}.ToHashSet()
        );
    }

    internal static Composer WithDoubleCrossMatch(this Composer builder)
    {
        return builder.With(
            d => d.PositionPairs,
            new[] {(LocusPosition.One, LocusPosition.Two), (LocusPosition.Two, LocusPosition.One)}.ToHashSet()
        );
    }

    internal static Composer WithDoubleDirectMatch(this Composer builder)
    {
        return builder.With(
            d => d.PositionPairs,
            new[] {(LocusPosition.One, LocusPosition.One), (LocusPosition.Two, LocusPosition.Two)}.ToHashSet()
        );
    }
}