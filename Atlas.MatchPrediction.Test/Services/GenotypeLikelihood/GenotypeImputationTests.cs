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
        public void GetListOfDiplotypes_WhenGenotypeHasAllLocusAndNoHomozygousCases_ReturnsListOf16Diplotypes()
        {
            var genotype = new PhenotypeInfo<string>
            {
                A = {Position1 = "*01:01", Position2 = "*03:01" },
                B = { Position1 = "*08:01", Position2 = "*07:02" },
                C = { Position1 = "*07:01", Position2 = "*07:02" },
                Dqb1 = { Position1 = "*02:01", Position2 = "*06:02" },
                Drb1 = { Position1 = "*03:01", Position2 = "*15:01" }
            };

            var diplotypes = genotypeImputation.GetPossibleDiplotypes(genotype);
            diplotypes.Count.Should().Be(16);
        }

        [Test]
        public void GetListOfDiplotypes_WhenGenotypeHasNoCLocusAndNoHomozygousCases_ReturnsListOf8Diplotypes()
        {
            var genotype = new PhenotypeInfo<string>
            {
                A = { Position1 = "*01:01", Position2 = "*03:01" },
                B = { Position1 = "*08:01", Position2 = "*07:02" },
                Dqb1 = { Position1 = "*02:01", Position2 = "*06:02" },
                Drb1 = { Position1 = "*03:01", Position2 = "*15:01" }
            };

            var diplotypes = genotypeImputation.GetPossibleDiplotypes(genotype);
            diplotypes.Count.Should().Be(8);
        }

        [Test]
        public void GetListOfDiplotypes_WhenGenotypeHasAllLocusAndHomozygousCases_ReturnsListOf8Diplotypes()
        {
            var genotype = new PhenotypeInfo<string>
            {
                A = { Position1 = "*01:01", Position2 = "*01:01" },
                B = { Position1 = "*08:01", Position2 = "*07:02" },
                C = { Position1 = "*07:01", Position2 = "*07:02" },
                Dqb1 = { Position1 = "*02:01", Position2 = "*06:02" },
                Drb1 = { Position1 = "*03:01", Position2 = "*15:01" }
            };

            var diplotypes = genotypeImputation.GetPossibleDiplotypes(genotype);
            diplotypes.Count.Should().Be(8);
        }

        [Test]
        public void GetListOfDiplotypes_WhenGenotypeHasNoCLocusAndHomozygousCases_ReturnsListOf4Diplotypes()
        {
            var genotype = new PhenotypeInfo<string>
            {
                A = { Position1 = "*01:01", Position2 = "*01:01" },
                B = { Position1 = "*08:01", Position2 = "*07:02" },
                Dqb1 = { Position1 = "*02:01", Position2 = "*06:02" },
                Drb1 = { Position1 = "*03:01", Position2 = "*15:01" }
            };

            var diplotypes = genotypeImputation.GetPossibleDiplotypes(genotype);
            diplotypes.Count.Should().Be(4);
        }

        [Test, Repeat(10000), Ignore("Only used for manual benchmarking. Ran in ~533ms")]
        public void PerformanceTest()
        {
            genotypeImputation.GetPossibleDiplotypes(new PhenotypeInfo<string>
            {
                A = { Position1 = "*01:01", Position2 = "*03:01" },
                B = { Position1 = "*08:01", Position2 = "*07:02" },
                C = { Position1 = "*07:01", Position2 = "*07:02" },
                Dqb1 = { Position1 = "*02:01", Position2 = "*06:02" },
                Drb1 = { Position1 = "*03:01", Position2 = "*15:01" }
            });
            genotypeImputation.GetPossibleDiplotypes(new PhenotypeInfo<string>
            {
                A = { Position1 = "*01:01", Position2 = "*03:01" },
                B = { Position1 = "*08:01", Position2 = "*07:02" },
                Dqb1 = { Position1 = "*02:01", Position2 = "*06:02" },
                Drb1 = { Position1 = "*03:01", Position2 = "*15:01" }
            });
            genotypeImputation.GetPossibleDiplotypes(new PhenotypeInfo<string>
            {
                A = { Position1 = "*01:01", Position2 = "*01:01" },
                B = { Position1 = "*08:01", Position2 = "*07:02" },
                C = { Position1 = "*07:01", Position2 = "*07:02" },
                Dqb1 = { Position1 = "*02:01", Position2 = "*06:02" },
                Drb1 = { Position1 = "*03:01", Position2 = "*15:01" }
            });
            genotypeImputation.GetPossibleDiplotypes(new PhenotypeInfo<string>
            {
                A = { Position1 = "*01:01", Position2 = "*01:01" },
                B = { Position1 = "*08:01", Position2 = "*07:02" },
                Dqb1 = { Position1 = "*02:01", Position2 = "*06:02" },
                Drb1 = { Position1 = "*03:01", Position2 = "*15:01" }
            });
        }
    }
}
