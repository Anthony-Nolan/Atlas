using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Data.Models;
using FluentAssertions;
using LochNessBuilder;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    public class MatchProbabilityDonorPatientHlaTests : MatchProbabilityTestsBase
    {
        [Test]
        public async Task CalculateMatchProbability_WhenNoNullLoci_DoesNotIncludeLociInResult()
        {
            var matchProbabilityInput = DefaultInputBuilder.Build();

            await ImportFrequencies(new List<HaplotypeFrequency> { Builder<HaplotypeFrequency>.New.Build() });

            var expectedProbabilityPerLocus = new LociInfo<decimal?> { A = 0, B = 0, C = 0, Dpb1 = null, Dqb1 = 0, Drb1 = 0 };

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedProbabilityPerLocus);
        }

        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C})]
        public async Task CalculateMatchProbability_WhenPatientHlaHasNullLoci_DoesNotIncludeLociInResult(Locus[] lociToExclude)
        {
            var matchProbabilityInput = DefaultInputBuilder.Build();

            await ImportFrequencies(new List<HaplotypeFrequency> {Builder<HaplotypeFrequency>.New.Build()});

            var expectedProbabilityPerLocus = new LociInfo<decimal?> { A = 0, B = 0, C = 0, Dpb1 = null, Dqb1 = 0, Drb1 = 0 };

            foreach (var loci in lociToExclude)
            {
                matchProbabilityInput.PatientHla.SetLocus(loci, null);
                expectedProbabilityPerLocus.SetLocus(loci, null);
            }

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedProbabilityPerLocus);
        }

        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C})]
        public async Task CalculateMatchProbability_WhenDonorHlaHasNullLoci_DoesNotIncludeLociInResult(Locus[] lociToExclude)
        {
            var matchProbabilityInput = DefaultInputBuilder.Build();

            await ImportFrequencies(new List<HaplotypeFrequency> { Builder<HaplotypeFrequency>.New.Build() });

            var expectedProbabilityPerLocus = new LociInfo<decimal?> { A = 0, B = 0, C = 0, Dpb1 = null, Dqb1 = 0, Drb1 = 0 };

            foreach (var loci in lociToExclude)
            {
                matchProbabilityInput.DonorHla.SetLocus(loci, null);
                expectedProbabilityPerLocus.SetLocus(loci, null);
            }

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedProbabilityPerLocus);
        }

        [TestCase(new[] {Locus.Dqb1}, new[] {Locus.C})]
        [TestCase(new[] {Locus.C}, new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C}, new[] {Locus.C})]
        public async Task CalculateMatchProbability_WhenPatientAndDonorHlaHasNullLoci_DoesNotIncludeLociInResult(
            Locus[] donorLociToExclude,
            Locus[] patientLociToExclude)
        {
            var matchProbabilityInput = DefaultInputBuilder.Build();

            await ImportFrequencies(new List<HaplotypeFrequency> { Builder<HaplotypeFrequency>.New.Build() });

            var expectedProbabilityPerLocus = new LociInfo<decimal?> { A = 0, B = 0, C = 0, Dpb1 = null, Dqb1 = 0, Drb1 = 0 };

            foreach (var loci in donorLociToExclude)
            {
                matchProbabilityInput.DonorHla.SetLocus(loci, null);
                expectedProbabilityPerLocus.SetLocus(loci, null);
            }
            foreach (var loci in patientLociToExclude)
            {
                matchProbabilityInput.PatientHla.SetLocus(loci, null);
                expectedProbabilityPerLocus.SetLocus(loci, null);
            }

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedProbabilityPerLocus);
        }
    }
}
