using System.Threading.Tasks;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Test.Integration.TestHelpers;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.Snapshots
{
    [TestFixture]
    internal partial class SnapshotTests
    {
        [TestCaseSource(nameof(metadataTestCases))]
        public async Task MatchPrediction_WithVariedFrequencySets(
            string donorRegistry,
            string donorEthnicity,
            string patientRegistry,
            string patientEthnicity,
            int? expected0Mm,
            int? expected1Mm,
            int? expected2Mm)
        {
            var matchProbabilityInput = DefaultInputBuilder
                .WithDonorMetadata(new FrequencySetMetadata {EthnicityCode = donorEthnicity, RegistryCode = donorRegistry})
                .WithPatientMetadata(new FrequencySetMetadata {EthnicityCode = patientEthnicity, RegistryCode = patientRegistry})
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ShouldHavePercentages(expected0Mm, expected1Mm, expected2Mm);
        }

        private static object[] metadataTestCases =
        {
            new object[] {Registry1, null, null, null, 22, 38, 33},
            // TODO: ATLAS-829: Use different enough HF sets that these results actually differ
            new object[] {Registry2, null, null, null, 22, 38, 33},
            new object[] {Registry2, Ethnicity1, null, null, 22, 38, 33},
            new object[] {Registry2, Ethnicity2, null, null, 14, 30, 38},

            new object[] {null, null, Registry1, null, 22, 38, 33},
            new object[] {null, null, Registry2, null, 22, 38, 33},
            new object[] {null, null, Registry2, Ethnicity1, 22, 38, 33},
            new object[] {null, null, Registry2, Ethnicity2, 14, 30, 38},

            new object[] {Registry1, null, Registry1, null, 22, 38, 33},
            new object[] {Registry2, null, Registry2, null, 22, 38, 33},
            new object[] {Registry2, Ethnicity1, Registry2, Ethnicity1, 22, 38, 33},
            new object[] {Registry2, Ethnicity2, Registry2, Ethnicity2, 11, 27, 38},
        };
    }
}