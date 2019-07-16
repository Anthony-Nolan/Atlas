using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using NUnit.Framework;

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
        public void ImportTestDonors()
        {
            var donorRepo = DependencyInjection.DependencyInjection.Provider.GetService<IDonorUpdateRepository>();

            cordDonorWithFullHomozygousMatchAtLocusA = GetDefaultInputDonorBuilder().Build();

            cordDonorWithFullHeterozygousMatchAtLocusA = GetDefaultInputDonorBuilder()
                .WithMatchingHlaAtLocus(
                    Locus.A,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusA_BothPositions, "non-matching-pgroup"}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusA_PositionTwo, "non-matching-pgroup"}}
                )
                .Build();

            cordDonorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA = GetDefaultInputDonorBuilder()
                .WithMatchingHlaAtLocus(
                    Locus.A,
                    new ExpandedHla { PGroups = new List<string> { PatientPGroup_LocusA_BothPositions, "non-matching-pgroup" } },
                    new ExpandedHla { PGroups = new List<string> { "non-matching-pgroup", "non-matching-pgroup-2" } }
                )
                .Build();
                

            cordDonorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA = GetDefaultInputDonorBuilder()
                .WithMatchingHlaAtLocus(
                    Locus.A,
                    new ExpandedHla { PGroups = new List<string> { PatientPGroup_LocusA_PositionOne, PatientPGroup_LocusA_PositionTwo } },
                    new ExpandedHla { PGroups = new List<string> { "non-matching-pgroup", "non-matching-pgroup-2" } }
                )
                .Build();

            cordDonorWithNoMatchAtLocusAAndExactMatchAtB = GetDefaultInputDonorBuilder()
                .WithMatchingHlaAtLocus(
                    Locus.A,
                    new ExpandedHla { PGroups = new List<string> { "non-matching-pgroup", "non-matching-pgroup-2" } },
                    new ExpandedHla { PGroups = new List<string> { "non-matching-pgroup-3", "non-matching-pgroup-4" } }
                )
                .WithMatchingHlaAtLocus(
                    Locus.B,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusB_PositionOne}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusB_PositionTwo}}
                )
                .Build();

            cordDonorWithNoMatchAtLocusAAndHalfMatchAtB = GetDefaultInputDonorBuilder()
                .WithMatchingHlaAtLocus(
                    Locus.A,
                    new ExpandedHla { PGroups = new List<string> { "non-matching-pgroup", "non-matching-pgroup-2" } },
                    new ExpandedHla { PGroups = new List<string> { "non-matching-pgroup-3", "non-matching-pgroup-4" } }
                )
                .WithMatchingHlaAtLocus(
                    Locus.B,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusB_PositionOne}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusA_PositionOne}}
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

            foreach (var donor in allDonors)
            {
                Task.Run(() => donorRepo.InsertDonorWithExpandedHla(donor)).Wait();
            }
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            DatabaseManager.ClearDatabase();
        }
        
        [Test]
        public async Task Search_ForMultipleSpecifiedRegistries_MatchesDonorsAtAllSpecifiedRegistries()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithRegistries(new List<RegistryCode> {RegistryCode.AN, RegistryCode.NMDP})
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithFullMatchAtAnthonyNolanRegistry.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithFullMatchAtNmdpRegistry.DonorId);
        }
        
        [Test]
        public async Task Search_DoesNotMatchDonorsAtUnspecifiedRegistries()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithRegistries(new List<RegistryCode> {DefaultRegistryCode})
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == cordDonorWithFullMatchAtNmdpRegistry.DonorId);
            results.Should().NotContain(d => d.Donor.DonorId == cordDonorWithFullMatchAtAnthonyNolanRegistry.DonorId);
        } 
        
        [Test]
        public async Task Search_ForAdultDonors_DoesNotMatchCordDonors()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Adult)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == cordDonorWithFullHeterozygousMatchAtLocusA.DonorId);
            results.Should().NotContain(d => d.Donor.DonorId == cordDonorWithFullHomozygousMatchAtLocusA.DonorId);
        }
        
        [Test]
        public async Task Search_ForCordDonors_DoesNotMatchAdultDonors()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Cord)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == adultDonorWithFullMatch.DonorId);
        }
        
        [Test]
        public async Task Search_ForCordDonors_MatchesCordDonors()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Cord)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithFullHeterozygousMatchAtLocusA.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithFullHomozygousMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task Search_ForAdultDonors_DoesNotMatchDonorsWithFewerMismatchesThanSpecified()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Adult)
                .WithDonorMismatchCount(1)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == adultDonorWithFullMatch.DonorId);
        }
        
        [Test]
        public async Task Search_ForCordDonors_MatchesDonorsWithFewerMismatchesThanSpecified()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Cord)
                .WithDonorMismatchCount(1)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithFullHeterozygousMatchAtLocusA.DonorId);
        }
        
        /// <returns> An input donor builder pre-populated with default donor data of an exact match. </returns>
        private InputDonorWithExpandedHlaBuilder GetDefaultInputDonorBuilder()
        {
            return new InputDonorWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithRegistryCode(DefaultRegistryCode)
                .WithDonorType(DefaultDonorType)
                .WithMatchingHlaAtLocus(
                    Locus.A,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusA_PositionOne}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusA_PositionTwo}}
                )
                .WithMatchingHlaAtLocus(
                    Locus.B,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusB_PositionOne}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusB_PositionTwo}}
                )
                .WithMatchingHlaAtLocus(
                    Locus.Drb1,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusDRB1_PositionOne}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusDRB1_PositionTwo}}
                );
        }
        
        /// <returns> A criteria builder pre-populated with default criteria data of an exact search. </returns>
        private static AlleleLevelMatchCriteriaBuilder GetDefaultCriteriaBuilder()
        {
            return new AlleleLevelMatchCriteriaBuilder()
                .WithSearchType(DefaultDonorType)
                .WithRegistries(new List<RegistryCode>{DefaultRegistryCode})
                .WithDonorMismatchCount(0)
                .WithLocusMatchCriteria(Locus.A, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    PGroupsToMatchInPositionOne = new List<string> { PatientPGroup_LocusA_BothPositions, PatientPGroup_LocusA_PositionOne },
                    PGroupsToMatchInPositionTwo = new List<string> { PatientPGroup_LocusA_BothPositions, PatientPGroup_LocusA_PositionTwo }
                })
                .WithLocusMatchCriteria(Locus.B, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    PGroupsToMatchInPositionOne = new List<string> { PatientPGroup_LocusB_PositionOne },
                    PGroupsToMatchInPositionTwo = new List<string> { PatientPGroup_LocusB_PositionTwo }
                })
                .WithLocusMatchCriteria(Locus.Drb1, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    PGroupsToMatchInPositionOne = new List<string> { PatientPGroup_LocusDRB1_PositionOne },
                    PGroupsToMatchInPositionTwo = new List<string> { PatientPGroup_LocusDRB1_PositionTwo }
                })
                .WithDonorMismatchCount(0);
        }
    }
}
