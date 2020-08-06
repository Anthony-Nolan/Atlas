using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Test.Integration.Resources.Alleles;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    public class ExcludedLociTests : MatchProbabilityTestsBase
    {
        [Test]
        public async Task CalculateMatchProbability_WhenNoExcludedLoci_IncludesAllLociInResults()
        {
            var matchProbabilityInput = DefaultInputBuilder.Build();

            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.00002m).Build(),
                DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.00001m).Build(),
            };

            await ImportFrequencies(possibleHaplotypes);

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            LocusSettings.MatchPredictionLoci.Should()
                .OnlyContain(l => matchDetails.ZeroMismatchProbabilityPerLocus.GetLocus(l) != null, "only excluded loci should be null");
            LocusSettings.MatchPredictionLoci.Should()
                .OnlyContain(l => matchDetails.OneMismatchProbabilityPerLocus.GetLocus(l) != null, "only excluded loci should be null");
            LocusSettings.MatchPredictionLoci.Should()
                .OnlyContain(l => matchDetails.TwoMismatchProbabilityPerLocus.GetLocus(l) != null, "only excluded loci should be null");
        }

        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C})]
        public async Task CalculateMatchProbability_WithExcludedLoci_DoesNotIncludeLociInResult(Locus[] lociToExclude)
        {
            var matchProbabilityInput = DefaultInputBuilder
                .With(h => h.ExcludedLoci, lociToExclude)
                .Build();

            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.00002m).Build(),
                DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.00001m).Build(),
            };

            await ImportFrequencies(possibleHaplotypes);

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            lociToExclude.Should()
                .OnlyContain(l => matchDetails.ZeroMismatchProbabilityPerLocus.GetLocus(l) == null, "excluded loci should be null");
            lociToExclude.Should()
                .OnlyContain(l => matchDetails.OneMismatchProbabilityPerLocus.GetLocus(l) == null, "excluded loci should be null");
            lociToExclude.Should()
                .OnlyContain(l => matchDetails.TwoMismatchProbabilityPerLocus.GetLocus(l) == null, "excluded loci should be null");
        }

        [TestCase(new[] {Locus.Dqb1}, 39)]
        [TestCase(new[] {Locus.C}, 33)]
        [TestCase(new[] {Locus.Dqb1, Locus.C}, 38)]
        public async Task CalculateMatchProbability_WithExcludedLoci_AndMultipleHaplotypesMatchAtOtherLoci_CalculatesOverallProbabilityCorrectly(
            Locus[] lociToExclude,
            int expectedZeroMismatchProbability)
        {
            var a1 = Alleles.UnambiguousAlleleDetails.A.Position1;
            var a2 = Alleles.UnambiguousAlleleDetails.A.Position2;
            var nonMatchingA = new AlleleWithGGroup {Allele = "01:01", GGroup = "01:01:01G"};
            var patientHla = DefaultUnambiguousAllelesBuilder
                .WithDataAt(Locus.A, LocusPosition.One, $"{a1.Allele}/{nonMatchingA.Allele}")
                .Build();

            var matchProbabilityInput = DefaultInputBuilder
                .WithPatientHla(patientHla)
                .With(i => i.ExcludedLoci, lociToExclude)
                .Build();

            var matchingHaplotype1 = DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.0002m);
            var matchingHaplotype2 = DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.0001m);
            var mismatchHaplotype1 = DefaultHaplotypeFrequency1.WithDataAt(Locus.A, nonMatchingA.GGroup).With(h => h.Frequency, 0.0004m);
            var mismatchHaplotype2 = DefaultHaplotypeFrequency2.WithDataAt(Locus.A, nonMatchingA.GGroup).With(h => h.Frequency, 0.0011m);

            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                matchingHaplotype1.WithFrequency(0.001m).Build(),
                matchingHaplotype2.WithFrequency(0.002m).Build(),

                mismatchHaplotype1.WithFrequency(0.003m).Build(),
                mismatchHaplotype2.WithFrequency(0.004m).Build(),

                matchingHaplotype1.WithDataAt(Locus.C, "12:03:01G").WithFrequency(0.005m).Build(),
                matchingHaplotype2.WithDataAt(Locus.C, "12:03:01G").WithFrequency(0.006m).Build(),

                matchingHaplotype2.WithDataAt(Locus.Dqb1, "05:03:01G").WithFrequency(0.007m).Build(),
                matchingHaplotype1.WithDataAt(Locus.Dqb1, "05:03:01G").WithFrequency(0.008m).Build(),

                mismatchHaplotype1.WithDataAt(Locus.C, "12:03:01G").WithFrequency(0.009m).Build(),
                mismatchHaplotype2.WithDataAt(Locus.C, "12:03:01G").WithFrequency(0.010m).Build(),

                mismatchHaplotype1.WithDataAt(Locus.Dqb1, "05:03:01G").WithFrequency(0.011m).Build(),
                mismatchHaplotype2.WithDataAt(Locus.Dqb1, "05:03:01G").WithFrequency(0.012m).Build(),
            };

            await ImportFrequencies(possibleHaplotypes);

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ZeroMismatchProbability.Percentage.Should().Be(expectedZeroMismatchProbability);
        }
    }
}