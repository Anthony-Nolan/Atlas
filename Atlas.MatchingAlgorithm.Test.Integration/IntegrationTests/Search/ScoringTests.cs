using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Test.Integration.Resources.TestData;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Search
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
        private DonorInfoWithExpandedHla testDonor;

        [OneTimeSetUp]
        public void ImportTestDonor()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                // source of donor HLA phenotypes
                defaultHlaSet = new SampleTestHlas.HeterozygousSet1();
                mismatchHlaSet = new SampleTestHlas.HeterozygousSet2();

                testDonor = BuildTestDonor();
                var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IActiveRepositoryFactory>();
                var donorRepository = repositoryFactory.GetDonorUpdateRepository();
                donorRepository.InsertBatchOfDonorsWithExpandedHla(new[] { testDonor }, false).Wait();
            });
        }

        [SetUp]
        public void ResolveSearchService()
        {
            searchService = DependencyInjection.DependencyInjection.Provider.GetService<ISearchService>();
        }

        #region Score no, some or all loci

        [Test]
        public async Task Search_ScoreNoLoci_DoesNotReturnAggregatedScoringResults()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.SixLocus_SingleExpressingAlleles,
                    mismatchHlaSet.SixLocus_SingleExpressingAlleles)
                .TenOutOfTen()
                .WithLociToScore(new List<Locus>())
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.AtlasDonorId == testDonor.DonorId);

            result.ScoringResult.TypedLociCountAtScoredLoci.Should().BeNull();
            result.ScoringResult.GradeScore.Should().BeNull();
            result.ScoringResult.ConfidenceScore.Should().BeNull();
            result.ScoringResult.MatchCategory.Should().BeNull();
        }
        
        [Test]
        public async Task Search_ScoreNoLoci_ReturnsTypedLociCount()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.SixLocus_SingleExpressingAlleles,
                    mismatchHlaSet.SixLocus_SingleExpressingAlleles)
                .TenOutOfTen()
                .WithLociToScore(new List<Locus>())
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.AtlasDonorId == testDonor.DonorId);

            result?.MatchingResult.TypedLociCount.Should().Be(6);
        }

        [Test]
        public async Task Search_ScoreSomeLoci_ReturnsAggregatedScoringResults()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.SixLocus_SingleExpressingAlleles,
                    mismatchHlaSet.SixLocus_SingleExpressingAlleles)
                .TenOutOfTen()
                .WithLociToScore(new List<Locus> { Locus.A })
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.AtlasDonorId == testDonor.DonorId);

            result.ScoringResult.TypedLociCountAtScoredLoci.Should().Be(1);
            result.ScoringResult.GradeScore.Should().NotBeNull();
            result.ScoringResult.ConfidenceScore.Should().NotBeNull();
            result.ScoringResult.MatchCategory.Should().NotBeNull();
        }

        [Test]
        public async Task Search_ScoreAllLoci_ReturnsAggregatedScoringResults()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.SixLocus_SingleExpressingAlleles,
                    mismatchHlaSet.SixLocus_SingleExpressingAlleles)
                .TenOutOfTen()
                .WithAllLociScored()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.AtlasDonorId == testDonor.DonorId);

            result.ScoringResult.TypedLociCountAtScoredLoci.Should().Be(6);
            result.ScoringResult.GradeScore.Should().NotBeNull();
            result.ScoringResult.ConfidenceScore.Should().NotBeNull();
            result.ScoringResult.MatchCategory.Should().NotBeNull();
        }

        [Test]
        public async Task Search_ScoreSomeLoci_OnlyReturnsLocusScoringResultsForScoredLoci()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.SixLocus_SingleExpressingAlleles,
                    mismatchHlaSet.SixLocus_SingleExpressingAlleles)
                .TenOutOfTen()
                .WithLociToScore(new List<Locus> { Locus.Dpb1 })
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.AtlasDonorId == testDonor.DonorId);

            var scoredLocus = result.ScoringResult.SearchResultsByLocus.Dpb1;
            scoredLocus.IsLocusTyped.Should().BeTrue();
            scoredLocus.MatchGradeScore.Should().NotBeNull();
            scoredLocus.MatchConfidenceScore.Should().NotBeNull();
            scoredLocus.ScoreDetailsAtPositionOne.Should().NotBeNull();
            scoredLocus.ScoreDetailsAtPositionTwo.Should().NotBeNull();

            var notScoredLocus = result.ScoringResult.SearchResultsByLocus.A;
            notScoredLocus.IsLocusTyped.Should().BeNull();
            notScoredLocus.MatchGradeScore.Should().BeNull();
            notScoredLocus.MatchConfidenceScore.Should().BeNull();
            notScoredLocus.ScoreDetailsAtPositionOne.Should().BeNull();
            notScoredLocus.ScoreDetailsAtPositionTwo.Should().BeNull();
        }

        #endregion

        #region Scoring of the different HLA typing categories

        [Test]
        public async Task Search_TenOutOfTen_PatientAndDonorHaveSingleAlleles_ReturnsDefiniteOrExactMatchCategory()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.SixLocus_SingleExpressingAlleles,
                    mismatchHlaSet.SixLocus_SingleExpressingAlleles)
                .TenOutOfTen()
                .WithAllLociScored()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.AtlasDonorId == testDonor.DonorId);

            var expectedMatchCategories = new List<MatchCategory?> { MatchCategory.Definite, MatchCategory.Exact };
            expectedMatchCategories.Should().Contain(result.ScoringResult.MatchCategory);
        }

        [Test]
        public async Task Search_TenOutOfTen_PatientHasMultipleAlleles_DonorHasSingleAlleles_ReturnsDefiniteOrExactMatchCategory()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.SixLocus_ExpressingAlleles_WithTruncatedNames,
                    mismatchHlaSet.SixLocus_ExpressingAlleles_WithTruncatedNames)
                .TenOutOfTen()
                .WithAllLociScored()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.AtlasDonorId == testDonor.DonorId);

            var expectedMatchCategories = new List<MatchCategory?> { MatchCategory.Definite, MatchCategory.Exact };
            expectedMatchCategories.Should().Contain(result.ScoringResult.MatchCategory);
        }

        [Test]
        public async Task Search_TenOutOfTen_PatientHasAlleleStrings_DonorHasSingleAlleles_ReturnsPotentialMatchCategory()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.SixLocus_XxCodes,
                    mismatchHlaSet.SixLocus_XxCodes)
                .TenOutOfTen()
                .WithAllLociScored()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.AtlasDonorId == testDonor.DonorId);

            result.ScoringResult.MatchCategory.Should().Be(MatchCategory.Potential);
        }

        [Test]
        public async Task Search_TenOutOfTen_PatientHasSerologies_DonorHasSingleAlleles_ReturnsPotentialMatchCategory()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.FiveLocus_Serologies,
                    mismatchHlaSet.FiveLocus_Serologies)
                .TenOutOfTen()
                .WithAllLociScored()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.AtlasDonorId == testDonor.DonorId);

            result.ScoringResult.MatchCategory.Should().Be(MatchCategory.Potential);
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
                .WithAllLociScored()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.AtlasDonorId == testDonor.DonorId);

            // Should be 6/6
            result.ScoringResult.MatchCategory.Should().NotBe(MatchCategory.Mismatch);

            // Should be 2/2 at A
            result.ScoringResult.SearchResultsByLocus.A.ScoreDetailsAtPositionOne.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.ScoringResult.SearchResultsByLocus.A.ScoreDetailsAtPositionOne.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);

            result.ScoringResult.SearchResultsByLocus.A.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.ScoringResult.SearchResultsByLocus.A.ScoreDetailsAtPositionTwo.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);

            // Should be 2/2 at B
            result.ScoringResult.SearchResultsByLocus.B.ScoreDetailsAtPositionOne.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.ScoringResult.SearchResultsByLocus.B.ScoreDetailsAtPositionOne.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);

            result.ScoringResult.SearchResultsByLocus.B.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.ScoringResult.SearchResultsByLocus.B.ScoreDetailsAtPositionTwo.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);

            // Should be 2/2 at DRB1
            result.ScoringResult.SearchResultsByLocus.Drb1.ScoreDetailsAtPositionOne.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.ScoringResult.SearchResultsByLocus.Drb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);

            result.ScoringResult.SearchResultsByLocus.Drb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.ScoringResult.SearchResultsByLocus.Drb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);
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
                .WithAllLociScored()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.AtlasDonorId == testDonor.DonorId);

            // Should be 5/6
            result.ScoringResult.MatchCategory.Should().Be(MatchCategory.Mismatch);

            // Should be 1/2 at A
            result.ScoringResult.SearchResultsByLocus.A.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.ScoringResult.SearchResultsByLocus.A.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);

            result.ScoringResult.SearchResultsByLocus.A.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.ScoringResult.SearchResultsByLocus.A.ScoreDetailsAtPositionTwo.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);

            // Should be 2/2 at B
            result.ScoringResult.SearchResultsByLocus.B.ScoreDetailsAtPositionOne.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.ScoringResult.SearchResultsByLocus.B.ScoreDetailsAtPositionOne.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);

            result.ScoringResult.SearchResultsByLocus.B.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.ScoringResult.SearchResultsByLocus.B.ScoreDetailsAtPositionTwo.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);

            // Should be 2/2 at DRB1
            result.ScoringResult.SearchResultsByLocus.Drb1.ScoreDetailsAtPositionOne.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.ScoringResult.SearchResultsByLocus.Drb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);

            result.ScoringResult.SearchResultsByLocus.Drb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().NotBe(MatchGrade.Mismatch);
            result.ScoringResult.SearchResultsByLocus.Drb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_SixOutOfSix_PGroupGradeAndPotentialConfidenceAssignedToLociWithMissingTypings()
        {
            // search is missing typings at C and DQB1
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.ThreeLocus_SingleExpressingAlleles,
                    mismatchHlaSet.ThreeLocus_SingleExpressingAlleles)
                .SixOutOfSix()
                .WithAllLociScored()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.AtlasDonorId == testDonor.DonorId);

            // Should be 2x potential P group matches at C
            result.ScoringResult.SearchResultsByLocus.C.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.PGroup);
            result.ScoringResult.SearchResultsByLocus.C.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            result.ScoringResult.SearchResultsByLocus.C.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.PGroup);
            result.ScoringResult.SearchResultsByLocus.C.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Should be 2x potential P group matches at DPB1
            result.ScoringResult.SearchResultsByLocus.Dpb1.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.PGroup);
            result.ScoringResult.SearchResultsByLocus.Dpb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            result.ScoringResult.SearchResultsByLocus.Dpb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.PGroup);
            result.ScoringResult.SearchResultsByLocus.Dpb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Should be 2x potential P group matches at DQB1
            result.ScoringResult.SearchResultsByLocus.Dqb1.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.PGroup);
            result.ScoringResult.SearchResultsByLocus.Dqb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            result.ScoringResult.SearchResultsByLocus.Dqb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.PGroup);
            result.ScoringResult.SearchResultsByLocus.Dqb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public async Task Search_SixOutOfSix_GradesAndConfidencesCalculatedForLociExcludedFromMatchingButIncludedInScoring()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(
                    defaultHlaSet.SixLocus_XxCodes,
                    mismatchHlaSet.SixLocus_XxCodes)
                .SixOutOfSix()
                .WithLociToScore(new List<Locus> { Locus.C, Locus.Dpb1, Locus.Dqb1 })
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.AtlasDonorId == testDonor.DonorId);

            // Should be 2x potential G group matches (XX code vs. Allele) at C
            result.ScoringResult.SearchResultsByLocus.C.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.ScoringResult.SearchResultsByLocus.C.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            result.ScoringResult.SearchResultsByLocus.C.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.ScoringResult.SearchResultsByLocus.C.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Should be 2x potential G group matches (XX code vs. Allele) at DPB1
            result.ScoringResult.SearchResultsByLocus.Dqb1.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.ScoringResult.SearchResultsByLocus.Dqb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            result.ScoringResult.SearchResultsByLocus.Dqb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.ScoringResult.SearchResultsByLocus.Dqb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Should be 2x potential G group matches (XX code vs. Allele) at DQB1
            result.ScoringResult.SearchResultsByLocus.Dqb1.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.ScoringResult.SearchResultsByLocus.Dqb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            result.ScoringResult.SearchResultsByLocus.Dqb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.ScoringResult.SearchResultsByLocus.Dqb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        #endregion

        private DonorInfoWithExpandedHla BuildTestDonor()
        {
            var donorHlaExpander = DependencyInjection.DependencyInjection.Provider.GetService<IDonorHlaExpanderFactory>().BuildForActiveHlaNomenclatureVersion();

            var matchingHlaPhenotype = donorHlaExpander.ExpandDonorHlaAsync(new DonorInfo { HlaNames = defaultHlaSet.SixLocus_SingleExpressingAlleles }).Result.MatchingHla;

            return new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(matchingHlaPhenotype)
                .Build();
        }
    }
}
