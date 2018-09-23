using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Test.Integration.TestData;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests.Search
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
        // In v.3.3.0 of HLA db, the null allele below is the only null member of the group 
        // of alleles beginning with the same first two fields.
        // Therefore, the two- and three-field truncated name variants - WITH suffix -
        // should only map this null allele.
        // The truncated name variants that have NO suffix should return the relevant
        // expressing alleles, as well as the null allele.
        private const string ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele = "01:01:01:01";
        private const string NullAllele = "01:01:01:02N";
        private const string NullAlleleAsTwoFieldNameNoSuffix = "01:01";
        private const string NullAlleleAsTwoFieldNameWithSuffix = "01:01N";
        private const string NullAlleleAsThreeFieldNameNoSuffix = "01:01:01";
        private const string NullAlleleAsThreeFieldNameWithSuffix = "01:01:01N";
        private const string NullAlleleAsStringWithMatchingExpressingAllele = 
            NullAllele + "/" + ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele;
        private const string NullAlleleAsStringWithNonMatchingExpressingAllele = NullAllele + "/01:09:01:01";
        private const string NullAlleleAsXxCode = "01:XX";
        private const string DifferentNullAllele = "03:01:01:02N";

        private List<MatchGrade> matchGradesForMatchingExpressingAlleles;
        private List<MatchGrade> matchGradesForMatchingNullAlleles;
        private PhenotypeInfo<string> originalHlaPhenotype;
        private PhenotypeInfo<string> patientWithNullAlleleAsTwoFieldNameNoSuffix;
        private PhenotypeInfo<string> patientWithNullAlleleAsTwoFieldNameWithSuffix;
        private PhenotypeInfo<string> patientWithNullAlleleAsThreeFieldNameNoSuffix;
        private PhenotypeInfo<string> patientWithNullAlleleAsThreeFieldNameWithSuffix;
        private PhenotypeInfo<string> patientWithNullAlleleAsStringWithMatchingExpressingAllele;
        private PhenotypeInfo<string> patientWithNullAlleleAsStringWithNonMatchingExpressingAllele;

        private IExpandHlaPhenotypeService expandHlaPhenotypeService;
        private IDonorImportRepository donorRepository;
        private ISearchService searchService;

        [OneTimeSetUp]
        public void ImportTestDonor()
        {
            expandHlaPhenotypeService = Container.Resolve<IExpandHlaPhenotypeService>();
            donorRepository = Container.Resolve<IDonorImportRepository>();

            matchGradesForMatchingExpressingAlleles = new List<MatchGrade>
            {
                MatchGrade.PGroup,
                MatchGrade.GGroup,
                MatchGrade.Protein,
                MatchGrade.CDna,
                MatchGrade.GDna
            };

            matchGradesForMatchingNullAlleles = new List<MatchGrade>
            {
                MatchGrade.NullGDna,
                MatchGrade.NullCDna,
                MatchGrade.NullPartial
            };

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
            var donorPhenotype = BuildPhenotype(ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);
            
            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingExpressingAlleles.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsTwoCopiesOfExpressingAllele_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingExpressingAlleles.Should()
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
            matchGradesForMatchingExpressingAlleles.Should()
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
            matchGradesForMatchingExpressingAlleles.Should()
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
        public async Task Search_SixOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsNullAlleleAsStringWithMatchingExpressingAllele_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithMatchingExpressingAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingExpressingAlleles.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsNullAlleleAsStringWithNonMatchingExpressingAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithNonMatchingExpressingAllele);
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
            matchGradesForMatchingExpressingAlleles.Should()
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
            var donorPhenotype = BuildPhenotype(ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
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
            var donorPhenotype = BuildPhenotype(ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
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
        public async Task Search_FiveOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsNullAlleleAsStringWithMatchingExpressingAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithMatchingExpressingAllele);
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
        public async Task Search_FiveOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsNullAlleleAsStringWithNonMatchingExpressingAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithNonMatchingExpressingAllele);
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
            var donorPhenotype = BuildPhenotype(ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingExpressingAlleles.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsTwoCopiesOfExpressingAllele_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingExpressingAlleles.Should()
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
            matchGradesForMatchingExpressingAlleles.Should()
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
            matchGradesForMatchingExpressingAlleles.Should()
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
        public async Task Search_SixOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsNullAlleleAsStringWithMatchingExpressingAllele_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithMatchingExpressingAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingExpressingAlleles.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsNullAlleleAsStringWithNonMatchingExpressingAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithNonMatchingExpressingAllele);
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
            matchGradesForMatchingExpressingAlleles.Should()
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
            var donorPhenotype = BuildPhenotype(ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
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
            var donorPhenotype = BuildPhenotype(ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
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
        public async Task Search_FiveOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsNullAlleleAsStringWithMatchingExpressingAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithMatchingExpressingAllele);
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
        public async Task Search_FiveOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsNullAlleleAsStringWithNonMatchingExpressingAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithNonMatchingExpressingAllele);
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

        #region Allele String, With Matching Expressing Allele

        [Test]
        public async Task Search_SixOutOfSix_NullAlleleAsStringWithMatchingExpressingAlleleVsOneCopyOfExpressingAllele_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithMatchingExpressingAllele)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingExpressingAlleles.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithMatchingExpressingAlleleVsTwoCopiesOfExpressingAllele_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithMatchingExpressingAllele)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingExpressingAlleles.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithMatchingExpressingAlleleVsNullAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithMatchingExpressingAllele)
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
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithMatchingExpressingAlleleVsDifferentNullAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(DifferentNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithMatchingExpressingAllele)
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
        public async Task Search_SixOutOfSix_NullAlleleAsStringWithMatchingExpressingAlleleVsItself_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithMatchingExpressingAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithMatchingExpressingAllele)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingExpressingAlleles.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithMatchingExpressingAlleleVsNullAlleleAsTwoFieldNameWithSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithMatchingExpressingAllele)
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
        public async Task Search_SixOutOfSix_NullAlleleAsStringWithMatchingExpressingAlleleVsNullAlleleAsTwoFieldNameNoSuffix_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithMatchingExpressingAllele)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingExpressingAlleles.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithMatchingExpressingAlleleVsNullAlleleAsThreeFieldNameWithSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithMatchingExpressingAllele)
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
        public async Task Search_SixOutOfSix_NullAlleleAsStringWithMatchingExpressingAlleleVsNullAlleleAsThreeFieldNameNoSuffix_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithMatchingExpressingAllele)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingExpressingAlleles.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithMatchingExpressingAlleleVsNullAlleleAsStringWithNonMatchingExpressingAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithNonMatchingExpressingAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithMatchingExpressingAllele)
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
        public async Task Search_SixOutOfSix_NullAlleleAsStringWithMatchingExpressingAlleleVsNullAlleleAsXxCode_ThenExpressingMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsXxCode);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithMatchingExpressingAllele)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingExpressingAlleles.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        #endregion

        #region Allele String, With Non-Matching Expressing Allele

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithNonMatchingExpressingAlleleVsOneCopyOfExpressingAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithNonMatchingExpressingAllele)
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
        public async Task Search_FourOutOfSix_NullAlleleAsStringWithNonMatchingExpressingAlleleVsTwoCopiesOfExpressingAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithNonMatchingExpressingAllele)
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
        public async Task Search_SixOutOfSix_NullAlleleAsStringWithNonMatchingExpressingAlleleVsNullAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithNonMatchingExpressingAllele)
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
        public async Task Search_SixOutOfSix_NullAlleleAsStringWithNonMatchingExpressingAlleleVsDifferentNullAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(DifferentNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithNonMatchingExpressingAllele)
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
        public async Task Search_SixOutOfSix_NullAlleleAsStringWithNonMatchingExpressingAlleleVsItself_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithNonMatchingExpressingAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithNonMatchingExpressingAllele)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingExpressingAlleles.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithNonMatchingExpressingAlleleVsNullAlleleAsTwoFieldNameNoSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithNonMatchingExpressingAllele)
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
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithNonMatchingExpressingAlleleVsNullAlleleAsThreeFieldNameNoSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithNonMatchingExpressingAllele)
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
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithNonMatchingExpressingAlleleVsNullAlleleAsTwoFieldNameWithSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithNonMatchingExpressingAllele)
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
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithNonMatchingExpressingAlleleVsNullAlleleAsThreeFieldNameWithSuffix_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithNonMatchingExpressingAllele)
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
        public async Task Search_FiveOutOfSix_NullAlleleAsStringWithNonMatchingExpressingAlleleVsNullAlleleAsStringWithMatchingExpressingAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithMatchingExpressingAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithNonMatchingExpressingAllele)
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
        public async Task Search_SixOutOfSix_NullAlleleAsStringWithNonMatchingExpressingAlleleVsNullAlleleAsXxCode_ThenExpressingMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsXxCode);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsStringWithNonMatchingExpressingAllele)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingExpressingAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
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

            patientWithNullAlleleAsStringWithMatchingExpressingAllele =
                BuildPhenotype(NullAlleleAsStringWithMatchingExpressingAllele);

            patientWithNullAlleleAsStringWithNonMatchingExpressingAllele =
                BuildPhenotype(NullAlleleAsStringWithNonMatchingExpressingAllele);
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
