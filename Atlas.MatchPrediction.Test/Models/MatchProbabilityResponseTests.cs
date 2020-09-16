using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Models;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Models
{
    [TestFixture]
    public class MatchProbabilityResponseTests
    {
        [Test]
        public void Round_RoundsAllProbabilityValues()
        {
            const decimal unRounded = 0.1234567890m;
            const decimal rounded = 0.1235m;

            var unRoundedResponse = MatchProbabilityResponseBuilder.New.WithAllProbabilityValuesSetTo(unRounded).Build();

            var roundedResponse = unRoundedResponse.Round(4);

            roundedResponse.MatchProbabilities.ZeroMismatchProbability.Decimal.Should().Be(rounded);
            roundedResponse.MatchProbabilities.OneMismatchProbability.Decimal.Should().Be(rounded);
            roundedResponse.MatchProbabilities.TwoMismatchProbability.Decimal.Should().Be(rounded);
            roundedResponse.ZeroMismatchProbabilityPerLocus.Should().BeEquivalentTo(new LociInfo<Probability>(new Probability(rounded)));
            roundedResponse.OneMismatchProbabilityPerLocus.Should().BeEquivalentTo(new LociInfo<Probability>(new Probability(rounded)));
            roundedResponse.TwoMismatchProbabilityPerLocus.Should().BeEquivalentTo(new LociInfo<Probability>(new Probability(rounded)));
        }

        [Test]
        public void Round_WhenValuesNull_ReturnsNullValues()
        {
            var unRoundedResponse = MatchProbabilityResponseBuilder.New.WithAllProbabilityValuesNull().Build();

            var roundedResponse = unRoundedResponse.Round(4);

            roundedResponse.MatchProbabilities.ZeroMismatchProbability.Should().BeNull();
            roundedResponse.MatchProbabilities.OneMismatchProbability.Should().BeNull();
            roundedResponse.MatchProbabilities.TwoMismatchProbability.Should().BeNull();
            roundedResponse.ZeroMismatchProbabilityPerLocus.Should().BeEquivalentTo(new LociInfo<Probability>((Probability) null));
            roundedResponse.OneMismatchProbabilityPerLocus.Should().BeEquivalentTo(new LociInfo<Probability>((Probability) null));
            roundedResponse.TwoMismatchProbabilityPerLocus.Should().BeEquivalentTo(new LociInfo<Probability>((Probability) null));
        }

        [Test]
        public void MatchCount_WhenUnrepresentedPhenotype_ReturnsNullMatchCounts()
        {
            var response = MatchProbabilityResponseBuilder.New
                .WithAllProbabilityValuesNull()
                .With(r => r.IsDonorPhenotypeUnrepresented, true).Build();

            response.OverallMatchCount.Should().Be(null);
            response.ExactMatchCount.Should().Be(null);
            response.PotentialMatchCount.Should().Be(null);
        }
    }
}