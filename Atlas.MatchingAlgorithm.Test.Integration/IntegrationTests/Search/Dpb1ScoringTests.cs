using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
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
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using NUnit.Framework.Constraints;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Search
{
    /// <summary>
    /// Tests to cover the DPB1 permissive mismatch feature.
    /// </summary>
    public class Dpb1ScoringTests
    {
        private const string AlleleInTceGroup3 = "01:01:01:01";
        private const string AlleleInTceGroup3_Other = "02:01:02:01";
        private const string AlleleInTceGroup2 = "03:01:01:01";
        private const string AlleleInTceGroup2_Other = "08:01";
        private const string AlleleInTceGroup1 = "10:01:01:01";
        private const string AlleleInTceGroup1_Other = "09:01:01";
        private const string AlleleWithoutTceGroup = "679:01";
        private const string AlleleWithoutTceGroup_Other = "1029:01";

        private readonly Dictionary<int?, string> PatientDpb1Hla = new Dictionary<int?, string>
        {
            {1, AlleleInTceGroup1},
            {2, AlleleInTceGroup2},
            {3, AlleleInTceGroup3},
        };

        private readonly Dictionary<int?, string> DonorDpb1Hla = new Dictionary<int?, string>
        {
            {1, AlleleInTceGroup1_Other},
            {2, AlleleInTceGroup2_Other},
            {3, AlleleInTceGroup3_Other},
        };

        private ISearchService searchService;
        private IScoringRequestService scoringRequestService;
        private PhenotypeInfo<string> defaultPhenotype;
        private int testDonorId;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                defaultPhenotype = GetDefaultPhenotype();
                testDonorId = SetupTestDonor(defaultPhenotype);
            });
        }

        [SetUp]
        public void SetUp()
        {
            scoringRequestService = DependencyInjection.DependencyInjection.Provider.GetService<IScoringRequestService>();
            searchService = DependencyInjection.DependencyInjection.Provider.GetService<ISearchService>();
        }

        /// <summary>
        /// Integers used in tandem with <see cref="PatientDpb1Hla"/> and <see cref="DonorDpb1Hla"/> to allow the test cases here to be
        /// very easily compared to the TCE group functional specification, which uses the group integers
        /// </summary>
        // Donor 11
        [TestCase(1, 1, 1, 1, LocusMatchCategory.PermissiveMismatch)]
        [TestCase(1, 2, 1, 1, LocusMatchCategory.PermissiveMismatch)]
        [TestCase(1, 3, 1, 1, LocusMatchCategory.PermissiveMismatch)]
        [TestCase(2, 2, 1, 1, LocusMatchCategory.Mismatch)]
        [TestCase(2, 3, 1, 1, LocusMatchCategory.Mismatch)]
        [TestCase(3, 3, 1, 1, LocusMatchCategory.Mismatch)]
        // Donor 12
        [TestCase(1, 1, 1, 2, LocusMatchCategory.PermissiveMismatch)]
        [TestCase(1, 2, 1, 2, LocusMatchCategory.PermissiveMismatch)]
        [TestCase(1, 3, 1, 2, LocusMatchCategory.PermissiveMismatch)]
        [TestCase(2, 2, 1, 2, LocusMatchCategory.Mismatch)]
        [TestCase(2, 3, 1, 2, LocusMatchCategory.Mismatch)]
        [TestCase(3, 3, 1, 2, LocusMatchCategory.Mismatch)]
        // Donor 13
        [TestCase(1, 1, 1, 3, LocusMatchCategory.PermissiveMismatch)]
        [TestCase(1, 2, 1, 3, LocusMatchCategory.PermissiveMismatch)]
        [TestCase(1, 3, 1, 3, LocusMatchCategory.PermissiveMismatch)]
        [TestCase(2, 2, 1, 3, LocusMatchCategory.Mismatch)]
        [TestCase(2, 3, 1, 3, LocusMatchCategory.Mismatch)]
        [TestCase(3, 3, 1, 3, LocusMatchCategory.Mismatch)]
        // Donor 22
        [TestCase(1, 1, 2, 2, LocusMatchCategory.Mismatch)]
        [TestCase(1, 2, 2, 2, LocusMatchCategory.Mismatch)]
        [TestCase(1, 3, 2, 2, LocusMatchCategory.Mismatch)]
        [TestCase(2, 2, 2, 2, LocusMatchCategory.PermissiveMismatch)]
        [TestCase(2, 3, 2, 2, LocusMatchCategory.PermissiveMismatch)]
        [TestCase(3, 3, 2, 2, LocusMatchCategory.Mismatch)]
        // Donor 23
        [TestCase(1, 1, 2, 3, LocusMatchCategory.Mismatch)]
        [TestCase(1, 2, 2, 3, LocusMatchCategory.Mismatch)]
        [TestCase(1, 3, 2, 3, LocusMatchCategory.Mismatch)]
        [TestCase(2, 2, 2, 3, LocusMatchCategory.PermissiveMismatch)]
        [TestCase(2, 3, 2, 3, LocusMatchCategory.PermissiveMismatch)]
        [TestCase(3, 3, 2, 3, LocusMatchCategory.Mismatch)]
        // Donor 33
        [TestCase(1, 1, 3, 3, LocusMatchCategory.Mismatch)]
        [TestCase(1, 2, 3, 3, LocusMatchCategory.Mismatch)]
        [TestCase(1, 3, 3, 3, LocusMatchCategory.Mismatch)]
        [TestCase(2, 2, 3, 3, LocusMatchCategory.Mismatch)]
        [TestCase(2, 3, 3, 3, LocusMatchCategory.Mismatch)]
        [TestCase(3, 3, 3, 3, LocusMatchCategory.PermissiveMismatch)]
        public async Task Dpb1Scoring_AssignsCorrectPerLocusMatchCategory(
            int patientTceGroup1,
            int patientTceGroup2,
            int donorTceGroup1,
            int donorTceGroup2,
            LocusMatchCategory expectedMatchCategory
        )
        {
            var donorPhenotype = GetDefaultPhenotype()
                .SetPosition(Locus.Dpb1, LocusPosition.One, DonorDpb1Hla[donorTceGroup1])
                .SetPosition(Locus.Dpb1, LocusPosition.Two, DonorDpb1Hla[donorTceGroup2]);

            var patientPhenotype = GetDefaultPhenotype()
                .SetPosition(Locus.Dpb1, LocusPosition.One, PatientDpb1Hla[patientTceGroup1])
                .SetPosition(Locus.Dpb1, LocusPosition.Two, PatientDpb1Hla[patientTceGroup2]);

            var scoringResult = await scoringRequestService.Score(new DonorHlaScoringRequest
            {
                DonorHla = donorPhenotype.ToPhenotypeInfoTransfer(),
                PatientHla = patientPhenotype.ToPhenotypeInfoTransfer(),
                ScoringCriteria = new ScoringCriteria
                {
                    LociToScore = new List<Locus> {Locus.Dpb1},
                    LociToExcludeFromAggregateScore = new List<Locus>()
                }
            });

            scoringResult.SearchResultAtLocusDpb1.MatchCategory.Should().Be(expectedMatchCategory);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorHaveTwoMismatchedDpb1Typings_ButPatientHasNoTceGroupAssignments_MatchCategoryIsUnknown()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(AlleleWithoutTceGroup);
            var result = await RunSixOutOfSixSearchWithAllLociScored(patientPhenotype);

            result.ScoringResult.ScoringResultsByLocus.Dpb1.MatchCategory.Should().Be(LocusMatchCategory.Unknown);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorHaveTwoMismatchedDpb1Typings_ButPatientHasNoTceGroupAssignments_TwoMismatchGradesAtDpb1()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(AlleleInTceGroup1);
            var result = await RunSixOutOfSixSearchWithAllLociScored(patientPhenotype);

            result.ScoringResult.ScoringResultsByLocus.Dpb1.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.ScoringResult.ScoringResultsByLocus.Dpb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.Mismatch);
        }

        [Test]
        public async Task
            Search_SixOutOfSix_PatientAndDonorHaveTwoMismatchedDpb1Typings_ButPatientHasNoTceGroupAssignments_TwoMismatchConfidencesAtDpb1()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(AlleleWithoutTceGroup);
            var result = await RunSixOutOfSixSearchWithAllLociScored(patientPhenotype);

            result.ScoringResult.ScoringResultsByLocus.Dpb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
            result.ScoringResult.ScoringResultsByLocus.Dpb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_SixOutOfSix_WhenDpb1IsExcludedFromAggregateScoring_AndResultMismatchedAtDpb1Only_TotalScoreIsNotMismatch()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(AlleleWithoutTceGroup);
            var searchRequest = new SearchRequestFromHlasBuilder(patientPhenotype)
                .SixOutOfSix()
                .WithAllLociScored()
                .WithDpb1ExcludedFromScoringAggregation()
                .Build();

            var searchResults = await searchService.Search(searchRequest);
            var result = searchResults.SingleOrDefault(d => d.AtlasDonorId == testDonorId);

            result.ScoringResult.MatchCategory.Should().NotBe(MatchCategory.Mismatch);
        }

        [Test]
        public async Task Search_SixOutOfSix_WhenDpb1IsNotExcludedFromAggregateScoring_AndResultMismatchedAtDpb1Only_TotalScoreIsMismatch()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(AlleleWithoutTceGroup);
            var result = await RunSixOutOfSixSearchWithAllLociScored(patientPhenotype);

            result.ScoringResult.MatchCategory.Should().Be(MatchCategory.Mismatch);
        }

        private static PhenotypeInfo<string> GetDefaultPhenotype()
        {
            var defaultHlaSet = new SampleTestHlas.HeterozygousSet1();
            return defaultHlaSet.SixLocus_SingleExpressingAlleles.SetLocus(Locus.Dpb1, AlleleInTceGroup3);
        }

        private static int SetupTestDonor(PhenotypeInfo<string> testDonorPhenotype)
        {
            var testDonor = BuildTestDonor(testDonorPhenotype);
            var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IActiveRepositoryFactory>();
            var donorRepository = repositoryFactory.GetDonorUpdateRepository();
            donorRepository.InsertBatchOfDonorsWithExpandedHla(new[] {testDonor}, false).Wait();
            return testDonor.DonorId;
        }

        private static DonorInfoWithExpandedHla BuildTestDonor(PhenotypeInfo<string> testDonorPhenotype)
        {
            var donorHlaExpander = DependencyInjection.DependencyInjection.Provider.GetService<IDonorHlaExpanderFactory>()
                .BuildForActiveHlaNomenclatureVersion();

            var matchingHlaPhenotype = donorHlaExpander.ExpandDonorHlaAsync(new DonorInfo {HlaNames = testDonorPhenotype}).Result.MatchingHla;

            return new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(matchingHlaPhenotype)
                .Build();
        }

        private PhenotypeInfo<string> GetPhenotypeWithDpb1HlaOf(string dpb1Hla) =>
            new PhenotypeInfo<string>(defaultPhenotype).SetLocus(Locus.Dpb1, dpb1Hla);

        private async Task<MatchingAlgorithmResult> RunSixOutOfSixSearchWithAllLociScored(PhenotypeInfo<string> patientPhenotype)
        {
            var searchRequest = new SearchRequestFromHlasBuilder(patientPhenotype)
                .SixOutOfSix()
                .WithAllLociScored()
                .Build();

            var searchResults = await searchService.Search(searchRequest);
            return searchResults.SingleOrDefault(d => d.AtlasDonorId == testDonorId);
        }
    }
}