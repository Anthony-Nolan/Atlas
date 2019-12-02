using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Nova.SearchAlgorithm.Services.Search.Matching;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Data.Models;

// ReSharper disable InconsistentNaming

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests.Matching
{
    public class MatchingTests
    {
        private IDonorMatchingService matchingService;
        private InputDonorWithExpandedHla cordDonorWithFullHomozygousMatchAtLocusA;
        private InputDonorWithExpandedHla cordDonorWithFullHeterozygousMatchAtLocusA;
        private InputDonorWithExpandedHla cordDonorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA;
        private InputDonorWithExpandedHla cordDonorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA;
        private InputDonorWithExpandedHla cordDonorWithNoMatchAtLocusAAndExactMatchAtB;
        private InputDonorWithExpandedHla cordDonorWithNoMatchAtLocusAAndHalfMatchAtB;

        // Registries chosen to be different from `DefaultRegistryCode`
        private InputDonorWithExpandedHla cordDonorWithFullMatchAtAnthonyNolanRegistry;
        private InputDonorWithExpandedHla cordDonorWithFullMatchAtNmdpRegistry;

        private InputDonorWithExpandedHla adultDonorWithFullMatch;

        private InputDonorWithExpandedHla unavailableMatchingCordDonor;

        private const string PatientPGroup_LocusA_BothPositions = "01:01P";
        private const string PatientPGroup_LocusA_PositionOne = "01:02";
        private const string PatientPGroup_LocusA_PositionTwo = "02:01";
        private const string PatientPGroup_LocusB_PositionOne = "07:02P";
        private const string PatientPGroup_LocusB_PositionTwo = "08:01P";
        private const string PatientPGroup_LocusDRB1_PositionOne = "01:11P";
        private const string PatientPGroup_LocusDRB1_PositionTwo = "03:41P";

        private const RegistryCode DefaultRegistryCode = RegistryCode.DKMS;
        private const DonorType DefaultDonorType = DonorType.Cord;

        [SetUp]
        public void ResolveSearchRepo()
        {
            matchingService = DependencyInjection.DependencyInjection.Provider.GetService<IDonorMatchingService>();
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IActiveRepositoryFactory>();
            var donorUpdateRepository = repositoryFactory.GetDonorUpdateRepository();

            AddTestDonorsAvailableForSearch(donorUpdateRepository);
            AddTestDonorUnavailableForSearch(donorUpdateRepository);
        }

        private void AddTestDonorsAvailableForSearch(IDonorUpdateRepository donorUpdateRepository)
        {
            cordDonorWithFullHomozygousMatchAtLocusA = GetDefaultInputDonorBuilder().Build();

            cordDonorWithFullHeterozygousMatchAtLocusA = GetDefaultInputDonorBuilder()
                .WithMatchingHlaAtLocus(
                    Locus.A,
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusA_BothPositions, "non-matching-pgroup")
                        .Build(),
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusA_PositionTwo, "non-matching-pgroup")
                        .Build()
                )
                .Build();

            cordDonorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA = GetDefaultInputDonorBuilder()
                .WithMatchingHlaAtLocus(
                    Locus.A,
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusA_BothPositions, "non-matching-pgroup")
                        .Build(),
                    new ExpandedHlaBuilder()
                        .WithPGroups("non-matching-pgroup", "non-matching-pgroup-2")
                        .Build()
                )
                .Build();


            cordDonorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA = GetDefaultInputDonorBuilder()
                .WithMatchingHlaAtLocus(
                    Locus.A,
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusA_PositionOne, PatientPGroup_LocusA_PositionTwo)
                        .Build(),
                    new ExpandedHlaBuilder()
                        .WithPGroups("non-matching-pgroup", "non-matching-pgroup-2")
                        .Build()
                )
                .Build();

            cordDonorWithNoMatchAtLocusAAndExactMatchAtB = GetDefaultInputDonorBuilder()
                .WithMatchingHlaAtLocus(
                    Locus.A,
                    new ExpandedHlaBuilder()
                        .WithPGroups("non-matching-pgroup", "non-matching-pgroup-2")
                        .Build(),
                    new ExpandedHlaBuilder()
                        .WithPGroups("non-matching-pgroup-3", "non-matching-pgroup-4")
                        .Build()
                )
                .WithMatchingHlaAtLocus(
                    Locus.B,
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusB_PositionOne)
                        .Build(),
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusB_PositionTwo)
                        .Build()
                )
                .Build();

            cordDonorWithNoMatchAtLocusAAndHalfMatchAtB = GetDefaultInputDonorBuilder()
                .WithMatchingHlaAtLocus(
                    Locus.A,
                    new ExpandedHlaBuilder()
                        .WithPGroups("non-matching-pgroup", "non-matching-pgroup-2")
                        .Build(),
                    new ExpandedHlaBuilder()
                        .WithPGroups("non-matching-pgroup-3", "non-matching-pgroup-4")
                        .Build()
                )
                .WithMatchingHlaAtLocus(
                    Locus.B,
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusB_PositionOne)
                        .Build(),
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusA_PositionOne)
                        .Build()
                )
                .Build();

            cordDonorWithFullMatchAtAnthonyNolanRegistry = GetDefaultInputDonorBuilder()
                .WithRegistryCode(RegistryCode.AN)
                .Build();

            cordDonorWithFullMatchAtNmdpRegistry = GetDefaultInputDonorBuilder()
                .WithRegistryCode(RegistryCode.NMDP)
                .Build();

            adultDonorWithFullMatch = GetDefaultInputDonorBuilder()
                .WithDonorType(DonorType.Adult)
                .Build();

            var allDonors = new List<InputDonorWithExpandedHla>
            {
                cordDonorWithFullHomozygousMatchAtLocusA,
                cordDonorWithFullHeterozygousMatchAtLocusA,
                cordDonorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA,
                cordDonorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA,
                cordDonorWithNoMatchAtLocusAAndExactMatchAtB,
                cordDonorWithNoMatchAtLocusAAndHalfMatchAtB,
                cordDonorWithFullMatchAtAnthonyNolanRegistry,
                cordDonorWithFullMatchAtNmdpRegistry,
                adultDonorWithFullMatch
            };

            Task.Run(() => donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(allDonors)).Wait();
        }

        private void AddTestDonorUnavailableForSearch(IDonorUpdateRepository donorUpdateRepository)
        {
            unavailableMatchingCordDonor = GetDefaultInputDonorBuilder().Build();

            Task.Run(() =>
            {
                donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] { unavailableMatchingCordDonor });
                donorUpdateRepository.SetDonorBatchAsUnavailableForSearch(new[] { unavailableMatchingCordDonor.DonorId });
            }).Wait();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            DatabaseManager.ClearDatabases();
        }

        [Test]
        public async Task GetMatches_ForMultipleSpecifiedRegistries_MatchesDonorsAtAllSpecifiedRegistries()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithRegistries(new List<RegistryCode> { RegistryCode.AN, RegistryCode.NMDP })
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithFullMatchAtAnthonyNolanRegistry.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithFullMatchAtNmdpRegistry.DonorId);
        }

        [Test]
        public async Task GetMatches_DoesNotMatchDonorsAtUnspecifiedRegistries()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithRegistries(new List<RegistryCode> { DefaultRegistryCode })
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == cordDonorWithFullMatchAtNmdpRegistry.DonorId);
            results.Should().NotContain(d => d.Donor.DonorId == cordDonorWithFullMatchAtAnthonyNolanRegistry.DonorId);
        }

        [Test]
        public async Task GetMatches_ForAdultDonors_DoesNotMatchCordDonors()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Adult)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == cordDonorWithFullHeterozygousMatchAtLocusA.DonorId);
            results.Should().NotContain(d => d.Donor.DonorId == cordDonorWithFullHomozygousMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task GetMatches_ForCordDonors_DoesNotMatchAdultDonors()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Cord)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == adultDonorWithFullMatch.DonorId);
        }

        [Test]
        public async Task GetMatches_ForCordDonors_MatchesCordDonors()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Cord)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithFullHeterozygousMatchAtLocusA.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithFullHomozygousMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task GetMatches_ForAdultDonors_DoesNotMatchDonorsWithFewerMismatchesThanSpecified()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Adult)
                .WithDonorMismatchCount(1)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == adultDonorWithFullMatch.DonorId);
        }

        [Test]
        public async Task GetMatches_ForCordDonors_MatchesDonorsWithFewerMismatchesThanSpecified()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Cord)
                .WithDonorMismatchCount(1)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithFullHeterozygousMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task GetMatches_DoesNotReturnMatchingDonorThatIsUnavailableForSearch()
        {
            var searchCriteria = GetDefaultCriteriaBuilder().Build();

            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == unavailableMatchingCordDonor.DonorId);
        }

        /// <returns> An input donor builder pre-populated with default donor data of an exact match. </returns>
        private InputDonorWithExpandedHlaBuilder GetDefaultInputDonorBuilder()
        {
            return new InputDonorWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithRegistryCode(DefaultRegistryCode)
                .WithDonorType(DefaultDonorType)
                .WithMatchingHlaAtLocus(
                    Locus.A,
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusA_PositionOne)
                        .Build(),
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusA_PositionTwo)
                        .Build()
                )
                .WithMatchingHlaAtLocus(
                    Locus.B,
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusB_PositionOne)
                        .Build(),
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusB_PositionTwo)
                        .Build()
                )
                .WithMatchingHlaAtLocus(
                    Locus.Drb1,
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusDRB1_PositionOne)
                        .Build(),
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroup_LocusDRB1_PositionTwo)
                        .Build()
                );
        }

        /// <returns> A criteria builder pre-populated with default criteria data of an exact search. </returns>
        private static AlleleLevelMatchCriteriaBuilder GetDefaultCriteriaBuilder()
        {
            return new AlleleLevelMatchCriteriaBuilder()
                .WithSearchType(DefaultDonorType)
                .WithRegistries(new List<RegistryCode> { DefaultRegistryCode })
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
