using System.Collections.Generic;
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

        [TestCase(16, 5, 10)]
        [TestCase(8, 0, 5)]
        [TestCase(4, 0.123, 3)]
        [TestCase(2, 1.344, 3)]
        [TestCase(1, 1.345, 0.343)]
        public void CalculateLikelihood_WhenDiplotypesAreHeterozygous_ReturnsExpectedLikelihood(
            int numberOfDiplotype, decimal frequency1, decimal frequency2)
        {
            var diplotypes = new List<Diplotype>();

            for (var i = 0; i < numberOfDiplotype; i++)
            {
                diplotypes.Add(new Diplotype
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
                });
            }

            var actualLikelihood = likelihoodCalculator.CalculateLikelihood(diplotypes);

            decimal expectedLikelihood = 0;
            for (var i = 0; i < numberOfDiplotype; i++)
            {
                expectedLikelihood += (frequency2 * frequency1 * 2);
            }

            actualLikelihood.Should().Be(expectedLikelihood);
        }

        [TestCase(5)]
        [TestCase(0)]
        [TestCase(1.234)]
        public void CalculateLikelihood_WhenSingleDiplotypeIsHeterozygous_ReturnsExpectedLikelihood(decimal frequency)
        {
            var homozygousDiplotype = new List<Diplotype>
            {
                new Diplotype
                {
                    Item1 = new Haplotype
                    {
                        Hla = new LociInfo<string>
                        {
                            A = "homozygous", B = "homozygous", C = "homozygous", Dqb1 = "homozygous", Drb1 = "homozygous"
                        },
                        Frequency = frequency
                    },
                    Item2 = new Haplotype
                    {
                        Hla = new LociInfo<string>
                        {
                            A = "homozygous", B = "homozygous", C = "homozygous", Dqb1 = "homozygous", Drb1 = "homozygous"
                        },
                        Frequency = frequency
                    }
                }
            };

            var actualLikelihood = likelihoodCalculator.CalculateLikelihood(homozygousDiplotype);

            var expectedLikelihood = (frequency * frequency * 1);

            actualLikelihood.Should().Be(expectedLikelihood);
        }
    }
}