using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using LochNessBuilder;
using MoreLinq;
using Builder = LochNessBuilder.Builder<Atlas.MatchingAlgorithm.Common.Models.SearchResults.LocusMatchDetails>;


namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders
{
    [Builder]
    internal static class LocusMatchDetailsBuilder
    {
        internal static Builder New => Builder.New;

        internal static Builder WithDoubleMatch(this Builder builder)
        {
            return builder.WithDoubleDirectMatch();
        }

        internal static Builder WithSingleMatch(this Builder builder)
        {
            return builder.With(
                d => d.PositionPairs,
                new[] {(LocusPosition.One, LocusPosition.Two)}.ToHashSet()
            );
        }

        internal static Builder WithDoubleCrossMatch(this Builder builder)
        {
            return builder.With(
                d => d.PositionPairs,
                new[] {(LocusPosition.One, LocusPosition.Two), (LocusPosition.Two, LocusPosition.One)}.ToHashSet()
            );
        }
        
        internal static Builder WithDoubleDirectMatch(this Builder builder)
        {
            return builder.With(
                d => d.PositionPairs,
                new[] {(LocusPosition.One, LocusPosition.One), (LocusPosition.Two, LocusPosition.Two)}.ToHashSet()
            );
        }
    }
}