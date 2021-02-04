using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Test.Integration.TestHelpers;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.Snapshots
{
    [TestFixture]
    internal partial class SnapshotTests
    {
        [TestCaseSource(nameof(genotypePairTestCases))]
        public async Task MatchPrediction_WithVariedPatientAndDonorHla(
            PhenotypeInfo<string> patientHla,
            PhenotypeInfo<string> donorHla,
            int? expected0Mm,
            int? expected1Mm,
            int? expected2Mm)
        {
            var matchProbabilityInput = DefaultInputBuilder.WithDonorHla(donorHla).WithPatientHla(patientHla)
                .WithDonorMetadata(new FrequencySetMetadata{ EthnicityCode = Ethnicity2, RegistryCode = Registry2})
                .WithPatientMetadata(new FrequencySetMetadata{ EthnicityCode = Ethnicity2, RegistryCode = Registry2})
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ShouldHavePercentages(expected0Mm, expected1Mm, expected2Mm);
        }

        /// <summary>
        /// Test data in the form: { patientHla, donorHla, expected probability of 0 mismatches [P(0mm)], P(1mm), P(2mm) }
        /// </summary>
        // TODO: ATLAS-830: Add more varied phenotypes to these tests
        private static object[] genotypePairTestCases =
        {
            // // Identical patient/donor
            // new object[]
            // {
            //     new PhenotypeInfo<string>("01:01", "11:01", "27:02", "35:01", "02:02", "04:01", "03:01", "04:01", "03:01", "05:02", "11:01", "16:01"),
            //     new PhenotypeInfo<string>("01:01", "11:01", "27:02", "35:01", "02:02", "04:01", "03:01", "04:01", "03:01", "05:02", "11:01", "16:01"),
            //     100, 0, 0
            // },
            // // Ambiguous donor - XX codes
            // new object[]
            // {
            //     new PhenotypeInfo<string>("01:01", "11:01", "27:02", "35:01", "02:02", "04:01", "03:01", "04:01", "03:01", "05:02", "11:01", "16:01"),
            //     new PhenotypeInfo<string>("01:XX", "11:XX", "27:XX", "35:XX", "02:XX", "04:XX", "03:XX", "04:XX", "03:XX", "05:XX", "11:XX", "16:XX"),
            //     36, 42, 19
            // },
            // // XX codes
            // new object[]
            // {
            //     new PhenotypeInfo<string>("01:XX", "11:XX", "27:XX", "35:XX", "02:XX", "04:XX", "03:XX", "04:XX", "03:XX", "05:XX", "11:XX", "16:XX"),
            //     new PhenotypeInfo<string>("01:XX", "11:XX", "27:XX", "35:XX", "02:XX", "04:XX", "03:XX", "04:XX", "03:XX", "05:XX", "11:XX", "16:XX"),
            //     22, 38, 33
            // },
            // Very slow search from UAT
            new object[]
            {
                new PhenotypeInfo<string>("01:XX", "02:XX", "07:XX", "08:XX", "07:XX", "07:XX", null, null, "02:XX", "06:XX", "03:XX", "15:XX"),
                new PhenotypeInfo<string>("1", "2", "7", "8", null, null, null, null, null, null, "15:AVK", "03:XX"),
                90, 8, 1
            },
            // // MACs vs XX Codes
            // new object[]
            // {
            //     new PhenotypeInfo<string>("01:BKR", "11:BDFZ", "27:HHUG", "35:HWMD", "02:XX", "04:HYFS", "03:JCRF", "04:JEAS", "03:XX", "05:XX", "11:XX", "16:XX"),
            //     new PhenotypeInfo<string>("01:XX", "11:XX", "27:XX", "35:XX", "02:XX", "04:XX", "03:XX", "04:XX", "03:XX", "05:XX", "11:XX", "16:XX"),
            //     12, 15, 65
            // },
            // // Ambiguous patient & donor - Different XX codes to the previous case
            // new object[]
            // {
            //     new PhenotypeInfo<string>("02:XX", "01:XX", "15:XX", "08:XX", "01:XX", "03:XX", "03:XX", "04:XX", "03:XX", "02:XX", "03:XX", "11:XX"),
            //     new PhenotypeInfo<string>("02:XX", "01:XX", "15:XX", "08:XX", "01:XX", "03:XX", "03:XX", "04:XX", "03:XX", "02:XX", "03:XX", "11:XX"),
            //     24, 44, 27
            // },
            // // Known single mismatch (at A)
            // new object[]
            // {
            //     new PhenotypeInfo<string>("01:XX", "01:XX", "15:XX", "08:XX", "01:XX", "03:XX", "03:XX", "04:XX", "03:XX", "02:XX", "03:XX", "11:XX"),
            //     new PhenotypeInfo<string>("02:XX", "01:XX", "15:XX", "08:XX", "01:XX", "03:XX", "03:XX", "04:XX", "03:XX", "02:XX", "03:XX", "11:XX"),
            //     0, 19, 42
            // },
            // // Known multiple mismatches
            // new object[]
            // {
            //     new PhenotypeInfo<string>("03:XX", "03:XX", "07:02", "07:XX", "07:XX", "07:XX", "03:XX", "04:XX", "06:XX", "06:XX", "15:XX", "15:XX"),
            //     new PhenotypeInfo<string>("02:XX", "01:XX", "15:XX", "08:XX", "01:XX", "03:XX", "03:XX", "04:XX", "03:XX", "02:XX", "03:XX", "11:XX"),
            //     0, 0, 0
            // },
            // // Unrepresented Phenotype
            // new object[]
            // {
            //     new PhenotypeInfo<string>("01:01", "11:01", "27:02", "35:01", "02:02", "04:01", "03:01", "04:01", "03:01", "05:02", "11:01", "16:01"),
            //     new PhenotypeInfo<string>("01:01:01:01", "11:01", "15:XX", "15:XX", "01:XX", "01:XX", "03:XX", "04:XX", "03:XX", "02:XX", "03:XX",
            //         "11:129"),
            //     null, null, null
            // },
            // // Missing Loci - Donor only
            // new object[]
            // {
            //     new PhenotypeInfo<string>("01:XX", "11:XX", "27:XX", "35:XX", "02:XX", "04:XX", "03:XX", "04:XX", "03:XX", "05:XX", "11:XX", "16:XX"),
            //     new PhenotypeInfo<string>("01:XX", "11:XX", "27:XX", "35:XX", null, null, null, null, null, null, "11:XX", "16:XX"),
            //     15, 28, 29
            // },
            // // Missing Loci - Patient and Donor
            // new object[]
            // {
            //     new PhenotypeInfo<string>("01:XX", "11:XX", "27:XX", "35:XX", null, null, null, null, null, null, "11:XX", "16:XX"),
            //     new PhenotypeInfo<string>("01:XX", "11:XX", "27:XX", "35:XX", null, null, null, null, null, null, "11:XX", "16:XX"),
            //     11, 21, 25
            // },
        };
    }
}