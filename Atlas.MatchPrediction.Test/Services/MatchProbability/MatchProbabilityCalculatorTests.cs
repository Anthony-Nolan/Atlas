using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Services.MatchProbability;
using NUnit.Framework;
using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.MatchPrediction.Models;
using FluentAssertions;

namespace Atlas.MatchPrediction.Test.Services.MatchProbability
{
    [TestFixture]
    public class MatchProbabilityCalculatorTests
    {
        private IMatchProbabilityCalculator matchProbabilityCalculator;

        private const string PatientLocus1 = "patientGenotype1";
        private const string PatientLocus2 = "patientGenotype2";
        private const string DonorLocus1 = "donorGenotype1";
        private const string DonorLocus2 = "donorGenotype2";

        private static readonly PhenotypeInfo<string> PatientGenotype1 = new PhenotypeInfoBuilder<string>()
        .WithDataAt(Locus.A, new LocusInfo<string> { Position1 = PatientLocus1, Position2 = PatientLocus1 }).Build();
        private static readonly PhenotypeInfo<string> PatientGenotype2 = new PhenotypeInfoBuilder<string>()
        .WithDataAt(Locus.A, new LocusInfo<string> { Position1 = PatientLocus2, Position2 = PatientLocus2 }).Build();
        private static readonly PhenotypeInfo<string> DonorGenotype1 = new PhenotypeInfoBuilder<string>()
        .WithDataAt(Locus.A, new LocusInfo<string> { Position1 = DonorLocus1, Position2 = DonorLocus1 }).Build();
        private static readonly PhenotypeInfo<string> DonorGenotype2 = new PhenotypeInfoBuilder<string>()
        .WithDataAt(Locus.A, new LocusInfo<string> { Position1 = DonorLocus2, Position2 = DonorLocus2 }).Build();

        private static readonly HashSet<PhenotypeInfo<string>> PatientGenotypes = new HashSet<PhenotypeInfo<string>> {PatientGenotype1, PatientGenotype2};
        private static readonly HashSet<PhenotypeInfo<string>> DonorGenotypes = new HashSet<PhenotypeInfo<string>> {DonorGenotype1, DonorGenotype2};

        [SetUp]
        public void Setup()
        {
            matchProbabilityCalculator = new MatchProbabilityCalculator();
        }

        [Test]
        public void CalculateMatchProbability_ReturnsMatchProbability()
        {
            var tenOutOfTenMatchCounts = new LociInfo<int?> {A = 2, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 2};
            var mismatchMatchCounts = new LociInfo<int?> {A = 2, B = 2, C = 2, Dpb1 = null, Dqb1 = 0, Drb1 = 0};

            var matchingPairs = new HashSet<GenotypeMatchDetails>
            {
                new GenotypeMatchDetails{PatientGenotype = PatientGenotype1, DonorGenotype = DonorGenotype1, MatchCounts = tenOutOfTenMatchCounts},
                new GenotypeMatchDetails{PatientGenotype = PatientGenotype2, DonorGenotype = DonorGenotype2, MatchCounts = mismatchMatchCounts}
            };

            var expectedMatchProbabilityPerLocus = new LociInfo<decimal?>
                {A = 0.5M, B = 0.5M, C = 0.5M, Dpb1 = null, Dqb1 = 0.25M, Drb1 = 0.25M};

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(
                PatientGenotypes,
                DonorGenotypes,
                matchingPairs,
                NewGenotypesLikelihoods(0.5m));

            actualProbability.ZeroMismatchProbability.Should().Be(0.25m);
            actualProbability.ZeroMismatchProbabilityPerLocus.Should().Be(expectedMatchProbabilityPerLocus);
        }

        [Test]
        public void CalculateMatchProbability_WhenLocusWithOneMismatch_ReturnsMatchProbability()
        {
            var tenOutOfTenMatchCounts = new LociInfo<int?> {A = 2, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 2};
            var singleMismatch = new LociInfo<int?> { A = 2, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 1 };

            var matchingPairs = new HashSet<GenotypeMatchDetails>
            {
                new GenotypeMatchDetails {PatientGenotype = PatientGenotype1, DonorGenotype = DonorGenotype1, MatchCounts = tenOutOfTenMatchCounts},
                new GenotypeMatchDetails {PatientGenotype = PatientGenotype2, DonorGenotype = DonorGenotype2, MatchCounts = singleMismatch}
            };
            

            var expectedMatchProbabilityPerLocus = new LociInfo<decimal?>
                {A = 0.5M, B = 0.5M, C = 0.5M, Dpb1 = null, Dqb1 = 0.5M, Drb1 = 0.25M};

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(
                PatientGenotypes,
                DonorGenotypes,
                matchingPairs,
                NewGenotypesLikelihoods(0.5m));

            actualProbability.OneMismatchProbability.Should().Be(0.25m);
            actualProbability.ZeroMismatchProbabilityPerLocus.Should().Be(expectedMatchProbabilityPerLocus);
        }

        [Test]
        public void CalculateMatchProbability_WhenLocusWithTwoMismatchesAtSameLocus_ReturnsMatchProbability()
        {
            var tenOutOfTenMatchCounts = new LociInfo<int?> {A = 2, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 2};
            var doubleMismatch1 = new LociInfo<int?> { A = 2, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 0 };

            var matchingPairs = new HashSet<GenotypeMatchDetails>
            {
                new GenotypeMatchDetails{PatientGenotype = PatientGenotype1, DonorGenotype = DonorGenotype1, MatchCounts = tenOutOfTenMatchCounts},
                new GenotypeMatchDetails{PatientGenotype = PatientGenotype2, DonorGenotype = DonorGenotype2, MatchCounts = doubleMismatch1}
            };

            var expectedMatchProbabilityPerLocus = new LociInfo<decimal?>
                {A = 0.5M, B = 0.5M, C = 0.5M, Dpb1 = null, Dqb1 = 0.5M, Drb1 = 0.25M};

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(
                PatientGenotypes,
                DonorGenotypes,
                matchingPairs,
                NewGenotypesLikelihoods(0.5m));

            actualProbability.TwoMismatchProbability.Should().Be(0.25m);
            actualProbability.ZeroMismatchProbabilityPerLocus.Should().Be(expectedMatchProbabilityPerLocus);
        }

        [Test]
        public void CalculateMatchProbability_WhenLocusWithTwoMismatchesAtDifferentLoci_ReturnsMatchProbability()
        {
            var tenOutOfTenMatchCounts = new LociInfo<int?> {A = 2, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 2};
            var doubleMismatch2 = new LociInfo<int?> { A = 2, B = 2, C = 2, Dpb1 = null, Dqb1 = 1, Drb1 = 1 };

            var matchingPairs = new HashSet<GenotypeMatchDetails>
            {
                new GenotypeMatchDetails{PatientGenotype = PatientGenotype1, DonorGenotype = DonorGenotype1, MatchCounts = tenOutOfTenMatchCounts},
                new GenotypeMatchDetails{PatientGenotype = PatientGenotype2, DonorGenotype = DonorGenotype2, MatchCounts = doubleMismatch2}
            };

            var expectedMatchProbabilityPerLocus = new LociInfo<decimal?>
                {A = 0.5M, B = 0.5M, C = 0.5M, Dpb1 = null, Dqb1 = 0.25M, Drb1 = 0.25M};

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(
                PatientGenotypes,
                DonorGenotypes,
                matchingPairs,
                NewGenotypesLikelihoods(0.5m));

            actualProbability.TwoMismatchProbability.Should().Be(0.25m);
            actualProbability.ZeroMismatchProbabilityPerLocus.Should().Be(expectedMatchProbabilityPerLocus);
        }

        [Test]
        public void CalculateMatchProbability_WithUnrepresentedPhenotypes_HasZeroProbability()
        {
            var tenOutOfTenMatchCounts = new LociInfo<int?> {A = 2, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 2};

            var matchingPairs = new HashSet<GenotypeMatchDetails>
            {
                new GenotypeMatchDetails{PatientGenotype = PatientGenotype1, DonorGenotype = DonorGenotype1, MatchCounts = tenOutOfTenMatchCounts}
            };

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(
                PatientGenotypes,
                DonorGenotypes,
                matchingPairs,
                NewGenotypesLikelihoods(0m));

            actualProbability.ZeroMismatchProbability.Should().Be(0m);
            actualProbability.OneMismatchProbability.Should().Be(0m);
            actualProbability.TwoMismatchProbability.Should().Be(0m);
        }

        private static Dictionary<PhenotypeInfo<string>, decimal> NewGenotypesLikelihoods(decimal likelihood)
        {
            return new Dictionary<PhenotypeInfo<string>, decimal>
            {
                {PatientGenotype1, likelihood},
                {PatientGenotype2, likelihood},
                {DonorGenotype1, likelihood},
                {DonorGenotype2, likelihood}
            };
        }
    }
}