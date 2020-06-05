using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.GenotypeLikelihood
{
    [TestFixture]
    public class GenotypeImputerTests
    {
        private IGenotypeImputer genotypeImputer;

        private const string A1 = "A1:A1";
        private const string A2 = "A2:A2";
        private const string B1 = "B1:B1";
        private const string B2 = "B2:B2";
        private const string C1 = "C1:C1";
        private const string C2 = "C2:C2";
        private const string Dqb11 = "Dqb11:Dqb11";
        private const string Dqb12 = "Dqb12:Dqb12";
        private const string Drb11 = "Drb11:Drb11";
        private const string Drb12 = "Drb12:Drb12";

        [SetUp]
        public void SetUp()
        {
            genotypeImputer = new GenotypeImputer();
        }

        [Test]
        public void GetPossibleDiplotypes_WhenGenotypeIsAllHomozygous_ReturnsExpectedDiplotypes()
        {
            var genotype = PhenotypeInfoBuilder.New
                .With(d => d.A, new LocusInfo<string>("homozygous"))
                .With(d => d.B, new LocusInfo<string>("homozygous"))
                .With(d => d.C, new LocusInfo<string>("homozygous"))
                .With(d => d.Dpb1, new LocusInfo<string>("homozygous"))
                .With(d => d.Dqb1, new LocusInfo<string>("homozygous"))
                .With(d => d.Drb1, new LocusInfo<string>("homozygous"))
                .Build();

            var expectedDiplotypeHla = new Diplotype(genotype).Map(h => h.Hla);

            var imputedGenotype = genotypeImputer.ImputeGenotype(genotype);
            var actualDiplotypeHla = imputedGenotype.Diplotypes.Select(d => d.Map(h => h.Hla));
            var actualHomozygousValue = imputedGenotype.IsHomozygousAtEveryLocus;

            actualDiplotypeHla.Should().BeEquivalentTo(expectedDiplotypeHla);
            actualHomozygousValue.Should().BeTrue();
        }

        [Test]
        public void GetPossibleDiplotypes_WhenGenotypeIsAllHeterozygous_ReturnsExpectedDiplotypes()
        {
            var genotype = PhenotypeInfoBuilder.New.Build();

            var expectedDiplotypeHla = new List<Diplotype>
            {
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = B1, C = C1, Dqb1 = Dqb11, Drb1 = Drb11}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = B2, C = C2, Dqb1 = Dqb12, Drb1 = Drb12}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = B1, C = C1, Dqb1 = Dqb11, Drb1 = Drb12}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = B2, C = C2, Dqb1 = Dqb12, Drb1 = Drb11}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = B1, C = C1, Dqb1 = Dqb12, Drb1 = Drb11}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = B2, C = C2, Dqb1 = Dqb11, Drb1 = Drb12}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = B1, C = C1, Dqb1 = Dqb12, Drb1 = Drb12}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = B2, C = C2, Dqb1 = Dqb11, Drb1 = Drb11}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = B1, C = C2, Dqb1 = Dqb11, Drb1 = Drb11}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = B2, C = C1, Dqb1 = Dqb12, Drb1 = Drb12}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = B1, C = C2, Dqb1 = Dqb11, Drb1 = Drb12}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = B2, C = C1, Dqb1 = Dqb12, Drb1 = Drb11}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = B1, C = C2, Dqb1 = Dqb12, Drb1 = Drb11}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = B2, C = C1, Dqb1 = Dqb11, Drb1 = Drb12}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = B1, C = C2, Dqb1 = Dqb12, Drb1 = Drb12}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = B2, C = C1, Dqb1 = Dqb11, Drb1 = Drb11}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = B2, C = C1, Dqb1 = Dqb11, Drb1 = Drb11}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = B1, C = C2, Dqb1 = Dqb12, Drb1 = Drb12}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = B2, C = C1, Dqb1 = Dqb11, Drb1 = Drb12}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = B1, C = C2, Dqb1 = Dqb12, Drb1 = Drb11}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = B2, C = C1, Dqb1 = Dqb12, Drb1 = Drb11}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = B1, C = C2, Dqb1 = Dqb11, Drb1 = Drb12}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = B2, C = C1, Dqb1 = Dqb12, Drb1 = Drb12}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = B1, C = C2, Dqb1 = Dqb11, Drb1 = Drb11}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = B2, C = C2, Dqb1 = Dqb11, Drb1 = Drb11}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = B1, C = C1, Dqb1 = Dqb12, Drb1 = Drb12}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = B2, C = C2, Dqb1 = Dqb11, Drb1 = Drb12}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = B1, C = C1, Dqb1 = Dqb12, Drb1 = Drb11}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = B2, C = C2, Dqb1 = Dqb12, Drb1 = Drb11}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = B1, C = C1, Dqb1 = Dqb11, Drb1 = Drb12}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = B2, C = C2, Dqb1 = Dqb12, Drb1 = Drb12}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = B1, C = C1, Dqb1 = Dqb11, Drb1 = Drb11}}
                }
            }.Select(d => d.Map(h => h.Hla));

            var imputedGenotype = genotypeImputer.ImputeGenotype(genotype);
            var actualDiplotypeHla = imputedGenotype.Diplotypes.Select(d => d.Map(h => h.Hla));
            var actualHomozygousValue = imputedGenotype.IsHomozygousAtEveryLocus;

            actualDiplotypeHla.Should().BeEquivalentTo(expectedDiplotypeHla);
            actualHomozygousValue.Should().BeFalse();
        }

        [TestCase("homozygous")]
        [TestCase(null)]
        public void GetPossibleDiplotypes_WhenGenotypeHasHomozygousOrEmptyLocusB_ReturnsExpectedDiplotypes(string locusValue)
        {
            var genotype = PhenotypeInfoBuilder.New.With(p => p.B, new LocusInfo<string>(locusValue)).Build();

            var expectedDiplotypeHla = new List<Diplotype>
            {
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = locusValue, C = C1, Dqb1 = Dqb11, Drb1 = Drb11}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = locusValue, C = C2, Dqb1 = Dqb12, Drb1 = Drb12}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = locusValue, C = C1, Dqb1 = Dqb11, Drb1 = Drb12}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = locusValue, C = C2, Dqb1 = Dqb12, Drb1 = Drb11}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = locusValue, C = C1, Dqb1 = Dqb12, Drb1 = Drb11}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = locusValue, C = C2, Dqb1 = Dqb11, Drb1 = Drb12}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = locusValue, C = C1, Dqb1 = Dqb12, Drb1 = Drb12}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = locusValue, C = C2, Dqb1 = Dqb11, Drb1 = Drb11}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = locusValue, C = C2, Dqb1 = Dqb11, Drb1 = Drb11}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = locusValue, C = C1, Dqb1 = Dqb12, Drb1 = Drb12}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = locusValue, C = C2, Dqb1 = Dqb11, Drb1 = Drb12}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = locusValue, C = C1, Dqb1 = Dqb12, Drb1 = Drb11}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = locusValue, C = C2, Dqb1 = Dqb12, Drb1 = Drb11}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = locusValue, C = C1, Dqb1 = Dqb11, Drb1 = Drb12}}
                },
                new Diplotype
                {
                    Item1 = new Haplotype
                        {Hla = new LociInfo<string> {A = A2, B = locusValue, C = C2, Dqb1 = Dqb12, Drb1 = Drb12}},
                    Item2 = new Haplotype
                        {Hla = new LociInfo<string> {A = A1, B = locusValue, C = C1, Dqb1 = Dqb11, Drb1 = Drb11}}
                }
            }.Select(d => d.Map(h => h.Hla));

            var imputedGenotype = genotypeImputer.ImputeGenotype(genotype);
            var actualDiplotypeHla = imputedGenotype.Diplotypes.Select(d => d.Map(h => h.Hla));
            var actualHomozygousValue = imputedGenotype.IsHomozygousAtEveryLocus;

            actualDiplotypeHla.Should().BeEquivalentTo(expectedDiplotypeHla);
            actualHomozygousValue.Should().BeFalse();
        }

        [TestCase(Locus.C)]
        [TestCase(Locus.Dqb1)]
        public void GetPossibleDiplotypes_WhenGenotypeHasEmptyLocus_Returns8Diplotypes(Locus emptyLocus)
        {
            var genotype = PhenotypeInfoBuilder.New.Build();
            genotype.SetLocus(emptyLocus, new LocusInfo<string>(null));

            var imputedGenotype = genotypeImputer.ImputeGenotype(genotype);
            var actualDiplotypes = imputedGenotype.Diplotypes.ToList();
            var actualHomozygousValue = imputedGenotype.IsHomozygousAtEveryLocus;

            actualDiplotypes.Count.Should().Be(8);
            actualHomozygousValue.Should().BeFalse();
        }

        [TestCase(1, 8, false)]
        [TestCase(2, 4, false)]
        [TestCase(3, 2, false)]
        [TestCase(4, 1, false)]
        [TestCase(5, 1, true)]
        public void GetPossibleDiplotypes_WhenGenotypeHasHomozygousCases_ReturnsDiplotypes(
            int numberOfHomozygousLoci,
            int expectedDiplotypeCount,
            bool expectedHomozygousValue)
        {
            var genotype = PhenotypeInfoBuilder.New.Build();

            var lociToMakeHomozygous = LocusSettings.MatchPredictionLoci.Take(numberOfHomozygousLoci);

            foreach (var locus in lociToMakeHomozygous)
            {
                genotype.SetLocus(locus, new LocusInfo<string>("homozygous"));
            }

            var imputedGenotype = genotypeImputer.ImputeGenotype(genotype);
            var actualDiplotypes = imputedGenotype.Diplotypes.ToList();
            var actualHomozygousValue = imputedGenotype.IsHomozygousAtEveryLocus;

            actualHomozygousValue.Should().Be(expectedHomozygousValue);
            actualDiplotypes.Count.Should().Be(expectedDiplotypeCount);
        }

        [TestCase(Locus.A)]
        [TestCase(Locus.B)]
        [TestCase(Locus.C)]
        [TestCase(Locus.Dqb1)]
        [TestCase(Locus.Drb1)]
        public void GetPossibleDiplotypes_WhenGenotypeHasHomozygousCase_Returns8Diplotypes(Locus homozygousLocus)
        {
            var genotype = PhenotypeInfoBuilder.New.Build();
            genotype.SetLocus(homozygousLocus, new LocusInfo<string>("homozygous"));

            var imputedGenotype = genotypeImputer.ImputeGenotype(genotype);
            var actualDiplotypes = imputedGenotype.Diplotypes.ToList();
            var actualHomozygousValue = imputedGenotype.IsHomozygousAtEveryLocus;

            actualDiplotypes.Count.Should().Be(8);
            actualHomozygousValue.Should().BeFalse();
        }

        [Test, Repeat(10000), Ignore("Only used for manual benchmarking. Ran in ~400ms")]
        public void PerformanceTest()
        {
            var genotypeWithAllFields = new PhenotypeInfo<string>
            {
                A = {Position1 = A1, Position2 = A2},
                B = {Position1 = B1, Position2 = B2},
                C = {Position1 = C1, Position2 = C2},
                Dqb1 = {Position1 = Dqb11, Position2 = Dqb12},
                Drb1 = {Position1 = Drb11, Position2 = Drb12}
            };
            genotypeImputer.ImputeGenotype(genotypeWithAllFields);

            var genotypeWithMissingField = new PhenotypeInfo<string>
            {
                A = {Position1 = A1, Position2 = A2},
                B = {Position1 = B1, Position2 = B2},
                Dqb1 = {Position1 = Dqb11, Position2 = Dqb12},
                Drb1 = {Position1 = Drb11, Position2 = Drb12}
            };
            genotypeImputer.ImputeGenotype(genotypeWithMissingField);

            var genotypeWithHomozygousType = new PhenotypeInfo<string>
            {
                A = {Position1 = "homozygous", Position2 = "homozygous"},
                B = {Position1 = B1, Position2 = B2},
                C = {Position1 = C1, Position2 = C2},
                Dqb1 = {Position1 = Dqb11, Position2 = Dqb12},
                Drb1 = {Position1 = Drb11, Position2 = Drb12}
            };
            genotypeImputer.ImputeGenotype(genotypeWithHomozygousType);

            var genotypeWithHomozygousTypeAndMissingField = new PhenotypeInfo<string>
            {
                A = {Position1 = "homozygous", Position2 = "homozygous"},
                B = {Position1 = B1, Position2 = B2},
                Dqb1 = {Position1 = Dqb11, Position2 = Dqb12},
                Drb1 = {Position1 = Drb11, Position2 = Drb12}
            };
            genotypeImputer.ImputeGenotype(genotypeWithHomozygousTypeAndMissingField);
        }
    }
}