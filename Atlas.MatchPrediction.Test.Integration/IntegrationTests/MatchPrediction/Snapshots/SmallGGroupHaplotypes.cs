using System.Threading.Tasks;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchPrediction.Test.Integration.TestHelpers;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.Snapshots
{
    [TestFixture]
    internal partial class SnapshotTests
    {
        [Test]
        public async Task MatchPrediction_WithSmallGGroupTypedHaplotypes_ForPatientAndDonor()
        {
            var matchProbabilityInput = DefaultInputBuilder
                .WithPatientMetadata(new FrequencySetMetadata {RegistryCode = RegistrySmallG})
                .WithDonorMetadata(new FrequencySetMetadata {RegistryCode = RegistrySmallG})
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ShouldHavePercentages(13, 30, 39);
        }
    }
}