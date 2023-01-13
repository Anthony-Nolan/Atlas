using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Models.FileSchema;
using Atlas.MatchPrediction.Test.Integration.Resources.Alleles;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    internal class SmallGGroupFrequencyTests : MatchProbabilityTestsBase
    {
        private static readonly List<HaplotypeFrequency> DefaultHaplotypeFrequencySetAsSmallGGroups = new List<HaplotypeFrequency>
        {
            DefaultSmallGGroupHaplotypeFrequency1.With(h => h.Frequency, 0.2m).Build(),
            DefaultSmallGGroupHaplotypeFrequency1.With(h => h.A, DefaultSmallGGroups.GetPosition(Locus.A, LocusPosition.Two))
                .With(h => h.Frequency, 0.2m).Build(),
            DefaultSmallGGroupHaplotypeFrequency1.With(h => h.B, DefaultSmallGGroups.GetPosition(Locus.B, LocusPosition.Two))
                .With(h => h.Frequency, 0.2m).Build(),
            DefaultSmallGGroupHaplotypeFrequency2.With(h => h.Frequency, 0.2m).Build(),
        };

        [Test]
        public async Task CalculateMatchProbability_WhenUsingSmallGGroupTypedFrequencySet_CalculatesProbability()
        {
            await ImportFrequencies(DefaultHaplotypeFrequencySetAsSmallGGroups, null, null, typingCategory: ImportTypingCategory.SmallGGroup);

            var patientHla = DefaultUnambiguousAllelesBuilder
                .WithDataAt(Locus.B, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.B.Position1.Allele}/08:182").Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, DefaultGGroups.A).Build();

            var matchProbabilityInput = DefaultInputBuilder
                .WithDonorHla(donorHla)
                .WithPatientHla(patientHla)
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.OneMismatchProbability.Percentage.Should().Be(50);
        }
    }
}