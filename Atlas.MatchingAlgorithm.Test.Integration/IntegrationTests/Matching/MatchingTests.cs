using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Search.Matching;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.Test.TestHelpers;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Matching
{
    public class MatchingTests
    {
        private IMatchingService matchingService;

        private DonorInfoWithExpandedHla cordDonorInfoWithFullHomozygousMatchAtLocusA;
        private DonorInfoWithExpandedHla cordDonorInfoWithFullHeterozygousMatchAtLocusA;
        private DonorInfoWithExpandedHla cordDonorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA;
        private DonorInfoWithExpandedHla cordDonorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA;
        private DonorInfoWithExpandedHla cordDonorInfoWithNoMatchAtLocusAAndExactMatchAtB;
        private DonorInfoWithExpandedHla cordDonorInfoWithNoMatchAtLocusAAndHalfMatchAtB;

        private DonorInfoWithExpandedHla adultDonorInfoWithFullMatch;

        private DonorInfoWithExpandedHla unavailableMatchingCordDonor;

        private const string PatientPGroup_LocusA_BothPositions = "01:01P";
        private const string PatientPGroup_LocusA_PositionOne = "01:02";
        private const string PatientPGroup_LocusA_PositionTwo = "02:01";
        private const string PatientPGroup_LocusB_PositionOne = "07:02P";
        private const string PatientPGroup_LocusB_PositionTwo = "08:01P";
        private const string PatientPGroup_LocusDRB1_PositionOne = "01:11P";
        private const string PatientPGroup_LocusDRB1_PositionTwo = "03:41P";

        private const DonorType DefaultDonorType = DonorType.Cord;

        [SetUp]
        public void ResolveSearchRepo()
        {
            matchingService = DependencyInjection.DependencyInjection.Provider.GetService<IMatchingService>();
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IActiveRepositoryFactory>();
                var donorUpdateRepository = repositoryFactory.GetDonorUpdateRepository();

                AddTestDonorsAvailableForSearch(donorUpdateRepository);
                AddTestDonorUnavailableForSearch(donorUpdateRepository);
            });
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearTransientDatabases);
        }

        private void AddTestDonorsAvailableForSearch(IDonorUpdateRepository donorUpdateRepository)
        {
            cordDonorInfoWithFullHomozygousMatchAtLocusA = GetDefaultDonorBuilder().Build();

            cordDonorInfoWithFullHeterozygousMatchAtLocusA = GetDefaultDonorBuilder()
                .WithHlaAtLocus(
                    Locus.A,
                    new TestHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusA_BothPositions, "non-matching-pgroup")
                        .Build(),
                    new TestHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusA_PositionTwo, "non-matching-pgroup")
                        .Build()
                )
                .Build();

            cordDonorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA = GetDefaultDonorBuilder()
                .WithHlaAtLocus(
                    Locus.A,
                    new TestHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusA_BothPositions, "non-matching-pgroup")
                        .Build(),
                    new TestHlaBuilder()
                        .WithPGroups("non-matching-pgroup", "non-matching-pgroup-2")
                        .Build()
                )
                .Build();


            cordDonorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA = GetDefaultDonorBuilder()
                .WithHlaAtLocus(
                    Locus.A,
                    new TestHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusA_PositionOne, PatientPGroup_LocusA_PositionTwo)
                        .Build(),
                    new TestHlaBuilder()
                        .WithPGroups("non-matching-pgroup", "non-matching-pgroup-2")
                        .Build()
                )
                .Build();

            cordDonorInfoWithNoMatchAtLocusAAndExactMatchAtB = GetDefaultDonorBuilder()
                .WithHlaAtLocus(
                    Locus.A,
                    new TestHlaBuilder()
                        .WithPGroups("non-matching-pgroup", "non-matching-pgroup-2")
                        .Build(),
                    new TestHlaBuilder()
                        .WithPGroups("non-matching-pgroup-3", "non-matching-pgroup-4")
                        .Build()
                )
                .WithHlaAtLocus(
                    Locus.B,
                    new TestHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusB_PositionOne)
                        .Build(),
                    new TestHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusB_PositionTwo)
                        .Build()
                )
                .Build();

            cordDonorInfoWithNoMatchAtLocusAAndHalfMatchAtB = GetDefaultDonorBuilder()
                .WithHlaAtLocus(
                    Locus.A,
                    new TestHlaBuilder()
                        .WithPGroups("non-matching-pgroup", "non-matching-pgroup-2")
                        .Build(),
                    new TestHlaBuilder()
                        .WithPGroups("non-matching-pgroup-3", "non-matching-pgroup-4")
                        .Build()
                )
                .WithHlaAtLocus(
                    Locus.B,
                    new TestHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusB_PositionOne)
                        .Build(),
                    new TestHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusA_PositionOne)
                        .Build()
                )
                .Build();

            adultDonorInfoWithFullMatch = GetDefaultDonorBuilder()
                .WithDonorType(DonorType.Adult)
                .Build();

            var allDonors = new List<DonorInfoWithExpandedHla>
            {
                cordDonorInfoWithFullHomozygousMatchAtLocusA,
                cordDonorInfoWithFullHeterozygousMatchAtLocusA,
                cordDonorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA,
                cordDonorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA,
                cordDonorInfoWithNoMatchAtLocusAAndExactMatchAtB,
                cordDonorInfoWithNoMatchAtLocusAAndHalfMatchAtB,
                adultDonorInfoWithFullMatch
            };

            Task.Run(() => donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(allDonors, false)).Wait();
        }

        private void AddTestDonorUnavailableForSearch(IDonorUpdateRepository donorUpdateRepository)
        {
            unavailableMatchingCordDonor = GetDefaultDonorBuilder().Build();

            Task.Run(() =>
            {
                donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] { unavailableMatchingCordDonor }, false);
                donorUpdateRepository.SetDonorBatchAsUnavailableForSearch(new[] { unavailableMatchingCordDonor.DonorId });
            }).Wait();
        }

        [Test]
        public async Task GetMatches_ForAdultDonors_DoesNotMatchCordDonors()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Adult)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().NotContain(d => d.DonorInfo.DonorId == cordDonorInfoWithFullHeterozygousMatchAtLocusA.DonorId);
            results.Should().NotContain(d => d.DonorInfo.DonorId == cordDonorInfoWithFullHomozygousMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task GetMatches_ForCordDonors_DoesNotMatchAdultDonors()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Cord)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().NotContain(d => d.DonorInfo.DonorId == adultDonorInfoWithFullMatch.DonorId);
        }

        [Test]
        public async Task GetMatches_ForCordDonors_MatchesCordDonors()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Cord)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.ShouldContainDonor(cordDonorInfoWithFullHeterozygousMatchAtLocusA.DonorId);
            results.ShouldContainDonor(cordDonorInfoWithFullHomozygousMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task GetMatches_ForAdultDonors_DoesNotMatchDonorsWithFewerMismatchesThanSpecified()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Adult)
                .WithDonorMismatchCount(1)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().NotContain(d => d.DonorInfo.DonorId == adultDonorInfoWithFullMatch.DonorId);
        }

        [Test]
        public async Task GetMatches_ForCordDonors_MatchesDonorsWithFewerMismatchesThanSpecified()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Cord)
                .WithDonorMismatchCount(1)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.ShouldContainDonor(cordDonorInfoWithFullHeterozygousMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task GetMatches_DoesNotReturnMatchingDonorThatIsUnavailableForSearch()
        {
            var searchCriteria = GetDefaultCriteriaBuilder().Build();

            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().NotContain(d => d.DonorInfo.DonorId == unavailableMatchingCordDonor.DonorId);
        }

        /// <returns> An input donor builder pre-populated with default donor data of an exact match. </returns>
        private static DonorInfoWithTestHlaBuilder GetDefaultDonorBuilder()
        {
            return new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                .WithDonorType(DefaultDonorType)
                .WithHlaAtLocus(
                    Locus.A,
                    new TestHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusA_PositionOne)
                        .Build(),
                    new TestHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusA_PositionTwo)
                        .Build()
                )
                .WithHlaAtLocus(
                    Locus.B,
                    new TestHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusB_PositionOne)
                        .Build(),
                    new TestHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusB_PositionTwo)
                        .Build()
                )
                .WithHlaAtLocus(
                    Locus.Drb1,
                    new TestHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusDRB1_PositionOne)
                        .Build(),
                    new TestHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusDRB1_PositionTwo)
                        .Build()
                );
        }

        /// <returns> A criteria builder pre-populated with default criteria data of an exact search. </returns>
        private static AlleleLevelMatchCriteriaBuilder GetDefaultCriteriaBuilder()
        {
            return new AlleleLevelMatchCriteriaBuilder()
                .WithSearchType(DefaultDonorType)
                .WithDonorMismatchCount(0)
                .WithLocusMatchCriteria(Locus.A, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    PGroupsToMatchInPositionOne = new[] { PatientPGroup_LocusA_BothPositions, PatientPGroup_LocusA_PositionOne },
                    PGroupsToMatchInPositionTwo = new[] { PatientPGroup_LocusA_BothPositions, PatientPGroup_LocusA_PositionTwo }
                })
                .WithLocusMatchCriteria(Locus.B, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    PGroupsToMatchInPositionOne = new[] { PatientPGroup_LocusB_PositionOne },
                    PGroupsToMatchInPositionTwo = new[] { PatientPGroup_LocusB_PositionTwo }
                })
                .WithLocusMatchCriteria(Locus.Drb1, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    PGroupsToMatchInPositionOne = new[] { PatientPGroup_LocusDRB1_PositionOne },
                    PGroupsToMatchInPositionTwo = new[] { PatientPGroup_LocusDRB1_PositionTwo }
                })
                .WithDonorMismatchCount(0);
        }
    }
}
