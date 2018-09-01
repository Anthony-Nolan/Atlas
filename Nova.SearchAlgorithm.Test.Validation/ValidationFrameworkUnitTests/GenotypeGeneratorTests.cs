using System.Collections.Generic;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Builders;
using Nova.SearchAlgorithm.Test.Validation.TestData.Exceptions;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationFrameworkUnitTests
{
    [TestFixture]
    public class GenotypeGeneratorTests
    {
        private GenotypeGenerator genotypeGenerator;
        private IAlleleRepository alleleRepository;

        // Alleles of more than one first/second field are necessary here, so that an allele strings can be generated
        // Neither the allele splitter nor TgsAllele class are mocked, making this a necessity
        private static readonly List<AlleleTestData> Alleles1 = new List<AlleleTestData>
        {
            new AlleleTestData {AlleleName = "01:01:01:test-allele-pos-1"},
            new AlleleTestData {AlleleName = "01:02:01:test-allele-pos-1"},
            new AlleleTestData {AlleleName = "02:01:01:test-allele-pos-1"},
            new AlleleTestData {AlleleName = "02:02:01:test-allele-pos-1"},
        };

        private static readonly List<AlleleTestData> Alleles2 = new List<AlleleTestData>
        {
            new AlleleTestData {AlleleName = "01:01:01:test-allele-pos-2"},
            new AlleleTestData {AlleleName = "01:02:01:test-allele-pos-2"},
            new AlleleTestData {AlleleName = "02:01:01:test-allele-pos-2"},
            new AlleleTestData {AlleleName = "02:02:01:test-allele-pos-2"},
        };

        private static readonly LocusInfo<List<AlleleTestData>> lociAlleles = new LocusInfo<List<AlleleTestData>>(Alleles1);

        private static PhenotypeInfo<List<AlleleTestData>> AllelesPhenotype
        {
            get { return new PhenotypeInfo<bool>().Map((l, p, noop) => p == TypePositions.One ? Alleles1 : Alleles2); }
        }

        [SetUp]
        public void SetUp()
        {
            alleleRepository = Substitute.For<IAlleleRepository>();

            alleleRepository.FourFieldAlleles().Returns(AllelesPhenotype);
            alleleRepository.ThreeFieldAlleles().Returns(AllelesPhenotype);
            alleleRepository.TwoFieldAlleles().Returns(AllelesPhenotype);
            alleleRepository.AllelesWithAlleleStringOfSubtypesPossible().Returns(AllelesPhenotype);

            genotypeGenerator = new GenotypeGenerator(alleleRepository);
        }

        [Test]
        public void GenerateGenotype_WhenNoAllelesExistInDataset_ThrowsException()
        {
            alleleRepository.FourFieldAlleles().Returns(new PhenotypeInfo<List<AlleleTestData>>());
            alleleRepository.ThreeFieldAlleles().Returns(new PhenotypeInfo<List<AlleleTestData>>());
            alleleRepository.TwoFieldAlleles().Returns(new PhenotypeInfo<List<AlleleTestData>>());
            
            Assert.Throws<InvalidTestDataException>(() => genotypeGenerator.GenerateGenotype(null));
        }

        [Test]
        public void GenerateGenotype_WhenNoCriteriaProvided_UsesAllelesFromTgsDataset()
        {
            genotypeGenerator.GenerateGenotype(null);

            alleleRepository.Received().FourFieldAlleles();
            alleleRepository.Received().ThreeFieldAlleles();
            alleleRepository.Received().TwoFieldAlleles();
        }

        [Test]
        public void GenerateGenotype_WhenLocusShouldBeHomozygous_SetsSameAlleleAtEachPosition()
        {
            const Locus locus = Locus.A;
            var criteria = new GenotypeCriteriaBuilder().HomozygousAtLocus(locus).Build();
            var genotype = genotypeGenerator.GenerateGenotype(criteria);

            genotype.Hla.DataAtPosition(locus, TypePositions.One).Should().Be(genotype.Hla.DataAtPosition(locus, TypePositions.Two));
        }

        [Test]
        public void GenerateGenotype_WhenLocusShouldNotBeHomozygous_SetsDifferentAlleleAtEachPosition()
        {
            const Locus locus = Locus.A;
            var criteria = new GenotypeCriteriaBuilder().Build();
            var genotype = genotypeGenerator.GenerateGenotype(criteria);

            genotype.Hla.DataAtPosition(locus, TypePositions.One).Should().NotBe(genotype.Hla.DataAtPosition(locus, TypePositions.Two));
        }

        [Test]
        public void GenerateGenotype_WhenAlleleStringOfSubtypesNotPossible_DoesNotSetAlleleStringOfSubtypes()
        {
            var criteria = new GenotypeCriteriaBuilder().Build();
            var genotype = genotypeGenerator.GenerateGenotype(criteria);

            genotype.Hla.A_1.GetHlaForResolution(HlaTypingResolution.AlleleStringOfSubtypes).Should().BeNullOrEmpty();
        }
        
        [Test]
        public void GenerateGenotype_WhenAlleleStringOfSubtypesPossible_SetsAlleleStringOfSubtypes()
        {
            var criteria = new GenotypeCriteriaBuilder().WithAlleleStringOfSubtypesPossibleAtAllLoci().Build();
            var genotype = genotypeGenerator.GenerateGenotype(criteria);

            genotype.Hla.A_1.GetHlaForResolution(HlaTypingResolution.AlleleStringOfSubtypes).Should().NotBeNullOrEmpty();
        }
        
        [Test]
        public void GenerateGenotype_WhenAlleleStringOfSubtypesPossible_ButDoNotExistInTestData_ThrowsException()
        {
            alleleRepository.AllelesWithAlleleStringOfSubtypesPossible()
                .Returns(new PhenotypeInfo<List<AlleleTestData>>(new List<AlleleTestData>
                {
                    new AlleleTestData{AlleleName = "01:01:01:01"},
                    new AlleleTestData{AlleleName = "02:01:01:01"},
                }));
            var criteria = new GenotypeCriteriaBuilder().WithAlleleStringOfSubtypesPossibleAtAllLoci().Build();

            Assert.Throws<InvalidTestDataException>(() => genotypeGenerator.GenerateGenotype(criteria));
        }
    }
}