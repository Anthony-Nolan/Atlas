using System.Reflection;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Test.Integration.TestHelpers;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using FluentAssertions;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    [TestFixture]
    internal class PerformanceBenchmarks : MatchProbabilityTestsBase
    {
        private const string EthnicityCode = "performance-benchmark-ethnicity";
        private const string RegistryCode = "performance-benchmark-registry";

        private Builder<MatchProbabilityInput> InputBuilder => DefaultInputBuilder
            .With(i => i.DonorFrequencySetMetadata, new FrequencySetMetadata {EthnicityCode = EthnicityCode, RegistryCode = RegistryCode})
            .With(i => i.PatientFrequencySetMetadata, new FrequencySetMetadata {EthnicityCode = EthnicityCode, RegistryCode = RegistryCode});

        // Selected to match common frequency in large haplotype frequency set. 
        private static PhenotypeInfoBuilder<string> UnambiguousPhenotypeBuilder => new PhenotypeInfoBuilder<string>()
            .WithDataAt(Locus.A, "01:01:01")
            .WithDataAt(Locus.B, "08:01:01")
            .WithDataAt(Locus.C, "07:01:01")
            .WithDataAt(Locus.Dqb1, "02:01:01")
            .WithDataAt(Locus.Drb1, "03:01:01");

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            await TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage_Async(async () => { await ImportHaplotypeFrequencies(); });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearDatabases);
        }

        [Test]
        [IgnoreExceptOnCiPerfTest("Ran in ~4.4s")]
        public async Task MatchPrediction_WithSmallAmbiguityAtEachDonorLocus_CalculatesProbabilityCorrectly()
        {
            var donorHla = new PhenotypeInfoBuilder<string>()
                .WithDataAt(Locus.A, "01:01:01/02:01:01")
                .WithDataAt(Locus.B, "08:01:01/07:02:01")
                .WithDataAt(Locus.C, "07:01:01/02:02:02")
                .WithDataAt(Locus.Dqb1, "02:01:01/06:02:01")
                .WithDataAt(Locus.Drb1, "03:01:01/07:01:01")
                .Build();

            var patientHla = UnambiguousPhenotypeBuilder.Build();

            var matchProbabilityInput = InputBuilder
                .With(i => i.DonorHla, donorHla)
                .With(i => i.PatientHla, patientHla)
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbability.Percentage.Should().Be(75);
            matchDetails.OneMismatchProbability.Percentage.Should().Be(22);
            matchDetails.TwoMismatchProbability.Percentage.Should().Be(3);
        }

        [Test]
        // TODO: ATLAS-400: This should be runnable
        // [Ignore("Too slow to complete yet.")]
        public async Task MatchPrediction_WithDonorFullyTyped_AtTruncatedTwoFieldAlleleResolution_CalculatesProbabilityCorrectly()
        {
            var donorHla = new PhenotypeInfoBuilder<string>()
                .WithDataAt(Locus.A, "01:01")
                .WithDataAt(Locus.B, "08:01")
                .WithDataAt(Locus.C, "07:01")
                .WithDataAt(Locus.Dqb1, "02:01")
                .WithDataAt(Locus.Drb1, "03:01")
                .Build();

            var patientHla = UnambiguousPhenotypeBuilder.Build();

            var matchProbabilityInput = InputBuilder
                .With(i => i.DonorHla, donorHla)
                .With(i => i.PatientHla, patientHla)
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbability.Percentage.Should().Be(97);
            matchDetails.OneMismatchProbability.Percentage.Should().Be(3);
            matchDetails.TwoMismatchProbability.Percentage.Should().Be(0);
        }

        [Test]
        // TODO: ATLAS-400: This should be runnable
        [Ignore("Too slow to complete yet.")]
        public async Task MatchPrediction_WithDonorFullyTyped_AtXXCodeResolution_CalculatesProbabilityCorrectly()
        {
            var donorHla = new PhenotypeInfoBuilder<string>()
                .WithDataAt(Locus.A, "01:XX")
                .WithDataAt(Locus.B, "08:XX")
                .WithDataAt(Locus.C, "07:XX")
                .WithDataAt(Locus.Dqb1, "02:XX")
                .WithDataAt(Locus.Drb1, "03:XX")
                .Build();

            var patientHla = UnambiguousPhenotypeBuilder.Build();

            var matchProbabilityInput = InputBuilder
                .With(i => i.DonorHla, donorHla)
                .With(i => i.PatientHla, patientHla)
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbability.Percentage.Should().Be(97);
            matchDetails.OneMismatchProbability.Percentage.Should().Be(3);
            matchDetails.TwoMismatchProbability.Percentage.Should().Be(0);
        }

        private static async Task ImportHaplotypeFrequencies()
        {
            var importer = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencyService>();

            // This file is an actual test file, representing frequencies from one of the largest registries 
            var filePath = $"Atlas.MatchPrediction.Test.Integration.Resources.HaplotypeFrequencySets.large.csv";

            await using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(filePath))
            using (var file = FrequencySetFileBuilder.FileWithoutContents(RegistryCode, EthnicityCode)
                .WithHaplotypeFrequencyFileStream(stream)
                .Build()
            )
            {
                await importer.ImportFrequencySet(file);
            }
        }
    }
}