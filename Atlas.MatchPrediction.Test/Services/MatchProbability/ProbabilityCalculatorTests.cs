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
    public class ProbabilityCalculatorTests
    {
        private IProbabilityCalculator probabilityCalculator;

        private const string PatientLocus1 = "patientGenotype1";
        private const string PatientLocus2 = "patientGenotype2";
        private const string DonorLocus1 = "donorGenotype1";
        private const string DonorLocus2 = "donorGenotype2";

        private static readonly PhenotypeInfo<string> PatientGenotype1 = PhenotypeInfoBuilder.New
            .With(d => d.A, new LocusInfo<string> {Position1 = PatientLocus1, Position2 = PatientLocus1}).Build();

        private static readonly PhenotypeInfo<string> PatientGenotype2 = PhenotypeInfoBuilder.New
            .With(d => d.A, new LocusInfo<string> {Position1 = PatientLocus2, Position2 = PatientLocus2}).Build();

        private static readonly PhenotypeInfo<string> DonorGenotype1 = PhenotypeInfoBuilder.New
            .With(d => d.A, new LocusInfo<string> {Position1 = DonorLocus1, Position2 = DonorLocus1}).Build();

        private static readonly PhenotypeInfo<string> DonorGenotype2 = PhenotypeInfoBuilder.New
            .With(d => d.A, new LocusInfo<string> {Position1 = DonorLocus2, Position2 = DonorLocus2}).Build();

        private static readonly Dictionary<PhenotypeInfo<string>, decimal> GenotypesLikelihoods = new Dictionary<PhenotypeInfo<string>, decimal>{
            {PatientGenotype1, 0.5m},
            {PatientGenotype2, 0.5m},
            {DonorGenotype1, 0.5m},
            {DonorGenotype2, 0.5m}
        };

        private static readonly HashSet<Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>>> MatchingPairs = 
            new HashSet<Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>>>
            {
                new Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>>(PatientGenotype1, DonorGenotype1)
            };

        private static readonly HashSet<PhenotypeInfo<string>> PatientGenotypes = new HashSet<PhenotypeInfo<string>>
        {
            PatientGenotype1,
            PatientGenotype2
        };

        private static readonly HashSet<PhenotypeInfo<string>> DonorGenotypes = new HashSet<PhenotypeInfo<string>>
        {
            DonorGenotype1,
            DonorGenotype2
        };

        [SetUp]
        public void Setup()
        {
            probabilityCalculator = new ProbabilityCalculator();
        }

        [Test]
        public void CalculateMatchProbability_ReturnsMatchProbability()
        {
            const decimal expectedProbability = 0.25m;

            var actualProbability = probabilityCalculator.CalculateMatchProbability(
                PatientGenotypes,
                DonorGenotypes,
                MatchingPairs,
                GenotypesLikelihoods);

            actualProbability.Should().Be(expectedProbability);
        }
    }
}