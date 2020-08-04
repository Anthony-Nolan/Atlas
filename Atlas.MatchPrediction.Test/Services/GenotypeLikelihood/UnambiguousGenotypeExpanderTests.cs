using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.GenotypeLikelihood
{
    [TestFixture]
    public class UnambiguousGenotypeExpanderTests
    {
        private IUnambiguousGenotypeExpander unambiguousGenotypeExpander;

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

        private readonly ISet<Locus> allLoci = LocusSettings.MatchPredictionLoci;

        [SetUp]
        public void SetUp()
        {
            unambiguousGenotypeExpander = new UnambiguousGenotypeExpander();
        }

        [Test]
        public void GetPossibleDiplotypes_WhenGenotypeIsAllHomozygous_ReturnsExpectedDiplotypes()
        {
            var genotype = new PhenotypeInfoBuilder<string>()
                .WithDataAt(Locus.A, "homozygous")
                .WithDataAt(Locus.B, "homozygous")
                .WithDataAt(Locus.C, "homozygous")
                .WithDataAt(Locus.Dpb1, "homozygous")
                .WithDataAt(Locus.Dqb1, "homozygous")
                .WithDataAt(Locus.Drb1, "homozygous")
                .Build();

            var expectedDiplotypeHla = new Diplotype(genotype).Map(h => h.Hla);

            var expandedGenotype = unambiguousGenotypeExpander.ExpandGenotype(genotype, allLoci);
            var actualDiplotypeHla = expandedGenotype.Diplotypes.Select(d => d.Map(h => h.Hla));
            var actualHomozygousValue = expandedGenotype.IsHomozygousAtEveryLocus;

            actualDiplotypeHla.Should().BeEquivalentTo(expectedDiplotypeHla);
            actualHomozygousValue.Should().BeTrue();
        }

        [Test]
        public void GetPossibleDiplotypes_WhenGenotypeIsAllHeterozygous_ReturnsExpectedDiplotypes()
        {
            var genotype = FullyHeterozygousGenotypeBuilder.Build();

            var expectedDiplotypeHla = new List<Diplotype>
            {
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = B1, C = C1, Dqb1 = Dqb11, Drb1 = Drb11}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = B2, C = C2, Dqb1 = Dqb12, Drb1 = Drb12}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = B1, C = C1, Dqb1 = Dqb11, Drb1 = Drb12}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = B2, C = C2, Dqb1 = Dqb12, Drb1 = Drb11}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = B1, C = C1, Dqb1 = Dqb12, Drb1 = Drb11}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = B2, C = C2, Dqb1 = Dqb11, Drb1 = Drb12}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = B1, C = C1, Dqb1 = Dqb12, Drb1 = Drb12}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = B2, C = C2, Dqb1 = Dqb11, Drb1 = Drb11}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = B1, C = C2, Dqb1 = Dqb11, Drb1 = Drb11}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = B2, C = C1, Dqb1 = Dqb12, Drb1 = Drb12}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = B1, C = C2, Dqb1 = Dqb11, Drb1 = Drb12}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = B2, C = C1, Dqb1 = Dqb12, Drb1 = Drb11}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = B1, C = C2, Dqb1 = Dqb12, Drb1 = Drb11}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = B2, C = C1, Dqb1 = Dqb11, Drb1 = Drb12}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = B1, C = C2, Dqb1 = Dqb12, Drb1 = Drb12}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = B2, C = C1, Dqb1 = Dqb11, Drb1 = Drb11}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = B2, C = C1, Dqb1 = Dqb11, Drb1 = Drb11}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = B1, C = C2, Dqb1 = Dqb12, Drb1 = Drb12}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = B2, C = C1, Dqb1 = Dqb11, Drb1 = Drb12}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = B1, C = C2, Dqb1 = Dqb12, Drb1 = Drb11}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = B2, C = C1, Dqb1 = Dqb12, Drb1 = Drb11}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = B1, C = C2, Dqb1 = Dqb11, Drb1 = Drb12}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = B2, C = C1, Dqb1 = Dqb12, Drb1 = Drb12}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = B1, C = C2, Dqb1 = Dqb11, Drb1 = Drb11}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = B2, C = C2, Dqb1 = Dqb11, Drb1 = Drb11}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = B1, C = C1, Dqb1 = Dqb12, Drb1 = Drb12}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = B2, C = C2, Dqb1 = Dqb11, Drb1 = Drb12}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = B1, C = C1, Dqb1 = Dqb12, Drb1 = Drb11}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = B2, C = C2, Dqb1 = Dqb12, Drb1 = Drb11}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = B1, C = C1, Dqb1 = Dqb11, Drb1 = Drb12}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = B2, C = C2, Dqb1 = Dqb12, Drb1 = Drb12}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = B1, C = C1, Dqb1 = Dqb11, Drb1 = Drb11}}
                )
            }.Select(d => d.Map(h => h.Hla));

            var expandedGenotype = unambiguousGenotypeExpander.ExpandGenotype(genotype, allLoci);
            var actualDiplotypeHla = expandedGenotype.Diplotypes.Select(d => d.Map(h => h.Hla));
            var actualHomozygousValue = expandedGenotype.IsHomozygousAtEveryLocus;

            actualDiplotypeHla.Should().BeEquivalentTo(expectedDiplotypeHla);
            actualHomozygousValue.Should().BeFalse();
        }

        [TestCase("homozygous")]
        [TestCase(null)]
        public void GetPossibleDiplotypes_WhenGenotypeHasHomozygousOrEmptyLocusB_ReturnsExpectedDiplotypes(string locusValue)
        {
            var genotype = FullyHeterozygousGenotypeBuilder.WithDataAt(Locus.B, locusValue).Build();

            var expectedDiplotypeHla = new List<Diplotype>
            {
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = locusValue, C = C1, Dqb1 = Dqb11, Drb1 = Drb11}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = locusValue, C = C2, Dqb1 = Dqb12, Drb1 = Drb12}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = locusValue, C = C1, Dqb1 = Dqb11, Drb1 = Drb12}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = locusValue, C = C2, Dqb1 = Dqb12, Drb1 = Drb11}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = locusValue, C = C1, Dqb1 = Dqb12, Drb1 = Drb11}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = locusValue, C = C2, Dqb1 = Dqb11, Drb1 = Drb12}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = locusValue, C = C1, Dqb1 = Dqb12, Drb1 = Drb12}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = locusValue, C = C2, Dqb1 = Dqb11, Drb1 = Drb11}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = locusValue, C = C2, Dqb1 = Dqb11, Drb1 = Drb11}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = locusValue, C = C1, Dqb1 = Dqb12, Drb1 = Drb12}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = locusValue, C = C2, Dqb1 = Dqb11, Drb1 = Drb12}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = locusValue, C = C1, Dqb1 = Dqb12, Drb1 = Drb11}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = locusValue, C = C2, Dqb1 = Dqb12, Drb1 = Drb11}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = locusValue, C = C1, Dqb1 = Dqb11, Drb1 = Drb12}}
                ),
                new Diplotype
                (
                    new Haplotype {Hla = new LociInfo<string> {A = A2, B = locusValue, C = C2, Dqb1 = Dqb12, Drb1 = Drb12}},
                    new Haplotype {Hla = new LociInfo<string> {A = A1, B = locusValue, C = C1, Dqb1 = Dqb11, Drb1 = Drb11}}
                )
            }.Select(d => d.Map(h => h.Hla));

            var expandedGenotype = unambiguousGenotypeExpander.ExpandGenotype(genotype, allLoci);
            var actualDiplotypeHla = expandedGenotype.Diplotypes.Select(d => d.Map(h => h.Hla));
            var actualHomozygousValue = expandedGenotype.IsHomozygousAtEveryLocus;

            actualDiplotypeHla.Should().BeEquivalentTo(expectedDiplotypeHla);
            actualHomozygousValue.Should().BeFalse();
        }

        [TestCase(Locus.C)]
        [TestCase(Locus.Dqb1)]
        public void GetPossibleDiplotypes_WhenGenotypeHasEmptyLocus_Returns8Diplotypes(Locus emptyLocus)
        {
            var genotype = FullyHeterozygousGenotypeBuilder.Build();
            genotype.SetLocus(emptyLocus, new LocusInfo<string>(null));

            var expandedGenotype = unambiguousGenotypeExpander.ExpandGenotype(genotype, allLoci);
            var actualDiplotypes = expandedGenotype.Diplotypes.ToList();
            var actualHomozygousValue = expandedGenotype.IsHomozygousAtEveryLocus;

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
            var genotype = FullyHeterozygousGenotypeBuilder.Build();

            var lociToMakeHomozygous = LocusSettings.MatchPredictionLoci.Take(numberOfHomozygousLoci);

            foreach (var locus in lociToMakeHomozygous)
            {
                genotype.SetLocus(locus, new LocusInfo<string>("homozygous"));
            }

            var expandedGenotype = unambiguousGenotypeExpander.ExpandGenotype(genotype, allLoci);
            var actualDiplotypes = expandedGenotype.Diplotypes.ToList();
            var actualHomozygousValue = expandedGenotype.IsHomozygousAtEveryLocus;

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
            var genotype = FullyHeterozygousGenotypeBuilder.Build();
            genotype.SetLocus(homozygousLocus, new LocusInfo<string>("homozygous"));

            var expandedGenotype = unambiguousGenotypeExpander.ExpandGenotype(genotype, allLoci);
            var actualDiplotypes = expandedGenotype.Diplotypes.ToList();
            var actualHomozygousValue = expandedGenotype.IsHomozygousAtEveryLocus;

            actualDiplotypes.Count.Should().Be(8);
            actualHomozygousValue.Should().BeFalse();
        }

        [Test, Repeat(10000), IgnoreExceptOnCiPerfTest("Ran in ~400ms")]
        public void PerformanceTest()
        {
            var genotypeWithAllFields = new PhenotypeInfo<string>
            {
                A = new LocusInfo<string>(A1, A2),
                B = new LocusInfo<string>(B1, B2),
                C = new LocusInfo<string>(C1, C2),
                Dqb1 = new LocusInfo<string>(Dqb11, Dqb12),
                Drb1 = new LocusInfo<string>(Drb11, Drb12)
            };
            unambiguousGenotypeExpander.ExpandGenotype(genotypeWithAllFields, allLoci);

            var genotypeWithMissingField = new PhenotypeInfo<string>
            {
                A = new LocusInfo<string>(A1, A2),
                B = new LocusInfo<string>(B1, B2),
                Dqb1 = new LocusInfo<string>(Dqb11, Dqb12),
                Drb1 = new LocusInfo<string>(Drb11, Drb12)
            };
            unambiguousGenotypeExpander.ExpandGenotype(genotypeWithMissingField, allLoci);

            var genotypeWithHomozygousType = new PhenotypeInfo<string>
            {
                A = new LocusInfo<string>("homozygous", "homozygous"),
                B = new LocusInfo<string>(B1, B2),
                C = new LocusInfo<string>(C1, C2),
                Dqb1 = new LocusInfo<string>(Dqb11, Dqb12),
                Drb1 = new LocusInfo<string>(Drb11, Drb12)
            };
            unambiguousGenotypeExpander.ExpandGenotype(genotypeWithHomozygousType, allLoci);

            var genotypeWithHomozygousTypeAndMissingField = new PhenotypeInfo<string>
            {
                A = new LocusInfo<string>("homozygous", "homozygous"),
                B = new LocusInfo<string>(B1, B2),
                Dqb1 = new LocusInfo<string>(Dqb11, Dqb12),
                Drb1 = new LocusInfo<string>(Drb11, Drb12)
            };
            unambiguousGenotypeExpander.ExpandGenotype(genotypeWithHomozygousTypeAndMissingField, allLoci);
        }

        private static PhenotypeInfoBuilder<string> FullyHeterozygousGenotypeBuilder => new PhenotypeInfoBuilder<string>(new PhenotypeInfo<string>
        {
            A = new LocusInfo<string>(A1, A2),
            B = new LocusInfo<string>(B1, B2),
            C = new LocusInfo<string>(C1, C2),
            Dqb1 = new LocusInfo<string>(Dqb11, Dqb12),
            Drb1 = new LocusInfo<string>(Drb11, Drb12)
        });
    }
}