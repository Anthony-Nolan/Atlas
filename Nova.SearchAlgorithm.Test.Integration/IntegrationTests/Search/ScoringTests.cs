using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Extensions.MatchingDictionaryConversionExtensions;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Test.Integration.TestData;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using NUnit.Framework;
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
        private IHlaMatchingLookupService matchingLookupService;

        private ITestHlaSet defaultHlaSet;
        private ITestHlaSet mismatchHlaSet;

        private InputDonor donor_FiveLocus_SingleAlleles;

        [OneTimeSetUp]
        public void ImportTestDonor()
        {
            matchingLookupService = Container.Resolve<IHlaMatchingLookupService>();

            // source of donor HLA phenotypes
            defaultHlaSet = new TestHla.HeterozygousSet1();
            mismatchHlaSet = new TestHla.HeterozygousSet2();

            // build test donors
            donor_FiveLocus_SingleAlleles = new InputDonorBuilder(DonorIdGenerator.NextId())
            .WithMatchingHla(BuildMatchingHlaPhenotype(defaultHlaSet.FiveLocus_SingleExpressingAlleles))
            .Build();

            // add test donors to repository
            var donorRepository = Container.Resolve<IDonorImportRepository>();
            donorRepository.AddOrUpdateDonorWithHla(donor_FiveLocus_SingleAlleles).Wait();
        }

        [SetUp]
        public void ResolveSearchService()
        {
            searchService = Container.Resolve<ISearchService>();
        }

        #region Total Match Grade

        // TODO: NOVA-1472 - implement

        #endregion

        #region Total Match Confidence

        // TODO: NOVA-1472 - implement

        #endregion

        #region Locus Match Grades

        // TODO: NOVA-1472 - implement

        #endregion

        #region Locus Match Confidences

        // TODO: NOVA-1472 - implement

        #endregion

        #region Scoring w.r.t. Matching

        [Test]
        public async Task Search_SixOutOfSix_ZeroMismatchGradesAndConfidencesAssigned()
        {
            var searchRequest = new ThreeLocusSearchRequestBuilder(
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
        public async Task Search_FiveOutOfSix_DonorMismatchedAtA_OneMismatchGradeAndConfidenceAssignedAtA()
        {
            var searchRequest = new ThreeLocusSearchRequestBuilder(
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
            var searchRequest = new ThreeLocusSearchRequestBuilder(
                    defaultHlaSet.ThreeLocus_SingleExpressingAlleles,
                    mismatchHlaSet.ThreeLocus_SingleExpressingAlleles)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donor_FiveLocus_SingleAlleles.DonorId);

            // Should be potential 2/2 at C
            result.SearchResultAtLocusC.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.PGroup);
            result.SearchResultAtLocusC.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            result.SearchResultAtLocusC.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.PGroup);
            result.SearchResultAtLocusC.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Potential);

            // Should be potential 2/2 at DQB1
            result.SearchResultAtLocusDqb1.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.PGroup);
            result.SearchResultAtLocusDqb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);

            result.SearchResultAtLocusDqb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.PGroup);
            result.SearchResultAtLocusDqb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public async Task Search_SixOutOfSix_GradesAndConfidencesCalculatedForLociExcludedFromMatching()
        {
            var searchRequest = new ThreeLocusSearchRequestBuilder(
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

        private PhenotypeInfo<ExpandedHla> BuildMatchingHlaPhenotype(PhenotypeInfo<string> hlas)
        {
            return hlas.Map((locus, positions, hlaName) => GetExpandedHla(locus, hlaName));
        }

        private ExpandedHla GetExpandedHla(Locus locus, string hlaName)
        {
            return hlaName == null
                ? null
                : matchingLookupService
                    .GetHlaLookupResult(locus.ToMatchLocus(), hlaName)
                    .Result
                    .ToExpandedHla(hlaName);
        }
    }
}
