using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Test.Integration.Resources.Alleles;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    // The tests in this suite are snapshots of frequencies calculated from the test data - they were not calculated or confirmed by hand  
    public class MissingLociTests : MatchProbabilityTestsBase
    {
        private readonly Dictionary<Locus, string> otherGGroupsAtLoci = new Dictionary<Locus, string>
        {
            {Locus.C, "12:03:01G"},
            {Locus.Dqb1, "05:03:01G"},
        };

        [TestCase(Locus.Dqb1, 40, 60, 0)]
        [TestCase(Locus.C, 40, 60, 0)]
        public async Task CalculateMatchProbability_WhenPatientHlaHasNullLoci_DoesNotIncludeLociInResult(
            Locus nullLocus,
            int zeroMismatchExpectedProbability,
            int oneMismatchExpectedProbability,
            int twoMismatchExpectedProbability)
        {
            var matchProbabilityInput = DefaultInputBuilder
                .With(h => h.PatientHla,
                    new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.Alleles()).WithDataAtLoci(null, nullLocus).Build()
                        .ToPhenotypeInfoTransfer())
                .Build();

            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.00002m).Build(),
                DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.00001m).Build(),
                DefaultHaplotypeFrequency1
                    .WithDataAt(nullLocus, otherGGroupsAtLoci[nullLocus])
                    .With(h => h.Frequency, 0.00003m)
                    .Build(),
            };

            await ImportFrequencies(possibleHaplotypes);

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbabilityPerLocus.GetLocus(nullLocus).Percentage.Should().Be(zeroMismatchExpectedProbability);
            matchDetails.OneMismatchProbabilityPerLocus.GetLocus(nullLocus).Percentage.Should().Be(oneMismatchExpectedProbability);
            matchDetails.TwoMismatchProbabilityPerLocus.GetLocus(nullLocus).Percentage.Should().Be(twoMismatchExpectedProbability);
        }

        [TestCase(Locus.Dqb1, 40, 60, 0)]
        [TestCase(Locus.C, 40, 60, 0)]
        public async Task CalculateMatchProbability_WhenDonorHlaHasNullLoci_CalculatesLociProbabilitiesCorrectly(
            Locus nullLocus,
            int zeroMismatchExpectedProbability,
            int oneMismatchExpectedProbability,
            int twoMismatchExpectedProbability)
        {
            var matchProbabilityInput = DefaultInputBuilder
                .WithDonorHla(new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.Alleles()).WithDataAtLoci(null, nullLocus).Build())
                .Build();

            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.00002m).Build(),
                DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.00001m).Build(),
                DefaultHaplotypeFrequency1
                    .WithDataAt(nullLocus, otherGGroupsAtLoci[nullLocus])
                    .With(h => h.Frequency, 0.00003m)
                    .Build(),
            };

            await ImportFrequencies(possibleHaplotypes);

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbabilityPerLocus.GetLocus(nullLocus).Percentage.Should().Be(zeroMismatchExpectedProbability);
            matchDetails.OneMismatchProbabilityPerLocus.GetLocus(nullLocus).Percentage.Should().Be(oneMismatchExpectedProbability);
            matchDetails.TwoMismatchProbabilityPerLocus.GetLocus(nullLocus).Percentage.Should().Be(twoMismatchExpectedProbability);
        }

        [TestCase(new[] {Locus.Dqb1}, new[] {Locus.C}, 10, 45, 45)]
        [TestCase(new[] {Locus.C}, new[] {Locus.C}, 52, 48, 0)]
        [TestCase(new[] {Locus.Dqb1, Locus.C}, new[] {Locus.C}, 13, 51, 36)]
        public async Task CalculateMatchProbability_WhenPatientAndDonorHlaHaveNullLoci_CalculatesOverallProbabilitiesCorrectly(
            Locus[] nullDonorLoci,
            Locus[] nullPatientLoci,
            int zeroMismatchProbability,
            int oneMismatchProbability,
            int twoMismatchProbability)
        {
            var matchProbabilityInput = DefaultInputBuilder
                .WithDonorHla(
                    new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.Alleles()).WithDataAtLoci(null, nullDonorLoci).Build()
                )
                .WithPatientHla(
                    new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.Alleles()).WithDataAtLoci(null, nullPatientLoci).Build()
                )
                .Build();

            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.00002m).Build(),
                DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.00001m).Build(),
                DefaultHaplotypeFrequency1
                    .WithDataAt(Locus.C, "12:03:01G")
                    .With(h => h.Frequency, 0.00003m)
                    .Build(),
                DefaultHaplotypeFrequency2
                    .WithDataAt(Locus.Dqb1, "05:03:01G")
                    .With(h => h.Frequency, 0.00003m)
                    .Build(),
            };

            await ImportFrequencies(possibleHaplotypes);

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ZeroMismatchProbability.Percentage.Should().Be(zeroMismatchProbability);
            matchDetails.MatchProbabilities.OneMismatchProbability.Percentage.Should().Be(oneMismatchProbability);
            matchDetails.MatchProbabilities.TwoMismatchProbability.Percentage.Should().Be(twoMismatchProbability);
        }
    }
}