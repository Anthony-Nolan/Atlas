using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.GenotypeLikelihood
{
    public class LikelihoodCalculatorTests
    {
        private IGenotypeLikelihoodCalculator genotypeLikelihoodCalculator;

        [SetUp]
        public void SetUp()
        {
            genotypeLikelihoodCalculator = new GenotypeLikelihoodCalculator();
        }

        [TestCase(0, 16, 5.0, 10.0, 1600)]
        [TestCase(1, 8, 0.0, 5.0, 0)]
        [TestCase(2, 4, 0.123, 3.0, 2.952)]
        [TestCase(3, 2, 1.344, 3.0, 16.128)]
        [TestCase(4, 1, 1.345, 0.343, 0.92267)]
        public void CalculateLikelihood_WhenDiplotypesAreHeterozygous_ReturnsExpectedLikelihood(
            int numberOfHomozygousCases,
            int numberOfDiplotypes,
            decimal frequency1,
            decimal frequency2,
            decimal expectedLikelihood)
        {
            var hlaInfo1 = new LociInfo<string>("A1:A1", "B1:B1", "C1:C1", valueDqb1: "Dqb11:Dqb11", valueDrb1: "Drb11:Drb11");
            var hlaInfo2 = new LociInfo<string>("A2:A2", "B2:B2", "C2:C2", valueDqb1: "Dqb12:Dqb12", valueDrb1: "Drb12:Drb12");

            var lociToMakeHomozygous = LocusSettings.MatchPredictionLoci.Take(numberOfHomozygousCases);

            foreach (var locus in lociToMakeHomozygous)
            {
                hlaInfo1 = hlaInfo1.SetLocus(locus, "homozygous");
                hlaInfo2 = hlaInfo2.SetLocus(locus, "homozygous");
            }

            var diplotypes = Enumerable.Range(0, numberOfDiplotypes).Select(i =>
                new DiplotypeBuilder()
                    .WithItem1(new Haplotype {Hla = hlaInfo1, Frequency = frequency1})
                    .WithItem2(new Haplotype {Hla = hlaInfo2, Frequency = frequency2})
                    .Build()
            ).ToList();

            var homozygousExpandedGenotype = new ExpandedGenotype
            {
                Diplotypes = diplotypes,
                IsHomozygousAtEveryLocus = false
            };

            var actualLikelihood = genotypeLikelihoodCalculator.CalculateLikelihood(homozygousExpandedGenotype);

            actualLikelihood.Should().Be(expectedLikelihood);
        }

        [TestCase(5.0, 25.0)]
        [TestCase(0, 0.0)]
        [TestCase(1.234, 1.522756)]
        public void CalculateLikelihood_ForSingleFullyHomozygousDiplotype_ReturnsExpectedLikelihood(
            decimal frequency,
            decimal expectedLikelihood)
        {
            var homozygousHlaData = new LociInfo<string>("homozygous").SetLocus(Locus.Dpb1, default);

            var homozygousExpandedGenotype = new ExpandedGenotype
            {
                Diplotypes = new List<Diplotype>
                {
                    new DiplotypeBuilder()
                        .WithItem1(new Haplotype {Hla = homozygousHlaData, Frequency = frequency})
                        .WithItem2(new Haplotype {Hla = homozygousHlaData, Frequency = frequency})
                        .Build()
                },
                IsHomozygousAtEveryLocus = true
            };

            var actualLikelihood = genotypeLikelihoodCalculator.CalculateLikelihood(homozygousExpandedGenotype);

            actualLikelihood.Should().Be(expectedLikelihood);
        }
    }
}