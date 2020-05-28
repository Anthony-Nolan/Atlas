using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.GenotypeLikelihood
{
    [TestFixture]
    public class GenotypeImputationTests
    {
        private IGenotypeImputation genotypeImputation;

        [SetUp]
        public void SetUp()
        {
            genotypeImputation = new GenotypeImputation();
        }

        [Test]
        public void GetPossibleDiplotypes_WhenGenotypeHasAllLociAndNoHomozygousCases_Returns16Diplotypes()
        {
            var genotype = new PhenotypeInfo<string>
            {
                A = {Position1 = "A-1", Position2 = "A-2" },
                B = { Position1 = "B-1", Position2 = "B-2" },
                C = { Position1 = "C-1", Position2 = "C-2" },
                Dqb1 = { Position1 = "Dqb1-1", Position2 = "Dqb1-2" },
                Drb1 = { Position1 = "Drb1-1", Position2 = "Drb1-2" }
            };

            var diplotypesFromGenotype = genotypeImputation.GetPossibleDiplotypes(genotype);

            var diplotypes = new List<DiplotypeInfo<string>>()
            {
                new DiplotypeInfo<string>
                {
                    Haplotype1 = new LociInfo<string>() {A = "A-2", B = "B-1", C = "C-1", Dqb1 = "Dqb1-1", Drb1 = "Drb1-1"},
                    Haplotype2 = new LociInfo<string>() {A = "A-1", B = "B-2", C = "C-2", Dqb1 = "Dqb1-2", Drb1 = "Drb1-2"}
                },
                new DiplotypeInfo<string>
                {
                    Haplotype1 = new LociInfo<string>() {A = "A-2", B = "B-1", C = "C-1", Dqb1 = "Dqb1-1", Drb1 = "Drb1-2"},
                    Haplotype2 = new LociInfo<string>() {A = "A-1", B = "B-2", C = "C-2", Dqb1 = "Dqb1-2", Drb1 = "Drb1-1"}
                },
                new DiplotypeInfo<string>
                {
                    Haplotype1 = new LociInfo<string>() {A = "A-2", B = "B-1", C = "C-1", Dqb1 = "Dqb1-2", Drb1 = "Drb1-1"},
                    Haplotype2 = new LociInfo<string>() {A = "A-1", B = "B-2", C = "C-2", Dqb1 = "Dqb1-1", Drb1 = "Drb1-2"}
                },
                new DiplotypeInfo<string>
                {
                    Haplotype1 = new LociInfo<string>() {A = "A-2", B = "B-1", C = "C-1", Dqb1 = "Dqb1-2", Drb1 = "Drb1-2"},
                    Haplotype2 = new LociInfo<string>() {A = "A-1", B = "B-2", C = "C-2", Dqb1 = "Dqb1-1", Drb1 = "Drb1-1"}
                },
                new DiplotypeInfo<string>
                {
                    Haplotype1 = new LociInfo<string>() {A = "A-2", B = "B-1", C = "C-2", Dqb1 = "Dqb1-1", Drb1 = "Drb1-1"},
                    Haplotype2 = new LociInfo<string>() {A = "A-1", B = "B-2", C = "C-1", Dqb1 = "Dqb1-2", Drb1 = "Drb1-2"}
                },
                new DiplotypeInfo<string>
                {
                    Haplotype1 = new LociInfo<string>() {A = "A-2", B = "B-1", C = "C-2", Dqb1 = "Dqb1-1", Drb1 = "Drb1-2"},
                    Haplotype2 = new LociInfo<string>() {A = "A-1", B = "B-2", C = "C-1", Dqb1 = "Dqb1-2", Drb1 = "Drb1-1"}
                },
                new DiplotypeInfo<string> 
                {
                    Haplotype1 = new LociInfo<string>() {A = "A-2", B = "B-1", C = "C-2", Dqb1 = "Dqb1-2", Drb1 = "Drb1-1"},
                    Haplotype2 = new LociInfo<string>() {A = "A-1", B = "B-2", C = "C-1", Dqb1 = "Dqb1-1", Drb1 = "Drb1-2"}
                },
                new DiplotypeInfo<string>
                {
                    Haplotype1 = new LociInfo<string>() {A = "A-2", B = "B-1", C = "C-2", Dqb1 = "Dqb1-2", Drb1 = "Drb1-2"},
                    Haplotype2 = new LociInfo<string>() {A = "A-1", B = "B-2", C = "C-1", Dqb1 = "Dqb1-1", Drb1 = "Drb1-1"}
                },
                new DiplotypeInfo<string>
                {
                    Haplotype1 = new LociInfo<string>() {A = "A-2", B = "B-2", C = "C-1", Dqb1 = "Dqb1-1", Drb1 = "Drb1-1"},
                    Haplotype2 = new LociInfo<string>() {A = "A-1", B = "B-1", C = "C-2", Dqb1 = "Dqb1-2", Drb1 = "Drb1-2"}
                },
                new DiplotypeInfo<string>
                {
                    Haplotype1 = new LociInfo<string>() {A = "A-2", B = "B-2", C = "C-1", Dqb1 = "Dqb1-1", Drb1 = "Drb1-2"},
                    Haplotype2 = new LociInfo<string>() {A = "A-1", B = "B-1", C = "C-2", Dqb1 = "Dqb1-2", Drb1 = "Drb1-1"}
                },
                new DiplotypeInfo<string>
                {
                    Haplotype1 = new LociInfo<string>() {A = "A-2", B = "B-2", C = "C-1", Dqb1 = "Dqb1-2", Drb1 = "Drb1-1"},
                    Haplotype2 = new LociInfo<string>() {A = "A-1", B = "B-1", C = "C-2", Dqb1 = "Dqb1-1", Drb1 = "Drb1-2"}
                },
                new DiplotypeInfo<string>
                {
                    Haplotype1 = new LociInfo<string>() {A = "A-2", B = "B-2", C = "C-1", Dqb1 = "Dqb1-2", Drb1 = "Drb1-2"},
                    Haplotype2 = new LociInfo<string>() {A = "A-1", B = "B-1", C = "C-2", Dqb1 = "Dqb1-1", Drb1 = "Drb1-1"}
                },
                new DiplotypeInfo<string>
                {
                    Haplotype1 = new LociInfo<string>() {A = "A-2", B = "B-2", C = "C-2", Dqb1 = "Dqb1-1", Drb1 = "Drb1-1"},
                    Haplotype2 = new LociInfo<string>() {A = "A-1", B = "B-1", C = "C-1", Dqb1 = "Dqb1-2", Drb1 = "Drb1-2"}
                },
                new DiplotypeInfo<string>
                {
                    Haplotype1 = new LociInfo<string>() {A = "A-2", B = "B-2", C = "C-2", Dqb1 = "Dqb1-1", Drb1 = "Drb1-2"},
                    Haplotype2 = new LociInfo<string>() {A = "A-1", B = "B-1", C = "C-1", Dqb1 = "Dqb1-2", Drb1 = "Drb1-1"}
                },
                new DiplotypeInfo<string>
                {
                    Haplotype1 = new LociInfo<string>() {A = "A-2", B = "B-2", C = "C-2", Dqb1 = "Dqb1-2", Drb1 = "Drb1-1"},
                    Haplotype2 = new LociInfo<string>() {A = "A-1", B = "B-1", C = "C-1", Dqb1 = "Dqb1-1", Drb1 = "Drb1-2"}
                },
                new DiplotypeInfo<string>
                {
                    Haplotype1 = new LociInfo<string>() {A = "A-2", B = "B-2", C = "C-2", Dqb1 = "Dqb1-2", Drb1 = "Drb1-2"},
                    Haplotype2 = new LociInfo<string>() {A = "A-1", B = "B-1", C = "C-1", Dqb1 = "Dqb1-1", Drb1 = "Drb1-1"}
                }
            };

            diplotypesFromGenotype.Should().BeEquivalentTo(diplotypes);
        }

        [TestCase(Locus.C)]
        [TestCase(Locus.Dqb1)]
        public void GetPossibleDiplotypes_WhenGenotypeHasEmptyLocus_ReturnsDiplotypes(Locus locusToIgnore)
        {
            var genotype = new PhenotypeInfo<string>
            {
                A = { Position1 = "A-1", Position2 = "A-2" },
                B = { Position1 = "B-1", Position2 = "B-2" },
                C = { Position1 = "C-1", Position2 = "C-2" },
                Dqb1 = { Position1 = "Dqb1-1", Position2 = "Dqb1-2" },
                Drb1 = { Position1 = "Drb1-1", Position2 = "Drb1-2" }
            };

            genotype.SetLocus(locusToIgnore, new LocusInfo<string> { Position1 = null, Position2 = null });

            var diplotypes = genotypeImputation.GetPossibleDiplotypes(genotype);
            diplotypes.Count.Should().Be(8);
        }

        [TestCase(1, 8)]
        [TestCase(2, 4)]
        [TestCase(3, 2)]
        [TestCase(4, 1)]
        [TestCase(5, 1)]
        public void GetPossibleDiplotypes_WhenGenotypeHasHomozygousCases_ReturnsDiplotypes(int numberOfHomozygousLoci, int expectedDiplotypeCount)
        {
            var genotype = new PhenotypeInfo<string>
            {
                A = { Position1 = "A-1", Position2 = "A-2" },
                B = { Position1 = "B-1", Position2 = "B-2" },
                C = { Position1 = "C-1", Position2 = "C-2" },
                Dqb1 = { Position1 = "Dqb1-1", Position2 = "Dqb1-2" },
                Drb1 = { Position1 = "Drb1-1", Position2 = "Drb1-2" }
            };

            genotype.EachLocus((locus, locusInfo) =>
            {
                if (numberOfHomozygousLoci <= 0 || locus == Locus.Dpb1) return;
                genotype.SetLocus(locus,
                    new LocusInfo<string> {Position1 = "homozygous", Position2 = "homozygous"});
                numberOfHomozygousLoci += -1;
            });

            var diplotypes = genotypeImputation.GetPossibleDiplotypes(genotype);
            diplotypes.Count.Should().Be(expectedDiplotypeCount);
        }

        [TestCase(Locus.A)]
        [TestCase(Locus.C)]
        [TestCase(Locus.B)]
        [TestCase(Locus.Dqb1)]
        [TestCase(Locus.Drb1)]
        public void GetPossibleDiplotypes_WhenGenotypeHasHomozygousCase_Returns4Diplotypes(Locus homozygousLocus)
        {
            var genotype = new PhenotypeInfo<string>
            {
                A = { Position1 = "A-1", Position2 = "A-2" },
                B = { Position1 = "B-1", Position2 = "B-2" },
                C = { Position1 = "C-1", Position2 = "C-2" },
                Dqb1 = { Position1 = "Dqb1-1", Position2 = "Dqb1-2" },
                Drb1 = { Position1 = "Drb1-1", Position2 = "Drb1-2" }
            };

            genotype.SetLocus(homozygousLocus, new LocusInfo<string> { Position1 = "homozygous", Position2 = "homozygous" });

            var diplotypes = genotypeImputation.GetPossibleDiplotypes(genotype);
            diplotypes.Count.Should().Be(8);
        }

        [Test, Repeat(10000), Ignore("Only used for manual benchmarking. Ran in ~408ms")]
        public void PerformanceTest()
        {
            genotypeImputation.GetPossibleDiplotypes(new PhenotypeInfo<string>
            {
                A = { Position1 = "A-1", Position2 = "A-2" },
                B = { Position1 = "B-1", Position2 = "B-2" },
                C = { Position1 = "C-1", Position2 = "C-2" },
                Dqb1 = { Position1 = "Dqb1-1", Position2 = "Dqb1-2" },
                Drb1 = { Position1 = "Drb1-1", Position2 = "Drb1-2" }
            });
            genotypeImputation.GetPossibleDiplotypes(new PhenotypeInfo<string>
            {
                A = { Position1 = "A-1", Position2 = "A-2" },
                B = { Position1 = "B-1", Position2 = "B-2" },
                Dqb1 = { Position1 = "Dqb1-1", Position2 = "Dqb1-2" },
                Drb1 = { Position1 = "Drb1-1", Position2 = "Drb1-2" }
            });
            genotypeImputation.GetPossibleDiplotypes(new PhenotypeInfo<string>
            {
                A = { Position1 = "homozygous", Position2 = "homozygous" },
                B = { Position1 = "B-1", Position2 = "B-2" },
                C = { Position1 = "C-1", Position2 = "C-2" },
                Dqb1 = { Position1 = "Dqb1-1", Position2 = "Dqb1-2" },
                Drb1 = { Position1 = "Drb1-1", Position2 = "Drb1-2" }
            });
            genotypeImputation.GetPossibleDiplotypes(new PhenotypeInfo<string>
            {
                A = { Position1 = "homozygous", Position2 = "homozygous" },
                B = { Position1 = "B-1", Position2 = "B-2" },
                Dqb1 = { Position1 = "Dqb1-1", Position2 = "Dqb1-2" },
                Drb1 = { Position1 = "Drb1-1", Position2 = "Drb1-2" }
            });
        }
    }
}
