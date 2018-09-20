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
    /// All logic for scoring is solely contained within the various scoring services, 
    /// which are covered by an extensive suite of unit tests.
    /// The purpose of these integration tests is to confirm that scoring behaves
    /// as expected when run as part of the larger search algorithm service,
    /// and that the results of scoring are consistent with those of matching.
    /// </summary>
    public class ScoringTests : IntegrationTestBase
    {
        private ISearchService searchService;
        private ITestHlaSet defaultHlaSet;
        private ITestHlaSet mismatchHlaSet;
        private InputDonor donor_FiveLocus_SingleAlleles;

        [OneTimeSetUp]
        public void ImportTestDonor()
        {
            // source of donor HLA phenotypes
            defaultHlaSet = new TestHla.HeterozygousSet1();
            mismatchHlaSet = new TestHla.HeterozygousSet2();

            donor_FiveLocus_SingleAlleles = BuildTestDonor();

            // add test donors to repository
            var donorRepository = Container.Resolve<IDonorImportRepository>();
            donorRepository.AddOrUpdateDonorWithHla(donor_FiveLocus_SingleAlleles).Wait();
        }

        [SetUp]
        public void ResolveSearchService()
        {
            searchService = Container.Resolve<ISearchService>();
        }

        #region Scoring of the different HLA typing categories

        [Test]
        public async Task Search_TenOutOfTen_PatientAndDonorHaveSingleAlleles_ReturnsMolecularGrades_AndAlleleLevelConfidences()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.FiveLocus_SingleExpressingAlleles,
                    mismatchHlaSet.FiveLocus_SingleExpressingAlleles)
                .TenOutOfTen()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donor_FiveLocus_SingleAlleles.DonorId);

            const int minPossibleGradeScore = 10 * (int) MatchGrade.PGroup;
            const int maxPossibleGradeScore = 10 * (int) MatchGrade.GDna;
            result.GradeScore.Should().BeInRange(minPossibleGradeScore, maxPossibleGradeScore);

            const int minPossibleConfidenceScore = 10 * (int)MatchConfidence.Exact;
            const int maxPossibleConfidenceScore = 10 * (int)MatchConfidence.Definite;
            result.ConfidenceScore.Should().BeInRange(minPossibleConfidenceScore, maxPossibleConfidenceScore);

            var expectedMatchConfidences = new List<MatchConfidence> { MatchConfidence.Definite, MatchConfidence.Exact };
            expectedMatchConfidences.Should().Contain(result.OverallMatchConfidence);
        }

        [Test]
        public async Task Search_TenOutOfTen_PatientHasMultipleAlleles_DonorHasSingleAlleles_ReturnsMolecularGrades_AndAlleleLevelConfidences()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.FiveLocus_ExpressingAlleles_WithTruncatedNames,
                    mismatchHlaSet.FiveLocus_ExpressingAlleles_WithTruncatedNames)
                .TenOutOfTen()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donor_FiveLocus_SingleAlleles.DonorId);

            const int minPossibleGradeScore = 10 * (int)MatchGrade.PGroup;
            const int maxPossibleGradeScore = 10 * (int)MatchGrade.GDna;
            result.GradeScore.Should().BeInRange(minPossibleGradeScore, maxPossibleGradeScore);

            const int minPossibleConfidenceScore = 10 * (int)MatchConfidence.Exact;
            const int maxPossibleConfidenceScore = 10 * (int)MatchConfidence.Definite;
            result.ConfidenceScore.Should().BeInRange(minPossibleConfidenceScore, maxPossibleConfidenceScore);

            var expectedMatchConfidences = new List<MatchConfidence> { MatchConfidence.Definite, MatchConfidence.Exact };
            expectedMatchConfidences.Should().Contain(result.OverallMatchConfidence);
        }

        [Test]
        public async Task Search_TenOutOfTen_PatientHasAlleleStrings_DonorHasSingleAlleles_ReturnsMolecularGrades_AndPotentialConfidences()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.FiveLocus_XxCodes,
                    mismatchHlaSet.FiveLocus_XxCodes)
                .TenOutOfTen()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donor_FiveLocus_SingleAlleles.DonorId);

            const int minPossibleGradeScore = 10 * (int)MatchGrade.PGroup;
            const int maxPossibleGradeScore = 10 * (int)MatchGrade.GGroup;
            result.GradeScore.Should().BeInRange(minPossibleGradeScore, maxPossibleGradeScore);

            const int expectedConfidenceScore = 10 * (int)MatchConfidence.Potential;
            result.ConfidenceScore.Should().Be(expectedConfidenceScore);

            result.OverallMatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public async Task Search_TenOutOfTen_PatientHasSerologies_DonorHasSingleAlleles_ReturnsSerologyGrades_AndPotentialConfidences()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.FiveLocus_Serologies,
                    mismatchHlaSet.FiveLocus_Serologies)
                .TenOutOfTen()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donor_FiveLocus_SingleAlleles.DonorId);

            const int minPossibleGradeScore = 10 * (int)MatchGrade.Broad;
            const int maxPossibleGradeScore = 10 * (int)MatchGrade.Associated;
            result.GradeScore.Should().BeInRange(minPossibleGradeScore, maxPossibleGradeScore);

            const int expectedConfidenceScore = 10 * (int)MatchConfidence.Potential;
            result.ConfidenceScore.Should().Be(expectedConfidenceScore);

            result.OverallMatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        #endregion

        #region Scoring w.r.t. Matching

        [Test]
        public async Task Search_SixOutOfSix_NoMismatchGradesAndConfidencesAssignedAtMatchLoci()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.FiveLocus_SingleExpressingAlleles,
                    mismatchHlaSet.FiveLocus_SingleExpressingAlleles)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donor_FiveLocus_SingleAlleles.DonorId);
            
            // Should be 6/6
            result.OverallMatchConfidence.Should().NotBe(MatchConfidence.Mismatch);

            // Should be 2/2 at A
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);

            // Should be 2/2 at B
            result.SearchResultAtLocusB.ScoreDetailsAtPositionOne.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusB.ScoreDetailsAtPositionOne.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);

            result.SearchResultAtLocusB.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusB.ScoreDetailsAtPositionTwo.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);

            // Should be 2/2 at DRB1
            result.SearchResultAtLocusDrb1.ScoreDetailsAtPositionOne.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusDrb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);

            result.SearchResultAtLocusDrb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusDrb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_FiveOutOfSix_DonorMismatchedAtA_OneMismatchGradeAndConfidenceAssignedAtA_NoneAtBAndDrb1()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.FiveLocus_SingleExpressingAlleles,
                    mismatchHlaSet.FiveLocus_SingleExpressingAlleles)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(Locus.A)
                .WithPositionOneOfSearchHlaMismatchedAt(Locus.A)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donor_FiveLocus_SingleAlleles.DonorId);

            // Should be 5/6
            result.OverallMatchConfidence.Should().Be(MatchConfidence.Mismatch);

            // Should be 1/2 at A
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionTwo.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);

            // Should be 2/2 at B
            result.SearchResultAtLocusB.ScoreDetailsAtPositionOne.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusB.ScoreDetailsAtPositionOne.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);

            result.SearchResultAtLocusB.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusB.ScoreDetailsAtPositionTwo.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);

            // Should be 2/2 at DRB1
            result.SearchResultAtLocusDrb1.ScoreDetailsAtPositionOne.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusDrb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);

            result.SearchResultAtLocusDrb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.SearchResultAtLocusDrb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_SixOutOfSix_PGroupGradeAndPotentialConfidenceAssignedToLociWithMissingTypings()
        {
            // search is missing typings at C and DQB1
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.ThreeLocus_SingleExpressingAlleles,
                    mismatchHlaSet.ThreeLocus_SingleExpressingAlleles)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donor_FiveLocus_SingleAlleles.DonorId);

            // Should be 2x potential P group matches at C
            result.SearchResultAtLocusC.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.PGroup);
            result.SearchResultAtLocusC.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            result.SearchResultAtLocusC.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.PGroup);
            result.SearchResultAtLocusC.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Should be 2x potential P group matches at DQB1
            result.SearchResultAtLocusDqb1.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.PGroup);
            result.SearchResultAtLocusDqb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            result.SearchResultAtLocusDqb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.PGroup);
            result.SearchResultAtLocusDqb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public async Task Search_SixOutOfSix_GradesAndConfidencesCalculatedForLociExcludedFromMatching()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.FiveLocus_XxCodes,
                    mismatchHlaSet.FiveLocus_XxCodes)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donor_FiveLocus_SingleAlleles.DonorId);

            // Should be 2x potential G group matches (XX code vs. Allele) at C
            result.SearchResultAtLocusC.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusC.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            result.SearchResultAtLocusC.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusC.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Should be 2x potential G group matches (XX code vs. Allele) at DQB1
            result.SearchResultAtLocusDqb1.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusDqb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            result.SearchResultAtLocusDqb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusDqb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        #endregion

        private InputDonor BuildTestDonor()
        {
            var expandHlaPhenotypeService = Container.Resolve<IExpandHlaPhenotypeService>();

            var matchingHlaPhenotype = expandHlaPhenotypeService
                .GetPhenotypeOfExpandedHla(defaultHlaSet.FiveLocus_SingleExpressingAlleles)
                .Result;

            return new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithMatchingHla(matchingHlaPhenotype)
                .Build();
        }
    }
}
