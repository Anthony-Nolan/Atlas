using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    public class NullAllelesTests : MatchProbabilityTestsBase
    {
        [Test]
        public async Task CalculateMatchProbability_WithLocusWithOneNullAndOneExpressingAlleleVsMatchingLocus_WhereOneNullAndOneExpressingAlleleMatch()
        {
            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, LocusPosition.Two, "01:11N").Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, LocusPosition.Two, "01:11N").Build();

            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.WithFrequency(0.04m).Build(),
                DefaultHaplotypeFrequency2.WithFrequency(0.03m).Build(),
                DefaultHaplotypeFrequency1.WithDataAt(Locus.A, "01:11N").WithFrequency(0.02m).Build(),
                DefaultHaplotypeFrequency2.WithDataAt(Locus.A, "01:11N").WithFrequency(0.01m).Build(),
            };

            await ImportFrequencies(possibleHaplotypes);

            var matchProbabilityInput = DefaultInputBuilder.WithPatientHla(patientHla).WithDonorHla(donorHla).Build();

            var matchProbability = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchProbability.MatchProbabilities.ZeroMismatchProbability.Percentage.Should().Be(100);
        }

        [Test]
        public async Task CalculateMatchProbability_WithLocusWithOneNullAndOneExpressingAlleleVsHeterozygousLocus_WhereExpressingAllelesMatch()
        {
            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, "02:09", "02:09").Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, "02:09", "01:11N").Build();

            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.WithDataAt(Locus.A, "02:01:01G").WithFrequency(0.04m).Build(),
                DefaultHaplotypeFrequency2.WithDataAt(Locus.A, "02:01:01G").WithFrequency(0.03m).Build(),
                DefaultHaplotypeFrequency1.WithDataAt(Locus.A, "01:11N").WithFrequency(0.02m).Build(),
                DefaultHaplotypeFrequency2.WithDataAt(Locus.A, "01:11N").WithFrequency(0.01m).Build(),
            };

            await ImportFrequencies(possibleHaplotypes);

            var matchProbabilityInput = DefaultInputBuilder.WithPatientHla(patientHla).WithDonorHla(donorHla).Build();

            var matchProbability = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchProbability.MatchProbabilities.ZeroMismatchProbability.Percentage.Should().Be(100);
        }

        [Test]
        public async Task CalculateMatchProbability_WithLocusWithOneNullAndOneExpressingAlleleVsLocus_WhereOneAlleleMatchingExpressingAllele()
        {
            var patientHla = DefaultUnambiguousAllelesBuilder.Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, LocusPosition.Two, "01:11N").Build();

            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.WithFrequency(0.04m).Build(),
                DefaultHaplotypeFrequency2.WithFrequency(0.03m).Build(),
                DefaultHaplotypeFrequency1.WithDataAt(Locus.A, "01:11N").WithFrequency(0.02m).Build(),
                DefaultHaplotypeFrequency2.WithDataAt(Locus.A, "01:11N").WithFrequency(0.01m).Build(),
            };

            await ImportFrequencies(possibleHaplotypes);

            var matchProbabilityInput = DefaultInputBuilder.WithPatientHla(patientHla).WithDonorHla(donorHla).Build();

            var matchProbability = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchProbability.MatchProbabilities.OneMismatchProbability.Percentage.Should().Be(100);
        }

        [Test]
        public async Task CalculateMatchProbability_WithLocusWithOneNullAndOneExpressingAlleleVsHeterozygousLocus_WhereNoAlleleMatchingExpressingAllele()
        {
            var patientHla = DefaultUnambiguousAllelesBuilder.Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, "01:01", "01:11N").Build();

            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.WithFrequency(0.04m).Build(),
                DefaultHaplotypeFrequency2.WithFrequency(0.03m).Build(),
                DefaultHaplotypeFrequency1.WithDataAt(Locus.A, "01:11N").WithFrequency(0.02m).Build(),
                DefaultHaplotypeFrequency2.WithDataAt(Locus.A, "01:11N").WithFrequency(0.01m).Build(),
                DefaultHaplotypeFrequency1.WithDataAt(Locus.A, "01:01:01G").WithFrequency(0.02m).Build(),
                DefaultHaplotypeFrequency2.WithDataAt(Locus.A, "01:01:01G").WithFrequency(0.01m).Build(),
            };

            await ImportFrequencies(possibleHaplotypes);
            var matchProbabilityInput = DefaultInputBuilder.WithPatientHla(patientHla).WithDonorHla(donorHla).Build();
            
            var matchProbability = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchProbability.MatchProbabilities.TwoMismatchProbability.Percentage.Should().Be(100);
        }

        [Test]
        public async Task CalculateMatchProbability_WithLocusWithOneNullAndOneExpressingAlleleVsHomozygousLocus_WhereNoAllelesMatchExpressingAllele()
        {
            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, "02:09", "02:09").Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, "01:01", "01:11N").Build();

            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.WithDataAt(Locus.A, "02:01:01G").WithFrequency(0.04m).Build(),
                DefaultHaplotypeFrequency2.WithDataAt(Locus.A, "02:01:01G").WithFrequency(0.03m).Build(),
                DefaultHaplotypeFrequency1.WithDataAt(Locus.A, "01:11N").WithFrequency(0.02m).Build(),
                DefaultHaplotypeFrequency2.WithDataAt(Locus.A, "01:11N").WithFrequency(0.01m).Build(),
                DefaultHaplotypeFrequency1.WithDataAt(Locus.A, "01:01:01G").WithFrequency(0.02m).Build(),
                DefaultHaplotypeFrequency2.WithDataAt(Locus.A, "01:01:01G").WithFrequency(0.01m).Build(),
            };

            await ImportFrequencies(possibleHaplotypes);

            var matchProbabilityInput = DefaultInputBuilder.WithPatientHla(patientHla).WithDonorHla(donorHla).Build();

            var matchProbability = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchProbability.MatchProbabilities.TwoMismatchProbability.Percentage.Should().Be(100);
        }

        [Test]
        public async Task CalculateMatchProbability_WithLocusWithOneNullAndOneExpressingAlleleVsLocusWithOneNullAndOneExpressingAllele_WhereExpressingAllelesDoNotMatch()
        {
            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, "02:09", "01:11N").Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, "01:01", "01:11N").Build();

            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.WithDataAt(Locus.A, "02:01:01G").WithFrequency(0.04m).Build(),
                DefaultHaplotypeFrequency2.WithDataAt(Locus.A, "02:01:01G").WithFrequency(0.03m).Build(),
                DefaultHaplotypeFrequency1.WithDataAt(Locus.A, "01:11N").WithFrequency(0.02m).Build(),
                DefaultHaplotypeFrequency2.WithDataAt(Locus.A, "01:11N").WithFrequency(0.01m).Build(),
                DefaultHaplotypeFrequency1.WithDataAt(Locus.A, "01:01:01G").WithFrequency(0.02m).Build(),
                DefaultHaplotypeFrequency2.WithDataAt(Locus.A, "01:01:01G").WithFrequency(0.01m).Build(),
            };

            await ImportFrequencies(possibleHaplotypes);

            var matchProbabilityInput = DefaultInputBuilder.WithPatientHla(patientHla).WithDonorHla(donorHla).Build();

            var matchProbability = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchProbability.MatchProbabilities.TwoMismatchProbability.Percentage.Should().Be(100);
        }
    }
}
