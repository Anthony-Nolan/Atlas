using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Services.MatchProbability;
using NUnit.Framework;
using System.Collections.Generic;
using Atlas.MatchPrediction.Models;
using FluentAssertions;
using LochNessBuilder;

namespace Atlas.MatchPrediction.Test.Services.MatchProbability
{
    [TestFixture]
    public class MatchProbabilityCalculatorTests
    {
        private IMatchProbabilityCalculator matchProbabilityCalculator;

        private const string PatientGenotype1 = "patientGenotype1";
        private const string PatientGenotype2 = "patientGenotype2";
        private const string DonorGenotype1 = "donorGenotype1";
        private const string DonorGenotype2 = "donorGenotype2";

        private static readonly HashSet<PhenotypeInfo<string>> DefaultPatientGenotypes = new HashSet<PhenotypeInfo<string>> 
            {new PhenotypeInfo<string>(PatientGenotype1), new PhenotypeInfo<string>(PatientGenotype2)};

        private static readonly HashSet<PhenotypeInfo<string>> DefaultDonorGenotypes = new HashSet<PhenotypeInfo<string>>
            {new PhenotypeInfo<string>(DonorGenotype1), new PhenotypeInfo<string>(DonorGenotype2)};

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
                DefaultGenotypeMatchDetails1.With(g => g.MatchCounts, tenOutOfTenMatchCounts).Build(),
                DefaultGenotypeMatchDetails2.With(g => g.MatchCounts, mismatchMatchCounts).Build()
            };

            var expectedMatchProbabilityPerLocus = new LociInfo<decimal?>
                {A = 0.5M, B = 0.5M, C = 0.5M, Dpb1 = null, Dqb1 = 0.25M, Drb1 = 0.25M};

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(
                DefaultPatientGenotypes,
                DefaultDonorGenotypes,
                matchingPairs,
                NewGenotypesLikelihoods(0.5m));

            actualProbability.ZeroMismatchProbability.Should().Be(0.25m);
            actualProbability.ZeroMismatchProbabilityPerLocus.Should().Be(expectedMatchProbabilityPerLocus);
        }

        [Test]
        public void CalculateMatchProbability_WhenLocusWithOneMismatch_ReturnsMatchProbability()
        {
            var tenOutOfTenMatchCounts = new LociInfo<int?> {A = 2, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 2};
            var singleMismatch = new LociInfo<int?> {A = 2, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 1 };

            var matchingPairs = new HashSet<GenotypeMatchDetails>
            {
                DefaultGenotypeMatchDetails1.With(g => g.MatchCounts, tenOutOfTenMatchCounts).Build(),
                DefaultGenotypeMatchDetails2.With(g => g.MatchCounts, singleMismatch).Build()
            };
            

            var expectedMatchProbabilityPerLocus = new LociInfo<decimal?>
                {A = 0.5M, B = 0.5M, C = 0.5M, Dpb1 = null, Dqb1 = 0.5M, Drb1 = 0.25M};

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(
                DefaultPatientGenotypes,
                DefaultDonorGenotypes,
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
                DefaultGenotypeMatchDetails1.With(g => g.MatchCounts, tenOutOfTenMatchCounts).Build(),
                DefaultGenotypeMatchDetails2.With(g => g.MatchCounts, doubleMismatch1).Build()
            };

            var expectedMatchProbabilityPerLocus = new LociInfo<decimal?>
                {A = 0.5M, B = 0.5M, C = 0.5M, Dpb1 = null, Dqb1 = 0.5M, Drb1 = 0.25M};

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(
                DefaultPatientGenotypes,
                DefaultDonorGenotypes,
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
                DefaultGenotypeMatchDetails1.With(g => g.MatchCounts, tenOutOfTenMatchCounts).Build(),
                DefaultGenotypeMatchDetails2.With(g => g.MatchCounts, doubleMismatch2).Build()
            };

            var expectedMatchProbabilityPerLocus = new LociInfo<decimal?>
                {A = 0.5M, B = 0.5M, C = 0.5M, Dpb1 = null, Dqb1 = 0.25M, Drb1 = 0.25M};

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(
                DefaultPatientGenotypes,
                DefaultDonorGenotypes,
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
                DefaultGenotypeMatchDetails1.With(g => g.MatchCounts, tenOutOfTenMatchCounts).Build()
            };

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(
                DefaultPatientGenotypes,
                DefaultDonorGenotypes,
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
                {new PhenotypeInfo<string>(PatientGenotype1), likelihood},
                {new PhenotypeInfo<string>(PatientGenotype2), likelihood},
                {new PhenotypeInfo<string>(DonorGenotype1), likelihood},
                {new PhenotypeInfo<string>(DonorGenotype2), likelihood}
            };
        }

        private static Builder<GenotypeMatchDetails> DefaultGenotypeMatchDetails1 => Builder<GenotypeMatchDetails>.New
            .With(r => r.PatientGenotype, new PhenotypeInfo<string>(PatientGenotype1))
            .With(r => r.DonorGenotype, new PhenotypeInfo<string>(DonorGenotype1));

        private static Builder<GenotypeMatchDetails> DefaultGenotypeMatchDetails2 => Builder<GenotypeMatchDetails>.New
            .With(r => r.PatientGenotype, new PhenotypeInfo<string>(PatientGenotype2))
            .With(r => r.DonorGenotype, new PhenotypeInfo<string>(DonorGenotype2));
    }
}