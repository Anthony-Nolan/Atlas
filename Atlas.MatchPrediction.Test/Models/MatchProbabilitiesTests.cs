using NUnit.Framework;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using FluentAssertions;

namespace Atlas.MatchPrediction.Test.Models
{
    [TestFixture]
    internal class MatchProbabilitiesTests
    {
        [TestCase(1, PredictiveMatchCategory.Exact)]
        [TestCase(0.995, PredictiveMatchCategory.Exact, Description = "Zero mismatch percentage would round to 100%")]
        [TestCase(0, PredictiveMatchCategory.Mismatch)]
        [TestCase(0.0049, PredictiveMatchCategory.Mismatch, Description = "Zero mismatch percentage would round to 0%")]
        [TestCase(0.6, PredictiveMatchCategory.Potential)]
        public void MatchCategory_ReturnsExpectedMatchCategory(decimal zeroMismatchValue, PredictiveMatchCategory? expectedCategory)
        {
            var matchProbability = MatchProbabilitiesBuilder.New.WithZeroMismatchProbability(zeroMismatchValue).Build();

            matchProbability.MatchCategory.Should().Be(expectedCategory);
        }
    }
}
