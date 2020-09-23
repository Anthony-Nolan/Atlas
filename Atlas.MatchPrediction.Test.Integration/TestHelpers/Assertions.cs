using Atlas.Client.Models.Search.Results.MatchPrediction;
using FluentAssertions;

namespace Atlas.MatchPrediction.Test.Integration.TestHelpers
{
    internal static class Assertions
    {
        public static void ShouldHavePercentages(this MatchProbabilities matchProbabilities, int? expected0Mm, int? expected1Mm, int? expected2Mm)
        {
            // Combine probabilities so that error message tells us all actual values at once
            var probabilities = (
                matchProbabilities.ZeroMismatchProbability?.Percentage,
                matchProbabilities.OneMismatchProbability?.Percentage,
                matchProbabilities.TwoMismatchProbability?.Percentage
            );
            var expectedProbabilities = (expected0Mm, expected1Mm, expected2Mm);

            probabilities.Should().BeEquivalentTo(expectedProbabilities);
        }
    }
}