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
        private const string NullAlleleAsStringWithExpressingAlleleOfSameGGroup = NullAllele + "/" + ExpressingAlleleFromSameGGroupAsNullAllele;
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
        public async Task SixOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsOneCopyOfExpressingAllele_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);
            
            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsTwoCopiesOfExpressingAllele_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsDifferentNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(DifferentNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsItself_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }
        
        [Test]
        public async Task FiveOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsNullAlleleAsTwoFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsNullAlleleAsThreeFieldNameNoSuffix_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsNullAlleleAsThreeFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfSameGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsTwoFieldNameNoSuffixVsNullAlleleAsXxCode_ExpressingMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsXxCode);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

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
        public async Task FiveOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsOneCopyOfExpressingAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FourOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsTwoCopiesOfExpressingAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .FourOutOfSix()
                .WithDoubleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsNullAllele_NullMatchGradeAndDefiniteConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsDifferentNullAllele_NullMismatchGradeAndDefiniteConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(DifferentNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.NullMismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsItself_NullMatchGradeAndDefiniteConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsNullAlleleAsTwoFieldNameNoSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsNullAlleleAsThreeFieldNameNoSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsNullAlleleAsThreeFieldNameWithSuffix_NullMatchGradeAndDefiniteConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfSameGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsTwoFieldNameWithSuffixVsNullAlleleAsXxCode_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsXxCode);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsTwoFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

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
        public async Task SixOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsOneCopyOfExpressingAllele_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsTwoCopiesOfExpressingAllele_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsDifferentNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(DifferentNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsItself_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsNullAlleleAsTwoFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsNullAlleleAsTwoFieldNameNoSuffix_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsNullAlleleAsThreeFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfSameGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsThreeFieldNameNoSuffixVsNullAlleleAsXxCode_ExpressingMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsXxCode);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameNoSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

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
        public async Task FiveOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsOneCopyOfExpressingAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FourOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsTwoCopiesOfExpressingAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .FourOutOfSix()
                .WithDoubleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsNullAllele_NullMatchGradeAndDefiniteConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsDifferentNullAllele_NullMismatchGradeAndDefiniteConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(DifferentNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.NullMismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsItself_NullMatchGradeAndDefiniteConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsNullAlleleAsTwoFieldNameNoSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsNullAlleleAsThreeFieldNameNoSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsNullAlleleAsTwoFieldNameWithSuffix_NullMatchGradeAndDefiniteConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfSameGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsThreeFieldNameWithSuffixVsNullAlleleAsXxCode_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsXxCode);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsThreeFieldNameWithSuffix)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

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
        public async Task SixOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsOneCopyOfExpressingAllele_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsTwoCopiesOfExpressingAllele_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsDifferentNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(DifferentNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsItself_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfSameGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsNullAlleleAsTwoFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsNullAlleleAsTwoFieldNameNoSuffix_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsNullAlleleAsThreeFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsNullAlleleAsThreeFieldNameNoSuffix_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsStringWithExpressingAlleleOfSameGGroupVsNullAlleleAsXxCode_ExpressingMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsXxCode);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

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
        public async Task FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsOneCopyOfExpressingAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FourOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsTwoCopiesOfExpressingAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .FourOutOfSix()
                .WithDoubleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsDifferentNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(DifferentNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsItself_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should()
                .Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsNullAlleleAsTwoFieldNameNoSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsNullAlleleAsThreeFieldNameNoSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsNullAlleleAsTwoFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsNullAlleleAsThreeFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfSameGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroupVsNullAlleleAsXxCode_ExpressingMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsXxCode);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

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
        public async Task SixOutOfSix_NullAlleleAsXxCodeVsOneCopyOfExpressingAllele_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsXxCode)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsXxCodeVsTwoCopiesOfExpressingAllele_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(ExpressingAlleleFromSameGGroupAsNullAllele);
            donorPhenotype.SetAtPosition(LocusUnderTest, OtherPosition, ExpressingAlleleFromSameGGroupAsNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsXxCode)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsXxCodeVsNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsXxCode)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsXxCodeVsDifferentNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(DifferentNullAllele);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsXxCode)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsXxCodeVsItself_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsXxCode);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsXxCode)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsXxCodeVsNullAlleleAsTwoFieldNameNoSuffix_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsXxCode)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsXxCodeVsNullAlleleAsTwoFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsTwoFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsXxCode)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsXxCodeVsNullAlleleAsThreeFieldNameNoSuffix_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameNoSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsXxCode)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task FiveOutOfSix_NullAlleleAsXxCodeVsNullAlleleAsThreeFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsThreeFieldNameWithSuffix);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsXxCode)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsXxCodeVsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfSameGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsXxCode)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Other position at same locus
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task SixOutOfSix_NullAlleleAsXxCodeVsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(patientWithNullAlleleAsXxCode)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

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
            patientWithNullAlleleAsTwoFieldNameNoSuffix = BuildPhenotype(NullAlleleAsTwoFieldNameNoSuffix);

            patientWithNullAlleleAsTwoFieldNameWithSuffix = BuildPhenotype(NullAlleleAsTwoFieldNameWithSuffix);

            patientWithNullAlleleAsThreeFieldNameNoSuffix = BuildPhenotype(NullAlleleAsThreeFieldNameNoSuffix);

            patientWithNullAlleleAsThreeFieldNameWithSuffix = BuildPhenotype(NullAlleleAsThreeFieldNameWithSuffix);

            patientWithNullAlleleAsStringWithExpressingAlleleOfSameGGroup = BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfSameGGroup);

            patientWithNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup =
                BuildPhenotype(NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);

            patientWithNullAlleleAsXxCode = BuildPhenotype(NullAlleleAsXxCode);
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
