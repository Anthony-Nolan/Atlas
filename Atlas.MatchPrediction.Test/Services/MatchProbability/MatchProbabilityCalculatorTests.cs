using System;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Services.MatchProbability;
using NUnit.Framework;
using System.Collections.Generic;
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

        [SetUp]
        public void Setup()
        {
            matchProbabilityCalculator = new MatchProbabilityCalculator();
        }

        [Test]
        public void CalculateMatchProbability_ReturnsMatchProbability()
        {
            var patientGenotype1 = PhenotypeInfoBuilder.New
                .With(d => d.A, new LocusInfo<string> { Position1 = PatientLocus1, Position2 = PatientLocus1 }).Build();

            var patientGenotype2 = PhenotypeInfoBuilder.New
                .With(d => d.A, new LocusInfo<string> { Position1 = PatientLocus2, Position2 = PatientLocus2 }).Build();

            var donorGenotype1 = PhenotypeInfoBuilder.New
                .With(d => d.A, new LocusInfo<string> { Position1 = DonorLocus1, Position2 = DonorLocus1 }).Build();

            var donorGenotype2 = PhenotypeInfoBuilder.New
                .With(d => d.A, new LocusInfo<string> { Position1 = DonorLocus2, Position2 = DonorLocus2 }).Build();

            var genotypesLikelihoods = new Dictionary<PhenotypeInfo<string>, decimal>
            {
                {patientGenotype1, 0.5m},
                {patientGenotype2, 0.5m},
                {donorGenotype1, 0.5m},
                {donorGenotype2, 0.5m}
            };

            var matchingPairs = new HashSet<Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>>>
            {
                new Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>>(patientGenotype1, donorGenotype1)
            };

            var patientGenotypes = new HashSet<PhenotypeInfo<string>>
            {
                patientGenotype1,
                patientGenotype2
            };

            var donorGenotypes = new HashSet<PhenotypeInfo<string>>
            {
                donorGenotype1,
                donorGenotype1
            };

            const decimal expectedProbability = 0.25m;

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(
                patientGenotypes,
                donorGenotypes,
                matchingPairs,
                genotypesLikelihoods);

            actualProbability.Should().Be(expectedProbability);
        }
    }
}