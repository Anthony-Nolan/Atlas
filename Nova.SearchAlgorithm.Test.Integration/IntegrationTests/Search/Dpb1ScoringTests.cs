using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Common.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
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
            defaultPhenotype = GetDefaultPhenotype();
            testDonorId = SetupTestDonor(defaultPhenotype);
        }

        [SetUp]
        public void ResolveSearchService()
        {
            searchService = DependencyInjection.DependencyInjection.Provider.GetService<ISearchService>();
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorHaveTwoMatchingDpb1Typings_MatchCountIs2AtDpb1()
        {
            var result = await RunSixOutOfSixSearchWithPatientPhenotypeOf(defaultPhenotype);

            result.SearchResultAtLocusDpb1.MatchCount.Should().Be(2);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorHaveTwoMatchingDpb1Typings_NoMismatchGradesAtDpb1()
        {
            var result = await RunSixOutOfSixSearchWithPatientPhenotypeOf(defaultPhenotype);

            dpb1MismatchGrades.Should().NotContain(result.SearchResultAtLocusDpb1.ScoreDetailsAtPositionOne.MatchGrade);
            dpb1MismatchGrades.Should().NotContain(result.SearchResultAtLocusDpb1.ScoreDetailsAtPositionTwo.MatchGrade);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorHaveTwoMatchingDpb1Typings_NoMismatchConfidencesAtDpb1()
        {
            var result = await RunSixOutOfSixSearchWithPatientPhenotypeOf(defaultPhenotype);

            result.SearchResultAtLocusDpb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);
            result.SearchResultAtLocusDpb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorDpb1TypingsAreMismatched_ButHaveSameTceGroup_MatchCountIs0AtDpb1()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(MismatchedDpb1HlaWithSameTceGroup);
            var result = await RunSixOutOfSixSearchWithPatientPhenotypeOf(patientPhenotype);

            result.SearchResultAtLocusDpb1.MatchCount.Should().Be(0);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorDpb1TypingsAreMismatched_ButHaveSameTceGroup_TwoPermissiveMismatchGradesAtDpb1()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(MismatchedDpb1HlaWithSameTceGroup);
            var result = await RunSixOutOfSixSearchWithPatientPhenotypeOf(patientPhenotype);

            result.SearchResultAtLocusDpb1.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.PermissiveMismatch);
            result.SearchResultAtLocusDpb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.PermissiveMismatch);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorDpb1TypingsAreMismatched_ButHaveSameTceGroup_TwoMismatchConfidencesAtDpb1()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(MismatchedDpb1HlaWithSameTceGroup);
            var result = await RunSixOutOfSixSearchWithPatientPhenotypeOf(patientPhenotype);

            result.SearchResultAtLocusDpb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
            result.SearchResultAtLocusDpb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorHaveTwoMismatchedDpb1Typings_ButHaveDifferentTceGroups_MatchCountIs0AtDpb1()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(MismatchedDpb1HlaWithSameTceGroup);
            var result = await RunSixOutOfSixSearchWithPatientPhenotypeOf(patientPhenotype);

            result.SearchResultAtLocusDpb1.MatchCount.Should().Be(0);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorHaveTwoMismatchedDpb1Typings_ButHaveDifferentTceGroups_TwoMismatchGradesAtDpb1()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(MismatchedDpb1HlaWithDifferentTceGroup);
            var result = await RunSixOutOfSixSearchWithPatientPhenotypeOf(patientPhenotype);

            result.SearchResultAtLocusDpb1.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusDpb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.Mismatch);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorHaveTwoMismatchedDpb1Typings_ButHaveDifferentTceGroups_TwoMismatchConfidencesAtDpb1()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(MismatchedDpb1HlaWithDifferentTceGroup);
            var result = await RunSixOutOfSixSearchWithPatientPhenotypeOf(patientPhenotype);

            result.SearchResultAtLocusDpb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
            result.SearchResultAtLocusDpb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorHaveTwoMismatchedDpb1Typings_ButPatientHasNoTceGroupAssignments_MatchCountIs0AtDpb1()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(MismatchedDpb1HlaWithSameTceGroup);
            var result = await RunSixOutOfSixSearchWithPatientPhenotypeOf(patientPhenotype);

            result.SearchResultAtLocusDpb1.MatchCount.Should().Be(0);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorHaveTwoMismatchedDpb1Typings_ButPatientHasNoTceGroupAssignments_TwoMismatchGradesAtDpb1()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(MismatchedDpb1HlaWithNoTceGroup);
            var result = await RunSixOutOfSixSearchWithPatientPhenotypeOf(patientPhenotype);

            result.SearchResultAtLocusDpb1.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusDpb1.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(MatchGrade.Mismatch);
        }

        [Test]
        public async Task Search_SixOutOfSix_PatientAndDonorHaveTwoMismatchedDpb1Typings_ButPatientHasNoTceGroupAssignments_TwoMismatchConfidencesAtDpb1()
        {
            var patientPhenotype = GetPhenotypeWithDpb1HlaOf(MismatchedDpb1HlaWithNoTceGroup);
            var result = await RunSixOutOfSixSearchWithPatientPhenotypeOf(patientPhenotype);

            result.SearchResultAtLocusDpb1.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
            result.SearchResultAtLocusDpb1.ScoreDetailsAtPositionTwo.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        private static PhenotypeInfo<string> GetDefaultPhenotype()
        {
            var defaultHlaSet = new TestHla.HeterozygousSet1();
            var phenotype = defaultHlaSet.SixLocus_SingleExpressingAlleles;
            phenotype.SetAtLocus(Locus.Dpb1, DefaultDpb1Hla);
            return phenotype;
        }

        private static int SetupTestDonor(PhenotypeInfo<string> testDonorPhenotype)
        {
            var testDonor = BuildTestDonor(testDonorPhenotype);
            var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IActiveRepositoryFactory>();
            var donorRepository = repositoryFactory.GetDonorUpdateRepository();
            donorRepository.InsertBatchOfDonorsWithExpandedHla(new[] { testDonor }).Wait();
            return testDonor.DonorId;
        }

        private static InputDonorWithExpandedHla BuildTestDonor(PhenotypeInfo<string> testDonorPhenotype)
        {
            var expandHlaPhenotypeService = DependencyInjection.DependencyInjection.Provider.GetService<IExpandHlaPhenotypeService>();

            var matchingHlaPhenotype = expandHlaPhenotypeService
                .GetPhenotypeOfExpandedHla(testDonorPhenotype)
                .Result;

            return new InputDonorWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithMatchingHla(matchingHlaPhenotype)
                .Build();
        }

        private PhenotypeInfo<string> GetPhenotypeWithDpb1HlaOf(string dpb1Hla)
        {
            var modifiedPhenotype = new PhenotypeInfo<string>(defaultPhenotype);
            modifiedPhenotype.SetAtLocus(Locus.Dpb1, dpb1Hla);

            return modifiedPhenotype;
        }

        private async Task<SearchResult> RunSixOutOfSixSearchWithPatientPhenotypeOf(PhenotypeInfo<string> patientPhenotype)
        {
            var searchRequest = new SearchRequestFromHlasBuilder(patientPhenotype)
                .SixOutOfSix()
                .Build();

            var searchResults = await searchService.Search(searchRequest);
            return searchResults.SingleOrDefault(d => d.DonorId == testDonorId);
        }
    }
}
