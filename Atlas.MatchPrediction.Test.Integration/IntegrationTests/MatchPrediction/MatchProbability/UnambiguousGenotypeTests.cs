using System.Reflection;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    public class UnambiguousGenotypeTests : MatchProbabilityTestsBase
    {
        // This genotype has been specifically selected to be unrepresented in the global HF set.
        private readonly PhenotypeInfo<string> unambiguousUnrepresentedGenotype = new PhenotypeInfoBuilder<string>()
            .WithDataAt(Locus.A, LocusPosition.One, "01:01")
            .WithDataAt(Locus.A, LocusPosition.Two, "23:01")
            .WithDataAt(Locus.B, LocusPosition.One, "44:03")
            .WithDataAt(Locus.B, LocusPosition.Two, "57:01")
            .WithDataAt(Locus.C, LocusPosition.One, "04:28")
            .WithDataAt(Locus.C, LocusPosition.Two, "06:02")
            .WithDataAt(Locus.Dqb1, LocusPosition.One, "03:01")
            .WithDataAt(Locus.Dqb1, LocusPosition.Two, "04:01")
            .WithDataAt(Locus.Drb1, LocusPosition.One, "08:07")
            .WithDataAt(Locus.Drb1, LocusPosition.Two, "11:04")
            .Build();

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            await TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage_Async(async () => { await ImportHaplotypeFrequencies(); });
        }

        [Test]
        public async Task
            CalculateMatchProbability_WhenPatientIsUnambiguousByPGroup_AndUnrepresentedInHaplotypeFrequencySet_DoesNotMarkPatientAsUnrepresented()
        {
            var donorHla = new PhenotypeInfoBuilder<string>()
                .WithDataAt(Locus.A, LocusPosition.One, "01:XX")
                .WithDataAt(Locus.A, LocusPosition.Two, "23:XX")
                .WithDataAt(Locus.B, LocusPosition.One, "44:XX")
                .WithDataAt(Locus.B, LocusPosition.Two, "57:XX")
                .WithDataAt(Locus.C, LocusPosition.One, "04:XX")
                .WithDataAt(Locus.C, LocusPosition.Two, "06:XX")
                .WithDataAt(Locus.Dqb1, LocusPosition.One, "03:XX")
                .WithDataAt(Locus.Dqb1, LocusPosition.Two, "04:XX")
                .WithDataAt(Locus.Drb1, LocusPosition.One, "08:XX")
                .WithDataAt(Locus.Drb1, LocusPosition.Two, "11:XX")
                .Build();

            var matchProbabilityInput = DefaultInputBuilder
                .WithPatientHla(unambiguousUnrepresentedGenotype)
                .WithPatientMetadata(GlobalHfSetMetadata)
                .WithDonorHla(donorHla)
                .WithDonorMetadata(GlobalHfSetMetadata)
                .Build();

            var matchProbability = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchProbability.IsPatientPhenotypeUnrepresented.Should().BeFalse();
            matchProbability.MatchProbabilities.ZeroMismatchProbability.Percentage.Should().Be(0);
            matchProbability.MatchProbabilities.OneMismatchProbability.Percentage.Should().Be(0);
            matchProbability.MatchProbabilities.TwoMismatchProbability.Percentage.Should().Be(32);
        }

        [Test]
        public async Task
            CalculateMatchProbability_WhenDonorIsUnambiguousByPGroup_AndUnrepresentedInHaplotypeFrequencySet_DoesNotMarkDonorAsUnrepresented()
        {
            var patientHla = new PhenotypeInfoBuilder<string>()
                .WithDataAt(Locus.A, LocusPosition.One, "01:XX")
                .WithDataAt(Locus.A, LocusPosition.Two, "23:XX")
                .WithDataAt(Locus.B, LocusPosition.One, "44:XX")
                .WithDataAt(Locus.B, LocusPosition.Two, "57:XX")
                .WithDataAt(Locus.C, LocusPosition.One, "04:XX")
                .WithDataAt(Locus.C, LocusPosition.Two, "06:XX")
                .WithDataAt(Locus.Dqb1, LocusPosition.One, "03:XX")
                .WithDataAt(Locus.Dqb1, LocusPosition.Two, "04:XX")
                .WithDataAt(Locus.Drb1, LocusPosition.One, "08:XX")
                .WithDataAt(Locus.Drb1, LocusPosition.Two, "11:XX")
                .Build();

            var matchProbabilityInput = DefaultInputBuilder
                .WithPatientHla(patientHla)
                .WithPatientMetadata(GlobalHfSetMetadata)
                .WithDonorHla(unambiguousUnrepresentedGenotype)
                .WithDonorMetadata(GlobalHfSetMetadata)
                .Build();

            var matchProbability = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchProbability.IsDonorPhenotypeUnrepresented.Should().BeFalse();
            matchProbability.MatchProbabilities.ZeroMismatchProbability.Percentage.Should().Be(0);
            matchProbability.MatchProbabilities.OneMismatchProbability.Percentage.Should().Be(0);
            matchProbability.MatchProbabilities.TwoMismatchProbability.Percentage.Should().Be(32);
        }
        
        [Test]
        public async Task
            CalculateMatchProbability_WhenDonorAndPatientAreBothUnambiguous_AndUnrepresented_DoesNotMarkEitherAsUnrepresented_AndReturnsCertainProbability()
        {
            var matchProbabilityInput = DefaultInputBuilder
                .WithPatientHla(unambiguousUnrepresentedGenotype)
                .WithPatientMetadata(GlobalHfSetMetadata)
                .WithDonorHla(unambiguousUnrepresentedGenotype)
                .WithDonorMetadata(GlobalHfSetMetadata)
                .Build();

            var matchProbability = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchProbability.IsDonorPhenotypeUnrepresented.Should().BeFalse();
            matchProbability.IsPatientPhenotypeUnrepresented.Should().BeFalse();
            matchProbability.MatchProbabilities.ZeroMismatchProbability.Percentage.Should().Be(100);
            matchProbability.MatchProbabilities.OneMismatchProbability.Percentage.Should().Be(0);
            matchProbability.MatchProbabilities.TwoMismatchProbability.Percentage.Should().Be(0);
        }
        
        private static async Task ImportHaplotypeFrequencies()
        {
            var importer = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencyService>();

            var filePath = $"Atlas.MatchPrediction.Test.Integration.Resources.HaplotypeFrequencySets.global.json";

            await using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(filePath))
            using (var file = FrequencySetFileBuilder.FileWithoutContents()
                .WithHaplotypeFrequencyFileStream(stream)
                .Build()
            )
            {
                await importer.ImportFrequencySet(file, new FrequencySetImportBehaviour{ ShouldBypassHlaValidation = true});
            }
        }
    }
}