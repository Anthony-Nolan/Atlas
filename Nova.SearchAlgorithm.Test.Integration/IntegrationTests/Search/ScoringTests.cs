using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Services.MatchingDictionary;
using Nova.SearchAlgorithm.Services.Search;
using Nova.SearchAlgorithm.Test.Integration.TestData;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using NUnit.Framework;

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
    public class ScoringTests
    {
        private ISearchService searchService;
        private ITestHlaSet defaultHlaSet;
        private ITestHlaSet mismatchHlaSet;
        private InputDonorWithExpandedHla testDonor;

        [OneTimeSetUp]
        public void ImportTestDonor()
        {
            // source of donor HLA phenotypes
            defaultHlaSet = new TestHla.HeterozygousSet1();
            mismatchHlaSet = new TestHla.HeterozygousSet2();

            testDonor = BuildTestDonor();
            var donorRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorImportRepository>();
            donorRepository.InsertDonorWithExpandedHla(testDonor).Wait();
        }

        [SetUp]
        public void ResolveSearchService()
        {
            searchService = DependencyInjection.DependencyInjection.Provider.GetService<ISearchService>();
        }

        #region Scoring of the different HLA typing categories

        [Test]
        public async Task Search_TenOutOfTen_PatientAndDonorHaveSingleAlleles_ReturnsMolecularGrades_AndAlleleLevelConfidences()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.SixLocus_SingleExpressingAlleles,
                    mismatchHlaSet.SixLocus_SingleExpressingAlleles)
                .TenOutOfTen()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == testDonor.DonorId);

            var expectedMatchConfidences = new List<MatchConfidence> { MatchConfidence.Definite, MatchConfidence.Exact };
            expectedMatchConfidences.Should().Contain(result.OverallMatchConfidence);
        }

        [Test]
        public async Task Search_TenOutOfTen_PatientHasMultipleAlleles_DonorHasSingleAlleles_ReturnsMolecularGrades_AndAlleleLevelConfidences()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.SixLocus_ExpressingAlleles_WithTruncatedNames,
                    mismatchHlaSet.SixLocus_ExpressingAlleles_WithTruncatedNames)
                .TenOutOfTen()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == testDonor.DonorId);

            var expectedMatchConfidences = new List<MatchConfidence> { MatchConfidence.Definite, MatchConfidence.Exact };
            expectedMatchConfidences.Should().Contain(result.OverallMatchConfidence);
        }

        [Test]
        public async Task Search_TenOutOfTen_PatientHasAlleleStrings_DonorHasSingleAlleles_ReturnsMolecularGrades_AndPotentialConfidences()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.SixLocus_XxCodes,
                    mismatchHlaSet.SixLocus_XxCodes)
                .TenOutOfTen()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == testDonor.DonorId);

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
            var result = results.SingleOrDefault(d => d.DonorId == testDonor.DonorId);

            result.OverallMatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        #endregion

        #region Scoring w.r.t. Matching

        [Test]
        public async Task Search_SixOutOfSix_NoMismatchGradesAndConfidencesAssignedAtMatchLoci()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.SixLocus_SingleExpressingAlleles,
                    mismatchHlaSet.SixLocus_SingleExpressingAlleles)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == testDonor.DonorId);
            
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
                    defaultHlaSet.SixLocus_SingleExpressingAlleles,
                    mismatchHlaSet.SixLocus_SingleExpressingAlleles)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(Locus.A)
                .WithPositionOneOfSearchHlaMismatchedAt(Locus.A)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == testDonor.DonorId);

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
            var result = results.SingleOrDefault(d => d.DonorId == testDonor.DonorId);

            // Should be 2x potential P group matches at C
            result.SearchResultAtLocusC.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.PGroup);
            result.SearchResultAtLocusC.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            result.SearchResultAtLocusC.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.PGroup);
            result.SearchResultAtLocusC.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Should be 2x potential P group matches at DPB1
            result.SearchResultAtLocusDpb1.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.PGroup);
            result.SearchResultAtLocusDpb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            result.SearchResultAtLocusDpb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.PGroup);
            result.SearchResultAtLocusDpb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Potential);

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
                    defaultHlaSet.SixLocus_XxCodes,
                    mismatchHlaSet.SixLocus_XxCodes)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == testDonor.DonorId);

            // Should be 2x potential G group matches (XX code vs. Allele) at C
            result.SearchResultAtLocusC.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusC.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            result.SearchResultAtLocusC.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusC.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Should be 2x potential G group matches (XX code vs. Allele) at DPB1
            result.SearchResultAtLocusDqb1.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusDqb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            result.SearchResultAtLocusDqb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusDqb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Should be 2x potential G group matches (XX code vs. Allele) at DQB1
            result.SearchResultAtLocusDqb1.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusDqb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            result.SearchResultAtLocusDqb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusDqb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        #endregion

        private InputDonorWithExpandedHla BuildTestDonor()
        {
            var expandHlaPhenotypeService = DependencyInjection.DependencyInjection.Provider.GetService<IExpandHlaPhenotypeService>();

            var matchingHlaPhenotype = expandHlaPhenotypeService
                .GetPhenotypeOfExpandedHla(defaultHlaSet.SixLocus_SingleExpressingAlleles)
                .Result;

            return new InputDonorWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithMatchingHla(matchingHlaPhenotype)
                .Build();
        }
    }
}
