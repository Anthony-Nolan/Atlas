using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.MatchProbability;
using FluentAssertions;
using NUnit.Framework;

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

        private readonly LociInfo<int?> tenOutOfTenMatchCounts = new LociInfo<int?> {A = 2, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 2};
        private readonly LociInfo<int?> mismatchMatchCounts = new LociInfo<int?> {A = 2, B = 2, C = 2, Dpb1 = null, Dqb1 = 0, Drb1 = 0};

        private readonly PhenotypeInfo<string> patientGenotype1 = new PhenotypeInfoBuilder<string>()
        .WithDataAt(Locus.A, new LocusInfo<string> { Position1 = PatientLocus1, Position2 = PatientLocus1 }).Build();
        private readonly PhenotypeInfo<string> patientGenotype2 = new PhenotypeInfoBuilder<string>()
        .WithDataAt(Locus.A, new LocusInfo<string> { Position1 = PatientLocus2, Position2 = PatientLocus2 }).Build();
        private readonly PhenotypeInfo<string> donorGenotype1 = new PhenotypeInfoBuilder<string>()
        .WithDataAt(Locus.A, new LocusInfo<string> { Position1 = DonorLocus1, Position2 = DonorLocus1 }).Build();
        private readonly PhenotypeInfo<string> donorGenotype2 = new PhenotypeInfoBuilder<string>()
        .WithDataAt(Locus.A, new LocusInfo<string> { Position1 = DonorLocus2, Position2 = DonorLocus2 }).Build();

        [SetUp]
        public void Setup()
        {
            matchProbabilityCalculator = new MatchProbabilityCalculator();
        }

        [Test]
        public void CalculateMatchProbability_ReturnsMatchProbability()
        {
            var genotypesLikelihoods = new Dictionary<PhenotypeInfo<string>, decimal>
            {
                {patientGenotype1, 0.5m},
                {patientGenotype2, 0.5m},
                {donorGenotype1, 0.5m},
                {donorGenotype2, 0.5m}
            };

            var matchingPairs = new HashSet<GenotypeMatchDetails>
            {
                new GenotypeMatchDetails{PatientGenotype = patientGenotype1, DonorGenotype = donorGenotype1, MatchCounts = tenOutOfTenMatchCounts},
                new GenotypeMatchDetails{PatientGenotype = patientGenotype2, DonorGenotype = donorGenotype2, MatchCounts = mismatchMatchCounts}
            };

            var patientGenotypes = new HashSet<PhenotypeInfo<string>> {patientGenotype1, patientGenotype2};
            var donorGenotypes = new HashSet<PhenotypeInfo<string>> {donorGenotype1, donorGenotype2};

            var expectedMatchProbabilityPerLocus = new LociInfo<Probability>
            {
                A = new Probability(0.5m),
                B = new Probability(0.5m),
                C = new Probability(0.5m),
                Dpb1 = null, 
                Dqb1 = new Probability(0.25m),
                Drb1 = new Probability(0.25m)
            };

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(
                patientGenotypes,
                donorGenotypes,
                matchingPairs,
                genotypesLikelihoods);

            actualProbability.ZeroMismatchProbability.Decimal.Should().Be(0.25m);
            actualProbability.ZeroMismatchProbabilityPerLocus.A.Should().Be(expectedMatchProbabilityPerLocus.A);
            actualProbability.ZeroMismatchProbabilityPerLocus.Should().Be(expectedMatchProbabilityPerLocus);
        }

        [Test]
        public void CalculateMatchProbability_WithUnrepresentedPhenotypes_HasZeroProbability()
        {
            var genotypesLikelihoods = new Dictionary<PhenotypeInfo<string>, decimal>
            {
                {patientGenotype1, 0m},
                {patientGenotype2, 0m},
                {donorGenotype1, 0m},
                {donorGenotype2, 0m}
            };

            var matchingPairs = new HashSet<GenotypeMatchDetails>
                {new GenotypeMatchDetails{PatientGenotype = patientGenotype1, DonorGenotype = donorGenotype1, MatchCounts = tenOutOfTenMatchCounts}};

            var patientGenotypes = new HashSet<PhenotypeInfo<string>> { patientGenotype1, patientGenotype2 };
            var donorGenotypes = new HashSet<PhenotypeInfo<string>> { donorGenotype1, donorGenotype2 };

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(
                patientGenotypes,
                donorGenotypes,
                matchingPairs,
                genotypesLikelihoods);

            actualProbability.ZeroMismatchProbability.Decimal.Should().Be(0m);
        }
    }
}