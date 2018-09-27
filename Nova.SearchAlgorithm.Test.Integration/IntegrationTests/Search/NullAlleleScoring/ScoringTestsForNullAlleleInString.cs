using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Test.Integration.TestData;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests.Search.NullAlleleScoring
{
    /// <summary>
    /// Confirm that scoring on allele strings with a null allele is as expected 
    /// when run as part of the larger search algorithm service.
    /// This fixture focuses on one locus with an allele string typing at one position.
    /// </summary>
    public class ScoringTestsForNullAlleleInString : IntegrationTestBase
    {
        private const Locus LocusUnderTest = Locus.A;
        private const TypePositions PositionUnderTest = TypePositions.One;
        private const TypePositions OtherPosition = TypePositions.Two;

        // Matching & scoring assertions are based on the following assumptions:
        // In v.3.3.0 of HLA db, the null allele below is the only null member of the group of alleles beginning with the same first two fields.
        // Therefore, the two- and three-field truncated name variants - WITH suffix - should only map this null allele.
        // The truncated name variants that have NO suffix should return the relevant expressing alleles, as well as the null allele.
        private const string ExpressingAlleleFromSameGGroupAsNullAllele = "01:01:01:01";
        private const string NullAllele = "01:01:01:02N";
        private const string NullAlleleAsTwoFieldNameNoSuffix = "01:01";
        private const string NullAlleleAsTwoFieldNameWithSuffix = "01:01N";
        private const string NullAlleleAsThreeFieldNameNoSuffix = "01:01:01";
        private const string NullAlleleAsThreeFieldNameWithSuffix = "01:01:01N";
        private const string NullAlleleAsStringWithExpressingAlleleOfSameGGroup = 
            NullAllele + "/" + ExpressingAlleleFromSameGGroupAsNullAllele;
        private const string NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup = NullAllele + "/01:09:01:01";
        private const string NullAlleleAsXxCode = "01:XX";
        private const string DifferentNullAllele = "03:01:01:02N";

        private readonly List<MatchGrade> matchGradesForExpressingAlleleOfSameGGroups = new List<MatchGrade>
        {
            MatchGrade.PGroup,
            MatchGrade.GGroup,
            MatchGrade.Protein,
            MatchGrade.CDna,
            MatchGrade.GDna
        };

        private readonly List<MatchGrade> matchGradesForMatchingNullAlleles = new List<MatchGrade>
        {
            MatchGrade.NullGDna,
            MatchGrade.NullCDna,
            MatchGrade.NullPartial
        };
        
        private PhenotypeInfo<string> originalHlaPhenotype;
        private PhenotypeInfo<string> patientWithNullAlleleAsTwoFieldNameNoSuffix;
        private PhenotypeInfo<string> patientWithNullAlleleAsTwoFieldNameWithSuffix;
        private PhenotypeInfo<string> patientWithNullAlleleAsThreeFieldNameNoSuffix;
        private PhenotypeInfo<string> patientWithNullAlleleAsThreeFieldNameWithSuffix;
        private PhenotypeInfo<string> patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup;
        private PhenotypeInfo<string> patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup;
        private PhenotypeInfo<string> patientWithNullAlleleAsXxCode;

        private IExpandHlaPhenotypeService expandHlaPhenotypeService;
        private IDonorImportRepository donorRepository;
        private ISearchService searchService;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            expandHlaPhenotypeService = Container.Resolve<IExpandHlaPhenotypeService>();
            donorRepository = Container.Resolve<IDonorImportRepository>();
            originalHlaPhenotype = new TestHla.HeterozygousSet1().FiveLocus_SingleExpressingAlleles;
            SetPatientPhenotypes();
        }

        [SetUp]
        public void ResolveSearchService()
        {
            searchService = Container.Resolve<ISearchService>();
        }

        #region Two-Field Name, No Expression Letter

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsOneCopyOfExpressingAllele_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);
            
            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsTwoCopiesOfExpressingAllele_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsNullAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsDifferentNullAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(DifferentNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsItself_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }
        
        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsNullAlleleAsTwoFieldNameWithSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsNullAlleleAsThreeFieldNameNoSuffix_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsNullAlleleAsThreeFieldNameWithSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfSameGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsNullAlleleAsXxCode_ThenExpressingMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsXxCode);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        #endregion

        #region Two-Field Name, With Expression Letter

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsOneCopyOfExpressingAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FourOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsTwoCopiesOfExpressingAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .FourOutOfSix()
                .WithDoubleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsNullAllele_ThenNullMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsDifferentNullAllele_ThenNullMismatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(DifferentNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.NullMismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsItself_ThenNullMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsNullAlleleAsTwoFieldNameNoSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsNullAlleleAsThreeFieldNameNoSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsNullAlleleAsThreeFieldNameWithSuffix_ThenNullMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfSameGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsNullAlleleAsXxCode_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsXxCode);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        #endregion

        #region Three-Field Name, No Expression Letter

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsOneCopyOfExpressingAllele_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsTwoCopiesOfExpressingAllele_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsNullAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsDifferentNullAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(DifferentNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsItself_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsNullAlleleAsTwoFieldNameWithSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsNullAlleleAsTwoFieldNameNoSuffix_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsNullAlleleAsThreeFieldNameWithSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfSameGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsNullAlleleAsXxCode_ThenExpressingMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsXxCode);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        #endregion

        #region Three-Field Name, With Expression Letter

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsOneCopyOfExpressingAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FourOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsTwoCopiesOfExpressingAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .FourOutOfSix()
                .WithDoubleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsNullAllele_ThenNullMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsDifferentNullAllele_ThenNullMismatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(DifferentNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.NullMismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsItself_ThenNullMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsNullAlleleAsTwoFieldNameNoSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsNullAlleleAsThreeFieldNameNoSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsNullAlleleAsTwoFieldNameWithSuffix_ThenNullMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfSameGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsNullAlleleAsXxCode_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsXxCode);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        #endregion

        #region Allele String, With Expressing Allele From Same G Group

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsOneCopyOfExpressingAllele_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsTwoCopiesOfExpressingAllele_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsNullAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsDifferentNullAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(DifferentNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsItself_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfSameGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsNullAlleleAsTwoFieldNameWithSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsNullAlleleAsTwoFieldNameNoSuffix_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsNullAlleleAsThreeFieldNameWithSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsNullAlleleAsThreeFieldNameNoSuffix_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsNullAlleleAsXxCode_ThenExpressingMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsXxCode);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        #endregion

        #region Allele String, With Expressing Allele From Different G Group

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsOneCopyOfExpressingAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FourOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsTwoCopiesOfExpressingAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .FourOutOfSix()
                .WithDoubleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsNullAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsDifferentNullAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(DifferentNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsItself_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsNullAlleleAsTwoFieldNameNoSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsNullAlleleAsThreeFieldNameNoSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsNullAlleleAsTwoFieldNameWithSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsNullAlleleAsThreeFieldNameWithSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfSameGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsNullAlleleAsXxCode_ThenExpressingMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsXxCode);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        #endregion

        #region XX Code

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsXxCodeVsOneCopyOfExpressingAllele_ThenGGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsXxCode)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsXxCodeVsTwoCopiesOfExpressingAllele_ThenGGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsXxCode)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsXxCodeVsNullAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsXxCode)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsXxCodeVsDifferentNullAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(DifferentNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsXxCode)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsXxCodeVsItself_ThenGGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsXxCode);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsXxCode)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsXxCodeVsNullAlleleAsTwoFieldNameNoSuffix_ThenGGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsXxCode)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsXxCodeVsNullAlleleAsTwoFieldNameWithSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsXxCode)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsXxCodeVsNullAlleleAsThreeFieldNameNoSuffix_ThenGGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsXxCode)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsXxCodeVsNullAlleleAsThreeFieldNameWithSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsXxCode)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsXxCodeVsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_ThenGGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfSameGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsXxCode)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsXxCodeVsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_ThenGGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsXxCode)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        #endregion

        #region Helper Methods
        private void SetPatientPhenotypes()
        {
            patientWithNullAlleleAsTwoFieldNameNoSuffix = 
                BuildPhenotype(NullAlleleAsTwoFieldNameNoSuffix);

            patientWithNullAlleleAsTwoFieldNameWithSuffix =
                BuildPhenotype(NullAlleleAsTwoFieldNameWithSuffix);

            patientWithNullAlleleAsThreeFieldNameNoSuffix =
                BuildPhenotype(NullAlleleAsThreeFieldNameNoSuffix);

            patientWithNullAlleleAsThreeFieldNameWithSuffix =
                BuildPhenotype(NullAlleleAsThreeFieldNameWithSuffix);

            patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup =
                BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfSameGGroup);

            patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup =
                BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);

            patientWithNullAlleleAsXxCode =
                BuildPhenotype(NullAlleleAsXxCode);
        }

        private PhenotypeInfo<string> BuildPhenotype(string hlaForPositionUnderTest)
        {
            var newPhenotype = originalHlaPhenotype.Map((l, p, hla) => hla);
            newPhenotype.SetAtPosition(LocusUnderTest, PositionUnderTest, hlaForPositionUnderTest);

            return newPhenotype;
        }

        private async Task<int> AddDonorPhenotypeToDonorRepository(PhenotypeInfo<string> donorPhenotype)
        {
            var matchingHlaPhenotype = expandHlaPhenotypeService
                .GetPhenotypeOfExpandedHla(donorPhenotype)
                .Result;

            var testDonor = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithMatchingHla(matchingHlaPhenotype)
                .Build();

            await donorRepository.AddOrUpdateDonorWithHla(testDonor);

            return testDonor.DonorId;
        }

        #endregion
    }
}
