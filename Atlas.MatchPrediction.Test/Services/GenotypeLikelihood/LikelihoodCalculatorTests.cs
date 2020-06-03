using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.GenotypeLikelihood
{
    public class LikelihoodCalculatorTests
    {
        private ILikelihoodCalculator likelihoodCalculator;

        [SetUp]
        public void SetUp()
        {
            likelihoodCalculator = new LikelihoodCalculator();
        }

        [TestCase(16, 5, 10, 1600)]
        [TestCase(8, 0, 5, 0)]
        [TestCase(4, 0.123, 3, 2.952)]
        [TestCase(2, 1.344, 3, 16.128)]
        [TestCase(1, 1.345, 0.343, 0.92267)]
        public void CalculateLikelihood_WhenDiplotypesAreHeterozygous_ReturnsExpectedLikelihood(
            int numberOfDiplotypes,
            decimal frequency1,
            decimal frequency2,
            decimal expectedLikelihood)
        {
            var diplotypes = Enumerable.Range(0, numberOfDiplotypes).Select(i => new Diplotype
            {
                Item1 = new Haplotype
                {
                    Hla = new LociInfo<string> {A = "A-2", B = "B-1", C = "C-1", Dqb1 = "Dqb1-1", Drb1 = "Drb1-1"},
                    Frequency = frequency1
                },
                Item2 = new Haplotype
                {
                    Hla = new LociInfo<string> {A = "A-1", B = "B-2", C = "C-2", Dqb1 = "Dqb1-2", Drb1 = "Drb1-2"},
                    Frequency = frequency2
                }
            }).ToList();

            var actualLikelihood = likelihoodCalculator.CalculateLikelihood(diplotypes);

            actualLikelihood.Should().Be(expectedLikelihood);
        }

        [TestCase(5, 25)]
        [TestCase(0, 0)]
        [TestCase(1.234, 1.522756)]
        public void CalculateLikelihood_ForSingleFullyHomozygousDiplotype_ReturnsExpectedLikelihood(
            decimal frequency,
            decimal expectedLikelihood)
        {
            var homozygousHlaData = new LociInfo<string>
            {
                A = "homozygous",
                B = "homozygous",
                C = "homozygous",
                Dqb1 = "homozygous",
                Drb1 = "homozygous"
            };

            var homozygousDiplotype = new List<Diplotype>
            {
                new Diplotype
                {
                    Item1 = new Haplotype
                    {
                        Hla = homozygousHlaData,
                        Frequency = frequency
                    },
                    Item2 = new Haplotype
                    {
                        Hla = homozygousHlaData,
                        Frequency = frequency
                    }
                }
            };

            var actualLikelihood = likelihoodCalculator.CalculateLikelihood(homozygousDiplotype);

            actualLikelihood.Should().Be(expectedLikelihood);
        }
    }
}