using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using FluentAssertions;
using NUnit.Framework;
using static EnumStringValues.EnumExtensions;

namespace Atlas.MatchPrediction.Test.Services.GenotypeLikelihood
{
    [TestFixture]
    public class GenotypeImputationTests
    {
        private IGenotypeImputer genotypeImputer;

        [SetUp]
        public void SetUp()
        {
            genotypeImputer = new GenotypeImputer();
        }

        [Test]
        public void GetPossibleDiplotypes_WhenGenotypeHasAllLociAndNoHomozygousCases_Returns16Diplotypes()
        {
            var genotype = PhenotypeInfoBuilder.New.With(l => l.Dpb1, new LocusInfo<string>(null)).Build();

            var actualDiplotypes = genotypeImputer.GetPossibleDiplotypes(genotype);

            var expectedDiplotypes = new List<DiplotypeInfo<string>>()
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

            actualDiplotypes.Should().BeEquivalentTo(expectedDiplotypes);
        }

        [TestCase(Locus.C)]
        [TestCase(Locus.Dqb1)]
        public void GetPossibleDiplotypes_WhenGenotypeHasEmptyLocus_Returns8Diplotypes(Locus emptyLocus)
        {
            var genotype = PhenotypeInfoBuilder.New.Build();
            genotype.SetLocus(emptyLocus, new LocusInfo<string>(null));

            var diplotypes = genotypeImputer.GetPossibleDiplotypes(genotype);
            diplotypes.Count.Should().Be(8);
        }

        [TestCase(1, 8)]
        [TestCase(2, 4)]
        [TestCase(3, 2)]
        [TestCase(4, 1)]
        [TestCase(5, 1)]
        public void GetPossibleDiplotypes_WhenGenotypeHasHomozygousCases_ReturnsDiplotypes(int numberOfHomozygousLoci, int expectedDiplotypeCount)
        {
            var genotype = PhenotypeInfoBuilder.New.Build();
            
            var lociToMakeHomozygous = EnumerateValues<Locus>().Except(new[] { Locus.Dpb1 }).Take(numberOfHomozygousLoci);

            foreach (var locus in lociToMakeHomozygous)
            {
                genotype.SetLocus(locus, new LocusInfo<string>("homozygous"));
            }

            var diplotypes = genotypeImputer.GetPossibleDiplotypes(genotype);
            diplotypes.Count.Should().Be(expectedDiplotypeCount);
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

            var diplotypes = genotypeImputer.GetPossibleDiplotypes(genotype);
            diplotypes.Count.Should().Be(8);
        }

        [Test, Repeat(10000), Ignore("Only used for manual benchmarking. Ran in ~400ms")]
        public void PerformanceTest()
        {
            var genotypeWithAllFields = new PhenotypeInfo<string>
            {
                A = {Position1 = "A-1", Position2 = "A-2"},
                B = {Position1 = "B-1", Position2 = "B-2"},
                C = {Position1 = "C-1", Position2 = "C-2"},
                Dqb1 = {Position1 = "Dqb1-1", Position2 = "Dqb1-2"},
                Drb1 = {Position1 = "Drb1-1", Position2 = "Drb1-2"}
            };
            genotypeImputer.GetPossibleDiplotypes(genotypeWithAllFields);

            var genotypeWithMissingField = new PhenotypeInfo<string>
            {
                A = {Position1 = "A-1", Position2 = "A-2"},
                B = {Position1 = "B-1", Position2 = "B-2"},
                Dqb1 = {Position1 = "Dqb1-1", Position2 = "Dqb1-2"},
                Drb1 = {Position1 = "Drb1-1", Position2 = "Drb1-2"}
            };
            genotypeImputer.GetPossibleDiplotypes(genotypeWithMissingField);

            var genotypeWithHomozygousType = new PhenotypeInfo<string>
            {
                A = {Position1 = "homozygous", Position2 = "homozygous"},
                B = {Position1 = "B-1", Position2 = "B-2"},
                C = {Position1 = "C-1", Position2 = "C-2"},
                Dqb1 = {Position1 = "Dqb1-1", Position2 = "Dqb1-2"},
                Drb1 = {Position1 = "Drb1-1", Position2 = "Drb1-2"}
            };
            genotypeImputer.GetPossibleDiplotypes(genotypeWithHomozygousType);

            var genotypeWithHomozygousTypeAndMissingField = new PhenotypeInfo<string>
            {
                A = {Position1 = "homozygous", Position2 = "homozygous"},
                B = {Position1 = "B-1", Position2 = "B-2"},
                Dqb1 = {Position1 = "Dqb1-1", Position2 = "Dqb1-2"},
                Drb1 = {Position1 = "Drb1-1", Position2 = "Drb1-2"}
            };
            genotypeImputer.GetPossibleDiplotypes(genotypeWithHomozygousTypeAndMissingField);
        }
    }
}
