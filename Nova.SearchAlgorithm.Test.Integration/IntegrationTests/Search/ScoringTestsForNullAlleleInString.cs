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
        private const string NullAllele = "01:01:01:02N";
        private const string NullAlleleAsTwoFieldNameNoExpressionLetter = "01:01";
        private const string NullAlleleAsTwoFieldAlleleWithExpressionLetter = "01:01N";
        private const string NullAlleleAsThreeFieldNameNoExpressionLetter = "01:01:01";
        private const string NullAlleleAsThreeFieldAlleleWithExpressionLetter = "01:01:01N";
        private const string NullAlleleAsAlleleString = NullAllele + "/01:02";
        private const string NullAlleleAsXxCode = "01:XX";
        private const string ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele = "01:01:01:01";
        private const string DifferentNullAllele = "03:01:01:02N";
        private const string DifferentNullAlleleAsTwoFieldNameNoExpressionLetter = "03:01";
        private const string DifferentNullAlleleAsTwoFieldAlleleWithExpressionLetter = "03:01N";
        private const string DifferentNullAlleleAsThreeFieldNameNoExpressionLetter = "03:01:01";
        private const string DifferentNullAlleleAsThreeFieldAlleleWithExpressionLetter = "03:01:01N";
        private const string DifferentNullAlleleAsAlleleString = DifferentNullAllele + "/03:08";
        private const string DifferentNullAlleleAsXxCode = "03:XX";

        private List<MatchGrade> matchGradesForMatchingExpressingAlleles;
        private List<MatchGrade> matchGradesForNullAlleles;
        private PhenotypeInfo<string> originalHlaPhenotype;
        private PhenotypeInfo<string> patientWithNullAlleleAsTwoFieldNameNoExpressionLetter;

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

            matchGradesForNullAlleles = new List<MatchGrade>
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
        public async Task Search_SixOutOfSix_NullAlleleAsTwoFieldNameNoExpressionLetterVsMatchingExpressingAllele_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);
            
            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameNoExpressionLetter)
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
        public async Task Search_FiveOutOfSix_NullAlleleAsTwoFieldNameNoExpressionLetterVsSameNullAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameNoExpressionLetter)
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
        public async Task Search_FiveOutOfSix_NullAlleleAsTwoFieldNameNoExpressionLetterVsDifferentNullAllele_ThenMismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(DifferentNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameNoExpressionLetter)
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
        public async Task Search_FiveOutOfSix_NullAlleleAsTwoFieldNameNoExpressionLetterVsHomozygousExpressing_ThenExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleWithSameFirstThreeFieldsAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = SearchRequestFromHlasBuilder
                .WithoutNonMatchingHlas(patientWithNullAlleleAsTwoFieldNameNoExpressionLetter)
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

        #endregion

        private void SetPatientPhenotypes()
        {
            patientWithNullAlleleAsTwoFieldNameNoExpressionLetter = 
                BuildPhenotype(NullAlleleAsTwoFieldNameNoExpressionLetter);
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
    }
}
