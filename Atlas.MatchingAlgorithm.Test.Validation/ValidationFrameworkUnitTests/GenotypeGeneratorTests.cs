using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Builders.Criteria;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Exceptions;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Repositories;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationFrameworkUnitTests
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
            new AlleleTestData {AlleleName = "01:01:01:test-allele-pos-1", PGroup = "01:01P"},
            new AlleleTestData {AlleleName = "01:02:01:test-allele-pos-1", PGroup = "01:01P"},
            new AlleleTestData {AlleleName = "02:01:01:test-allele-pos-1", PGroup = "02:02P"},
            new AlleleTestData {AlleleName = "02:02:01:test-allele-pos-1", PGroup = "02:02P"},
        };

        private static readonly List<AlleleTestData> Alleles2 = new List<AlleleTestData>
        {
            new AlleleTestData {AlleleName = "01:01:01:test-allele-pos-2"},
            new AlleleTestData {AlleleName = "01:02:01:test-allele-pos-2"},
            new AlleleTestData {AlleleName = "02:01:01:test-allele-pos-2"},
            new AlleleTestData {AlleleName = "02:02:01:test-allele-pos-2"},
        };

        private static readonly LociInfo<List<AlleleTestData>> LociAlleles = new LociInfo<List<AlleleTestData>>(Alleles1);

        private static PhenotypeInfo<List<AlleleTestData>> AllelesPhenotype
        {
            get { return new PhenotypeInfo<bool>().Map((l, p, noop) => p == LocusPosition.One ? Alleles1 : Alleles2); }
        }

        [SetUp]
        public void SetUp()
        {
            alleleRepository = Substitute.For<IAlleleRepository>();

            alleleRepository.FourFieldAlleles().Returns(AllelesPhenotype);
            alleleRepository.ThreeFieldAlleles().Returns(AllelesPhenotype);
            alleleRepository.TwoFieldAlleles().Returns(AllelesPhenotype);
            
            alleleRepository.AllelesWithAlleleStringOfSubtypesPossible().Returns(AllelesPhenotype);
            alleleRepository.AllelesWithTwoFieldMatchPossible().Returns(AllelesPhenotype);
            alleleRepository.DonorAllelesWithThreeFieldMatchPossible().Returns(AllelesPhenotype);
            
            alleleRepository.AllelesWithNonNullExpressionSuffix().Returns(AllelesPhenotype);
            alleleRepository.NullAlleles().Returns(AllelesPhenotype);
            
            alleleRepository.AllelesForCDnaMatching().Returns(LociAlleles);
            alleleRepository.AllelesForGGroupMatching().Returns(AllelesPhenotype);
            alleleRepository.AllelesForProteinMatching().Returns(AllelesPhenotype);
            alleleRepository.DonorAllelesForPGroupMatching().Returns(LociAlleles);

            alleleRepository.AllelesWithStringsOfSingleAndMultiplePGroupsPossible().Returns(AllelesPhenotype);

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

            genotype.Hla.GetPosition(locus, LocusPosition.One).Should().Be(genotype.Hla.GetPosition(locus, LocusPosition.Two));
        }

        [Test]
        public void GenerateGenotype_WhenLocusShouldNotBeHomozygous_SetsDifferentAlleleAtEachPosition()
        {
            const Locus locus = Locus.A;
            var criteria = new GenotypeCriteriaBuilder().Build();
            var genotype = genotypeGenerator.GenerateGenotype(criteria);

            genotype.Hla.GetPosition(locus, LocusPosition.One).Should().NotBe(genotype.Hla.GetPosition(locus, LocusPosition.Two));
        }

        [Test]
        public void GenerateGenotype_WhenAlleleStringOfSubtypesNotPossible_DoesNotSetAlleleStringOfSubtypes()
        {
            var criteria = new GenotypeCriteriaBuilder().Build();
            var genotype = genotypeGenerator.GenerateGenotype(criteria);

            genotype.Hla.A.Position1.GetHlaForResolution(HlaTypingResolution.AlleleStringOfSubtypes).Should().BeNullOrEmpty();
        }
        
        [Test]
        public void GenerateGenotype_WhenAlleleStringOfSubtypesPossible_SetsAlleleStringOfSubtypes()
        {
            var criteria = new GenotypeCriteriaBuilder().WithAlleleStringOfSubtypesPossibleAtAllLoci().Build();
            var genotype = genotypeGenerator.GenerateGenotype(criteria);

            genotype.Hla.A.Position1.GetHlaForResolution(HlaTypingResolution.AlleleStringOfSubtypes).Should().NotBeNullOrEmpty();
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
        
        [Test]
        public void GenerateGenotype_WhenDataDoesNotExistForAlleleStringOfNames_ThrowsException()
        {
            alleleRepository.AllelesWithAlleleStringOfSubtypesPossible()
                .Returns(new PhenotypeInfo<List<AlleleTestData>>(new List<AlleleTestData>
                {
                    new AlleleTestData{AlleleName = "01:01:01:01"},
                }));
            var criteria = new GenotypeCriteriaBuilder().WithAlleleStringOfSubtypesPossibleAtAllLoci().Build();

            Assert.Throws<InvalidTestDataException>(() => genotypeGenerator.GenerateGenotype(criteria));
        }
        
        [Test]
        public void GenerateGenotype_WhenFourFieldTgsAlleleRequested_UsesAllelesFromFourFieldDataSetOnly()
        {
            var criteria = new GenotypeCriteriaBuilder().WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.FourFieldAllele).Build();
            genotypeGenerator.GenerateGenotype(criteria);

            alleleRepository.Received().FourFieldAlleles();
            alleleRepository.DidNotReceive().ThreeFieldAlleles();
            alleleRepository.DidNotReceive().TwoFieldAlleles();
        }
        
        [Test]
        public void GenerateGenotype_WhenThreeFieldTgsAlleleRequested_UsesAllelesFromThreeFieldDataSetOnly()
        {
            var criteria = new GenotypeCriteriaBuilder().WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.ThreeFieldAllele).Build();
            genotypeGenerator.GenerateGenotype(criteria);

            alleleRepository.Received().ThreeFieldAlleles();
            alleleRepository.DidNotReceive().FourFieldAlleles();
            alleleRepository.DidNotReceive().TwoFieldAlleles();
        }
        
        [Test]
        public void GenerateGenotype_WhenTwoFieldTgsAlleleRequested_UsesAllelesFromTwoFieldDataSetOnly()
        {
            var criteria = new GenotypeCriteriaBuilder().WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.TwoFieldAllele).Build();
            genotypeGenerator.GenerateGenotype(criteria);

            alleleRepository.Received().TwoFieldAlleles();
            alleleRepository.DidNotReceive().ThreeFieldAlleles();
            alleleRepository.DidNotReceive().FourFieldAlleles();
        }
        
        [Test]
        public void GenerateGenotype_WhenPGroupMatchRequested_UsesAllelesFromPGroupDataset()
        {
            var criteria = new GenotypeCriteriaBuilder().WithMatchLevelPossibleAtAllLoci(MatchLevel.PGroup).Build();
            genotypeGenerator.GenerateGenotype(criteria);

            alleleRepository.Received().DonorAllelesForPGroupMatching();
        }
        
        [Test]
        public void GenerateGenotype_WhenGGroupMatchRequested_UsesAllelesFromGPGroupDataset()
        {
            var criteria = new GenotypeCriteriaBuilder().WithMatchLevelPossibleAtAllLoci(MatchLevel.GGroup).Build();
            genotypeGenerator.GenerateGenotype(criteria);

            alleleRepository.Received().AllelesForGGroupMatching();
        }
        
        [Test]
        public void GenerateGenotype_WhenThreeFieldMatchRequested_UsesAllelesFromThreeFieldMatchDataset()
        {
            var criteria = new GenotypeCriteriaBuilder().WithMatchLevelPossibleAtAllLoci(MatchLevel.FirstThreeFieldAllele).Build();
            genotypeGenerator.GenerateGenotype(criteria);

            alleleRepository.Received().DonorAllelesWithThreeFieldMatchPossible();
        }
        
        [Test]
        public void GenerateGenotype_WhenTwoFieldMatchRequested_UsesAllelesFromTwoFieldMatchDataset()
        {
            var criteria = new GenotypeCriteriaBuilder().WithMatchLevelPossibleAtAllLoci(MatchLevel.FirstTwoFieldAllele).Build();
            genotypeGenerator.GenerateGenotype(criteria);

            alleleRepository.Received().AllelesWithTwoFieldMatchPossible();
        }
        
        [Test]
        public void GenerateGenotype_WhenAlleleStringOfSubtypesPossible_UsesAllelesFromAlleleStringOfSubtypesDataset()
        {
            var criteria = new GenotypeCriteriaBuilder().WithAlleleStringOfSubtypesPossibleAtAllLoci().Build();
            genotypeGenerator.GenerateGenotype(criteria);

            alleleRepository.Received().AllelesWithAlleleStringOfSubtypesPossible();
        }
        
        [Test]
        public void GenerateGenotype_WhenNullAllelesRequested_UsesAllelesFromNullDataset()
        {
            var criteria = new GenotypeCriteriaBuilder().WithNullAlleleAtAllLoci().Build();
            genotypeGenerator.GenerateGenotype(criteria);

            alleleRepository.Received().NullAlleles();
        }
        
        [Test]
        public void GenerateGenotype_WhenNonNullExpressionSuffixAllelesRequested_UsesAllelesFromNonNullExpressionSuffixDataset()
        {
            var criteria = new GenotypeCriteriaBuilder().WithNonNullExpressionSuffixAtLocus(Locus.A).Build();
            genotypeGenerator.GenerateGenotype(criteria);

            alleleRepository.Received().AllelesWithNonNullExpressionSuffix();
        }
        
        [Test]
        public void GenerateGenotype_WhenCDnaMatchRequested_UsesAllelesFromCDnaDataset()
        {
            var criteria = new GenotypeCriteriaBuilder().WithMatchLevelPossibleAtAllLoci(MatchLevel.CDna).Build();
            genotypeGenerator.GenerateGenotype(criteria);

            alleleRepository.Received().AllelesForCDnaMatching();
        }
        
        [Test]
        public void GenerateGenotype_WhenProteinMatchRequested_UsesAllelesFromProteinMatchDataset()
        {
            var criteria = new GenotypeCriteriaBuilder().WithMatchLevelPossibleAtAllLoci(MatchLevel.Protein).Build();
            genotypeGenerator.GenerateGenotype(criteria);

            alleleRepository.Received().AllelesForProteinMatching();
        }

        [Test]
        public void GenerateGenotype_WhenAlleleStringOfNamesWithSinglePGroupPossible_SetsAlleleString()
        {
            var criteria = new GenotypeCriteriaBuilder().WithStringOfSingleAndMultiplePGroupsPossibleAtAllLoci().Build();
            var genotype = genotypeGenerator.GenerateGenotype(criteria);

            genotype.Hla.A.Position1.GetHlaForResolution(HlaTypingResolution.AlleleStringOfNamesWithSinglePGroup).Should().NotBeNullOrEmpty();
        }

        [Test]
        public void GenerateGenotype_WhenAlleleStringOfNamesWithSinglePGroupPossible_UsesAllelesWithStringsOfSingleAndMultiplePGroupsPossibleDataset()
        {
            var criteria = new GenotypeCriteriaBuilder().WithStringOfSingleAndMultiplePGroupsPossibleAtAllLoci().Build();
            genotypeGenerator.GenerateGenotype(criteria);

            alleleRepository.Received().AllelesWithStringsOfSingleAndMultiplePGroupsPossible();
        }

        [Test]
        public void GenerateGenotype_WhenAlleleStringOfNamesWithSinglePGroupNotPossible_DoesNotSetAlleleString()
        {
            var criteria = new GenotypeCriteriaBuilder().Build();
            var genotype = genotypeGenerator.GenerateGenotype(criteria);

            // No P groups listed in A.Position2 test data, so no allele string of single P group can be built at this position
            genotype.Hla.A.Position2.GetHlaForResolution(HlaTypingResolution.AlleleStringOfNamesWithSinglePGroup).Should().BeNullOrEmpty();
        }

        [Test]
        public void GenerateGenotype_WhenAlleleStringOfNamesWithMultiplePGroupPossible_SetsAlleleString()
        {
            var criteria = new GenotypeCriteriaBuilder().WithStringOfSingleAndMultiplePGroupsPossibleAtAllLoci().Build();
            var genotype = genotypeGenerator.GenerateGenotype(criteria);

            genotype.Hla.A.Position1.GetHlaForResolution(HlaTypingResolution.AlleleStringOfNamesWithMultiplePGroups).Should().NotBeNullOrEmpty();
        }

        [Test]
        public void GenerateGenotype_WhenAlleleStringOfNamesWithMultiplePGroupsPossible_UsesAllelesWithStringsOfMultipleAndMultiplePGroupsPossibleDataset()
        {
            var criteria = new GenotypeCriteriaBuilder().WithStringOfSingleAndMultiplePGroupsPossibleAtAllLoci().Build();
            genotypeGenerator.GenerateGenotype(criteria);

            alleleRepository.Received().AllelesWithStringsOfSingleAndMultiplePGroupsPossible();
        }
    }
}