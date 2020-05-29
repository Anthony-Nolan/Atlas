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
    public class GenotypeImputerTests
    {

        private List<DiplotypeInfo<string>> listOfDiplotypes; 


        private IGenotypeImputer genotypeImputer;

        [SetUp]
        public void SetUp()
        {
            genotypeImputer = new GenotypeImputer();

            listOfDiplotypes = new List<DiplotypeInfo<string>> 
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
        }

        [Test]
        public void GetPossibleDiplotypes_WhenGenotypeIsAllHeterozygous_ReturnsDiplotypes()
        {
            var genotype = PhenotypeInfoBuilder.New
                .With(d => d.A, new LocusInfo<string>("homozygous"))
                .With(d => d.B, new LocusInfo<string>("homozygous"))
                .With(d => d.C, new LocusInfo<string>("homozygous"))
                .With(d => d.Dpb1, new LocusInfo<string>("homozygous"))
                .With(d => d.Dqb1, new LocusInfo<string>("homozygous"))
                .With(d => d.Drb1, new LocusInfo<string>("homozygous"))
                .Build();
            var expectedDiplotypes = new DiplotypeInfo<string>(genotype);

            var actualDiplotypes = genotypeImputer.GetPossibleDiplotypes(genotype);

            actualDiplotypes.Should().BeEquivalentTo(expectedDiplotypes);
        }

        [Test]
        public void GetPossibleDiplotypes_WhenGenotypeIsAllHomozygous_ReturnsDiplotypes()
        {
            var genotype = PhenotypeInfoBuilder.New.Build();
            var expectedDiplotypes = listOfDiplotypes;

            var actualDiplotypes = genotypeImputer.GetPossibleDiplotypes(genotype);

            actualDiplotypes.Should().BeEquivalentTo(expectedDiplotypes);
        }

        [TestCase(1, 8)]
        [TestCase(2, 4)]
        [TestCase(3, 2)]
        [TestCase(4, 1)]
        public void GetPossibleDiplotypes_WhenGenotypeHasEmptyLoci_ReturnsDiplotypes(int numberOfEmptyLoci, int expectedDiplotypeCount)
        {
            var lociToMakeHomozygous = EnumerateValues<Locus>().Except(new[] { Locus.A, Locus.Dpb1 }).Take(numberOfEmptyLoci);

            var genotype = PhenotypeInfoBuilder.New.Build();
            var expectedDiplotypes = listOfDiplotypes.GetRange(0, expectedDiplotypeCount);

            foreach (var locus in lociToMakeHomozygous)
            {
                genotype.SetLocus(locus, new LocusInfo<string>(null));

                foreach (var diplotype in expectedDiplotypes)
                {
                    diplotype.SetAtLocus(locus, new LocusInfo<string>(null));
                }
            }

            var actualDiplotypes = genotypeImputer.GetPossibleDiplotypes(genotype);

            actualDiplotypes.Should().BeEquivalentTo(expectedDiplotypes);
        }

        [TestCase(1, 8)]
        [TestCase(2, 4)]
        [TestCase(3, 2)]
        [TestCase(4, 1)]
        public void GetPossibleDiplotypes_WhenGenotypeHasHomozygousCases_ReturnsDiplotypes(int numberOfHomozygousLoci, int expectedDiplotypeCount)
        {
            var expectedDiplotypes = listOfDiplotypes.GetRange(0, expectedDiplotypeCount);
            var lociToMakeHomozygous = EnumerateValues<Locus>().Except(new[] { Locus.A, Locus.Dpb1 }).Take(numberOfHomozygousLoci);

            var genotype = PhenotypeInfoBuilder.New.Build();

            foreach (var locus in lociToMakeHomozygous)
            {
                genotype.SetLocus(locus, new LocusInfo<string>("homozygous"));

                foreach (var diplotype in expectedDiplotypes)
                {
                    diplotype.SetAtLocus(locus, new LocusInfo<string>("homozygous"));
                }
            }

            var actualDiplotypes = genotypeImputer.GetPossibleDiplotypes(genotype);

            actualDiplotypes.Should().BeEquivalentTo(expectedDiplotypes);
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
