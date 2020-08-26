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
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Search
{
    /// <summary>
    /// Tests to cover the DPB1 permissive mismatch feature.
    /// </summary>
    public class Dpb1ScoringTests
    {
        private const string DefaultDpb1Hla = "01:01:01:01";
        private const string MismatchedDpb1HlaWithSameTceGroup = "02:01:02:01";
        private const string MismatchedDpb1HlaWithDifferentTceGroup = "03:01:01:01";
        private const string MismatchedDpb1HlaWithNoTceGroup = "679:01";

        private readonly MatchGrade[] dpb1MismatchGrades =
        {
            MatchGrade.Mismatch,
            MatchGrade.PermissiveMismatch
        };

        private ISearchService searchService;
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
        public void ResolveSearchService()
        {
            searchService = DependencyInjection.DependencyInjection.Provider.GetService<ISearchService>();
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorHaveTwoMatchingDpb1Typings_MatchCountIs2AtDpb1()
        {
            var result = await RunSixOutOfSixSearchWithAllLociScored(defaultPhenotype);

            result.ScoringResult.ScoringResultsByLocus.Dpb1.MatchCount.Should().Be(2);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorHaveTwoMatchingDpb1Typings_NoMismatchGradesAtDpb1()
        {
            var result = await RunSixOutOfSixSearchWithAllLociScored(defaultPhenotype);

            dpb1MismatchGrades.Should().NotContain(result.ScoringResult.ScoringResultsByLocus.Dpb1.ScoreDetailsAtPositionOne.MatchGrade);
            dpb1MismatchGrades.Should().NotContain(result.ScoringResult.ScoringResultsByLocus.Dpb1.ScoreDetailsAtPositionTwo.MatchGrade);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorHaveTwoMatchingDpb1Typings_NoMismatchConfidencesAtDpb1()
        {
            var result = await RunSixOutOfSixSearchWithAllLociScored(defaultPhenotype);

            result.ScoringResult.ScoringResultsByLocus.Dpb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);
            result.ScoringResult.ScoringResultsByLocus.Dpb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorDpb1TypingsAreMismatched_ButHaveSameTceGroup_MatchCountIs0AtDpb1()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(MismatchedDpb1HlaWithSameTceGroup);
            var result = await RunSixOutOfSixSearchWithAllLociScored(patientPhenotype);

            result.ScoringResult.ScoringResultsByLocus.Dpb1.MatchCount.Should().Be(0);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorDpb1TypingsAreMismatched_ButHaveSameTceGroup_TwoPermissiveMismatchGradesAtDpb1()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(MismatchedDpb1HlaWithSameTceGroup);
            var result = await RunSixOutOfSixSearchWithAllLociScored(patientPhenotype);

            result.ScoringResult.ScoringResultsByLocus.Dpb1.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.PermissiveMismatch);
            result.ScoringResult.ScoringResultsByLocus.Dpb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.PermissiveMismatch);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorDpb1TypingsAreMismatched_ButHaveSameTceGroup_TwoMismatchConfidencesAtDpb1()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(MismatchedDpb1HlaWithSameTceGroup);
            var result = await RunSixOutOfSixSearchWithAllLociScored(patientPhenotype);

            result.ScoringResult.ScoringResultsByLocus.Dpb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
            result.ScoringResult.ScoringResultsByLocus.Dpb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorHaveTwoMismatchedDpb1Typings_ButHaveDifferentTceGroups_MatchCountIs0AtDpb1()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(MismatchedDpb1HlaWithSameTceGroup);
            var result = await RunSixOutOfSixSearchWithAllLociScored(patientPhenotype);

            result.ScoringResult.ScoringResultsByLocus.Dpb1.MatchCount.Should().Be(0);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorHaveTwoMismatchedDpb1Typings_ButHaveDifferentTceGroups_TwoMismatchGradesAtDpb1()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(MismatchedDpb1HlaWithDifferentTceGroup);
            var result = await RunSixOutOfSixSearchWithAllLociScored(patientPhenotype);

            result.ScoringResult.ScoringResultsByLocus.Dpb1.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.ScoringResult.ScoringResultsByLocus.Dpb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.Mismatch);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorHaveTwoMismatchedDpb1Typings_ButHaveDifferentTceGroups_TwoMismatchConfidencesAtDpb1()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(MismatchedDpb1HlaWithDifferentTceGroup);
            var result = await RunSixOutOfSixSearchWithAllLociScored(patientPhenotype);

            result.ScoringResult.ScoringResultsByLocus.Dpb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
            result.ScoringResult.ScoringResultsByLocus.Dpb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorHaveTwoMismatchedDpb1Typings_ButPatientHasNoTceGroupAssignments_MatchCountIs0AtDpb1()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(MismatchedDpb1HlaWithSameTceGroup);
            var result = await RunSixOutOfSixSearchWithAllLociScored(patientPhenotype);

            result.ScoringResult.ScoringResultsByLocus.Dpb1.MatchCount.Should().Be(0);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorHaveTwoMismatchedDpb1Typings_ButPatientHasNoTceGroupAssignments_TwoMismatchGradesAtDpb1()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(MismatchedDpb1HlaWithNoTceGroup);
            var result = await RunSixOutOfSixSearchWithAllLociScored(patientPhenotype);

            result.ScoringResult.ScoringResultsByLocus.Dpb1.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.ScoringResult.ScoringResultsByLocus.Dpb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.Mismatch);
        }

        [Test]
        public async Task
            Search_SixOutOfSix_PatientAndDonorHaveTwoMismatchedDpb1Typings_ButPatientHasNoTceGroupAssignments_TwoMismatchConfidencesAtDpb1()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(MismatchedDpb1HlaWithNoTceGroup);
            var result = await RunSixOutOfSixSearchWithAllLociScored(patientPhenotype);

            result.ScoringResult.ScoringResultsByLocus.Dpb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
            result.ScoringResult.ScoringResultsByLocus.Dpb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_SixOutOfSix_WhenDpb1IsExcludedFromAggregateScoring_AndResultMismatchedAtDpb1Only_TotalScoreIsNotMismatch()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(MismatchedDpb1HlaWithNoTceGroup);
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
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(MismatchedDpb1HlaWithNoTceGroup);
            var result = await RunSixOutOfSixSearchWithAllLociScored(patientPhenotype);

            result.ScoringResult.MatchCategory.Should().Be(MatchCategory.Mismatch);
        }

        private static PhenotypeInfo<string> GetDefaultPhenotype()
        {
            var defaultHlaSet = new SampleTestHlas.HeterozygousSet1();
            return defaultHlaSet.SixLocus_SingleExpressingAlleles.SetLocus(Locus.Dpb1, DefaultDpb1Hla);
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