using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.ExpandAmbiguousPhenotype
{
    [TestFixture]
    public class AmbiguousPhenotypeExpanderTests
    {
        private IAmbiguousPhenotypeExpander ambiguousPhenotypeExpander;

        private const string Hla1 = "Hla1";
        private const string Hla2 = "Hla2";
        private const string Hla3 = "Hla3";
        private const string Hla4 = "Hla4";
        private const string Hla5 = "Hla5";
        private const string Hla6 = "Hla6";
        private const string Hla7 = "Hla7";
        private const string Hla8 = "Hla8";
        private const string Hla9 = "Hla9";
        private const string Hla10 = "Hla10";

        [SetUp]
        public void SetUp()
        {
            ambiguousPhenotypeExpander = new AmbiguousPhenotypeExpander();
        }

        [Test]
        public void ExpandPhenotype_WhenOnlyOneAllelePerLocus_ReturnsSingleGenotype()
        {
            var allelesPerLocus = new PhenotypeInfo<List<string>>
            {
                A = new LocusInfo<List<string>>(new List<string> {Hla1}, new List<string> {Hla2}),
                B = new LocusInfo<List<string>>(new List<string> {Hla3}, new List<string> {Hla4}),
                C = new LocusInfo<List<string>>(new List<string> {Hla5}, new List<string> {Hla6}),
                Dqb1 = new LocusInfo<List<string>>(new List<string> {Hla7}, new List<string> {Hla8}),
                Drb1 = new LocusInfo<List<string>>(new List<string> {Hla9}, new List<string> {Hla10}),
            };

            var actualGenotypes = ambiguousPhenotypeExpander.ExpandPhenotype(allelesPerLocus).ToList();

            var expectedGenotype = new PhenotypeInfo<string>
            {
                A = new LocusInfo<string>(Hla1, Hla2),
                B = new LocusInfo<string>(Hla3, Hla4),
                C = new LocusInfo<string>(Hla5, Hla6),
                Dqb1 = new LocusInfo<string>(Hla7, Hla8),
                Drb1 = new LocusInfo<string>(Hla9, Hla10)
            };

            actualGenotypes.Count.Should().Be(1);
            actualGenotypes[0].Should().BeEquivalentTo(expectedGenotype);
        }

        [Test]
        public void ExpandPhenotype_WhenMixOfMultipleAndSingleAllelesPerLocus_ReturnsExpectedGenotypes()
        {
            var allelesPerLocus = new PhenotypeInfo<List<string>>
            {
                A = new LocusInfo<List<string>>(new List<string> {Hla1, Hla2, Hla3}, new List<string> {Hla1, Hla2, Hla3}),
                B = new LocusInfo<List<string>>(new List<string> {Hla3, Hla4}, new List<string> {Hla4}),
                C = new LocusInfo<List<string>>(new List<string> {Hla5}, new List<string> {Hla6}),
                Dqb1 = new LocusInfo<List<string>>(new List<string> {Hla7}, new List<string> {Hla8}),
                Drb1 = new LocusInfo<List<string>>(new List<string> {Hla9}, new List<string> {Hla10}),
            };

            var expectedGenotype = new List<PhenotypeInfo<string>>
            {
                new PhenotypeInfo<string>
                {
                    A = new LocusInfo<string>(Hla1, Hla1),
                    B = new LocusInfo<string>(Hla3, Hla4),
                    C = new LocusInfo<string>(Hla5, Hla6),
                    Dqb1 = new LocusInfo<string>(Hla7, Hla8),
                    Drb1 = new LocusInfo<string>(Hla9, Hla10)
                },
                new PhenotypeInfo<string>
                {
                    A = new LocusInfo<string>(Hla1, Hla1),
                    B = new LocusInfo<string>(Hla4, Hla4),
                    C = new LocusInfo<string>(Hla5, Hla6),
                    Dqb1 = new LocusInfo<string>(Hla7, Hla8),
                    Drb1 = new LocusInfo<string>(Hla9, Hla10)
                },
                new PhenotypeInfo<string>
                {
                    A = new LocusInfo<string>(Hla1, Hla2),
                    B = new LocusInfo<string>(Hla3, Hla4),
                    C = new LocusInfo<string>(Hla5, Hla6),
                    Dqb1 = new LocusInfo<string>(Hla7, Hla8),
                    Drb1 = new LocusInfo<string>(Hla9, Hla10)
                },
                new PhenotypeInfo<string>
                {
                    A = new LocusInfo<string>(Hla1, Hla2),
                    B = new LocusInfo<string>(Hla4, Hla4),
                    C = new LocusInfo<string>(Hla5, Hla6),
                    Dqb1 = new LocusInfo<string>(Hla7, Hla8),
                    Drb1 = new LocusInfo<string>(Hla9, Hla10)
                },
                new PhenotypeInfo<string>
                {
                    A = new LocusInfo<string>(Hla1, Hla3),
                    B = new LocusInfo<string>(Hla3, Hla4),
                    C = new LocusInfo<string>(Hla5, Hla6),
                    Dqb1 = new LocusInfo<string>(Hla7, Hla8),
                    Drb1 = new LocusInfo<string>(Hla9, Hla10)
                },
                new PhenotypeInfo<string>
                {
                    A = new LocusInfo<string>(Hla1, Hla3),
                    B = new LocusInfo<string>(Hla4, Hla4),
                    C = new LocusInfo<string>(Hla5, Hla6),
                    Dqb1 = new LocusInfo<string>(Hla7, Hla8),
                    Drb1 = new LocusInfo<string>(Hla9, Hla10)
                },
                new PhenotypeInfo<string>
                {
                    A = new LocusInfo<string>(Hla2, Hla1),
                    B = new LocusInfo<string>(Hla3, Hla4),
                    C = new LocusInfo<string>(Hla5, Hla6),
                    Dqb1 = new LocusInfo<string>(Hla7, Hla8),
                    Drb1 = new LocusInfo<string>(Hla9, Hla10)
                },
                new PhenotypeInfo<string>
                {
                    A = new LocusInfo<string>(Hla2, Hla1),
                    B = new LocusInfo<string>(Hla4, Hla4),
                    C = new LocusInfo<string>(Hla5, Hla6),
                    Dqb1 = new LocusInfo<string>(Hla7, Hla8),
                    Drb1 = new LocusInfo<string>(Hla9, Hla10)
                },
                new PhenotypeInfo<string>
                {
                    A = new LocusInfo<string>(Hla2, Hla2),
                    B = new LocusInfo<string>(Hla3, Hla4),
                    C = new LocusInfo<string>(Hla5, Hla6),
                    Dqb1 = new LocusInfo<string>(Hla7, Hla8),
                    Drb1 = new LocusInfo<string>(Hla9, Hla10)
                },
                new PhenotypeInfo<string>
                {
                    A = new LocusInfo<string>(Hla2, Hla2),
                    B = new LocusInfo<string>(Hla4, Hla4),
                    C = new LocusInfo<string>(Hla5, Hla6),
                    Dqb1 = new LocusInfo<string>(Hla7, Hla8),
                    Drb1 = new LocusInfo<string>(Hla9, Hla10)
                },
                new PhenotypeInfo<string>
                {
                    A = new LocusInfo<string>(Hla2, Hla3),
                    B = new LocusInfo<string>(Hla3, Hla4),
                    C = new LocusInfo<string>(Hla5, Hla6),
                    Dqb1 = new LocusInfo<string>(Hla7, Hla8),
                    Drb1 = new LocusInfo<string>(Hla9, Hla10)
                },
                new PhenotypeInfo<string>
                {
                    A = new LocusInfo<string>(Hla2, Hla3),
                    B = new LocusInfo<string>(Hla4, Hla4),
                    C = new LocusInfo<string>(Hla5, Hla6),
                    Dqb1 = new LocusInfo<string>(Hla7, Hla8),
                    Drb1 = new LocusInfo<string>(Hla9, Hla10)
                },
                new PhenotypeInfo<string>
                {
                    A = new LocusInfo<string>(Hla3, Hla1),
                    B = new LocusInfo<string>(Hla3, Hla4),
                    C = new LocusInfo<string>(Hla5, Hla6),
                    Dqb1 = new LocusInfo<string>(Hla7, Hla8),
                    Drb1 = new LocusInfo<string>(Hla9, Hla10)
                },
                new PhenotypeInfo<string>
                {
                    A = new LocusInfo<string>(Hla3, Hla1),
                    B = new LocusInfo<string>(Hla4, Hla4),
                    C = new LocusInfo<string>(Hla5, Hla6),
                    Dqb1 = new LocusInfo<string>(Hla7, Hla8),
                    Drb1 = new LocusInfo<string>(Hla9, Hla10)
                },
                new PhenotypeInfo<string>
                {
                    A = new LocusInfo<string>(Hla3, Hla2),
                    B = new LocusInfo<string>(Hla3, Hla4),
                    C = new LocusInfo<string>(Hla5, Hla6),
                    Dqb1 = new LocusInfo<string>(Hla7, Hla8),
                    Drb1 = new LocusInfo<string>(Hla9, Hla10)
                },
                new PhenotypeInfo<string>
                {
                    A = new LocusInfo<string>(Hla3, Hla2),
                    B = new LocusInfo<string>(Hla4, Hla4),
                    C = new LocusInfo<string>(Hla5, Hla6),
                    Dqb1 = new LocusInfo<string>(Hla7, Hla8),
                    Drb1 = new LocusInfo<string>(Hla9, Hla10)
                },
                new PhenotypeInfo<string>
                {
                    A = new LocusInfo<string>(Hla3, Hla3),
                    B = new LocusInfo<string>(Hla3, Hla4),
                    C = new LocusInfo<string>(Hla5, Hla6),
                    Dqb1 = new LocusInfo<string>(Hla7, Hla8),
                    Drb1 = new LocusInfo<string>(Hla9, Hla10)
                },
                new PhenotypeInfo<string>
                {
                    A = new LocusInfo<string>(Hla3, Hla3),
                    B = new LocusInfo<string>(Hla4, Hla4),
                    C = new LocusInfo<string>(Hla5, Hla6),
                    Dqb1 = new LocusInfo<string>(Hla7, Hla8),
                    Drb1 = new LocusInfo<string>(Hla9, Hla10)
                }
            };

            var actualGenotypes = ambiguousPhenotypeExpander.ExpandPhenotype(allelesPerLocus).ToList();

            actualGenotypes.Should().BeEquivalentTo(expectedGenotype);
        }

        // Ran in ~15.9 sec
        [TestCase(1, 1)]
        [TestCase(2, 1024)]
        [TestCase(3, 59049)]
        [TestCase(4, 1048576)]
        [TestCase(5, 9765625)]
        public void ExpandPhenotype_WithMultipleAllelePerLocus_ReturnsExpectedNumberOfGenotypes(int numberOfAllelesPerLocus, int expectedNumberOfGenotypes)
        {
            var alleles = new List<string>();

            for (var i = 0; i < numberOfAllelesPerLocus; i++)
            {
                alleles.Add(Hla1);
            }

            var allelesPerLocus = new PhenotypeInfo<List<string>>
            {
                A = new LocusInfo<List<string>>(alleles, alleles),
                B = new LocusInfo<List<string>>(alleles, alleles),
                C = new LocusInfo<List<string>>(alleles, alleles),
                Dqb1 = new LocusInfo<List<string>>(alleles, alleles),
                Drb1 = new LocusInfo<List<string>>(alleles, alleles)
            };

            var actualGenotypes = ambiguousPhenotypeExpander.ExpandPhenotype(allelesPerLocus).ToList();

            actualGenotypes.Count.Should().Be(expectedNumberOfGenotypes);
        }
    }
}