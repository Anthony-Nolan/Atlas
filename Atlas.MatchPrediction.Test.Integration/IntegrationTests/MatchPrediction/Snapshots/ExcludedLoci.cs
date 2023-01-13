using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.Test.Integration.TestHelpers;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.Snapshots
{
    internal partial class SnapshotTests
    {
        [TestCase(new[] {Locus.C}, 10, 22, 34)]
        [TestCase(new[] {Locus.Dqb1}, 16, 26, 40)]
        [TestCase(new[] {Locus.C, Locus.Dqb1}, 10, 20, 33)]
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