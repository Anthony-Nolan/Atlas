using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.MatchPrediction.Test.Integration.TestHelpers;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.Snapshots
{
    internal partial class SnapshotTests
    {
        [TestCase(new[] {Locus.C}, 14, 29, 36)]
        [TestCase(new[] {Locus.Dqb1}, 21, 30, 41)]
        [TestCase(new[] {Locus.C, Locus.Dqb1}, 14, 25, 38)]
        [Repeat(30)]
        public async Task MatchPrediction_WithExcludedLoci(
            Locus[] excludedLoci,
            int? expected0Mm,
            int? expected1Mm,
            int? expected2Mm)
        {
            var matchProbabilityInput = DefaultInputBuilder
                .WithExcludedLoci(excludedLoci)
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ShouldHavePercentages(expected0Mm, expected1Mm, expected2Mm);
        }
    }
}