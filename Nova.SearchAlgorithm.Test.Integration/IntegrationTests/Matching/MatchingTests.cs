using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using NUnit.Framework;
// ReSharper disable InconsistentNaming

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests.Matching
{
    public class MatchingTests : IntegrationTestBase
    {
        private IDonorMatchingService matchingService;
        private InputDonor cordDonorWithFullHomozygousMatchAtLocusA;
        private InputDonor cordDonorWithFullHeterozygousMatchAtLocusA;
        private InputDonor cordDonorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA;
        private InputDonor cordDonorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA;
        private InputDonor cordDonorWithNoMatchAtLocusAAndExactMatchAtB;
        private InputDonor cordDonorWithNoMatchAtLocusAAndHalfMatchAtB;

        // Registries chosen to be different from `DefaultRegistryCode`
        private InputDonor cordDonorWithFullMatchAtAnthonyNolanRegistry;
        private InputDonor cordDonorWithFullMatchAtNmdpRegistry;
        
        private InputDonor adultDonorWithFullMatch;

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
            matchingService = Container.Resolve<IDonorMatchingService>();
        }
        
        [OneTimeSetUp]
        public void ImportTestDonors()
        {
            var importRepo = Container.Resolve<IDonorImportRepository>();

            cordDonorWithFullHomozygousMatchAtLocusA = GetDefaultInputDonorBuilder().Build();

            cordDonorWithFullHeterozygousMatchAtLocusA = GetDefaultInputDonorBuilder()
                .WithHlaAtLocus(
                    Locus.A,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusA_BothPositions, "non-matching-pgroup"}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusA_PositionTwo, "non-matching-pgroup"}}
                )
                .Build();

            cordDonorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA = GetDefaultInputDonorBuilder()
                .WithHlaAtLocus(
                    Locus.A,
                    new ExpandedHla { PGroups = new List<string> { PatientPGroup_LocusA_BothPositions, "non-matching-pgroup" } },
                    new ExpandedHla { PGroups = new List<string> { "non-matching-pgroup", "non-matching-pgroup-2" } }
                )
                .Build();
                

            cordDonorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA = GetDefaultInputDonorBuilder()
                .WithHlaAtLocus(
                    Locus.A,
                    new ExpandedHla { PGroups = new List<string> { PatientPGroup_LocusA_PositionOne, PatientPGroup_LocusA_PositionTwo } },
                    new ExpandedHla { PGroups = new List<string> { "non-matching-pgroup", "non-matching-pgroup-2" } }
                )
                .Build();

            cordDonorWithNoMatchAtLocusAAndExactMatchAtB = GetDefaultInputDonorBuilder()
                .WithHlaAtLocus(
                    Locus.A,
                    new ExpandedHla { PGroups = new List<string> { "non-matching-pgroup", "non-matching-pgroup-2" } },
                    new ExpandedHla { PGroups = new List<string> { "non-matching-pgroup-3", "non-matching-pgroup-4" } }
                )
                .WithHlaAtLocus(
                    Locus.B,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusB_PositionOne}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusB_PositionTwo}}
                )
                .Build();

            cordDonorWithNoMatchAtLocusAAndHalfMatchAtB = GetDefaultInputDonorBuilder()
                .WithHlaAtLocus(
                    Locus.A,
                    new ExpandedHla { PGroups = new List<string> { "non-matching-pgroup", "non-matching-pgroup-2" } },
                    new ExpandedHla { PGroups = new List<string> { "non-matching-pgroup-3", "non-matching-pgroup-4" } }
                )
                .WithHlaAtLocus(
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
            
            var allDonors = new List<InputDonor>
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
                Task.Run(() => importRepo.AddOrUpdateDonorWithHla(donor)).Wait();
            }
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtA_DoesNotMatchDonorWithNoMatchAtLocusAAndHalfMatchAtLocusB()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(2)
                .WithLocusMismatchCount(Locus.A, 2)
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == cordDonorWithNoMatchAtLocusAAndHalfMatchAtB.DonorId);
        }

        [Test]
        public async Task Search_WithThreeAllowedMismatchesTwoAtAAndOneAtB_MatchesDonorsWithExactMatchAtAAndB()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(3)
                .WithLocusMismatchCount(Locus.A, 2)
                .WithLocusMismatchCount(Locus.B, 1)
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithFullHeterozygousMatchAtLocusA.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithFullHomozygousMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task Search_WithThreeAllowedMismatchesTwoAtAAndOneAtB_MatchesDonorsWithSingleMatchAtAAndCompleteMatchAtB()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(3)
                .WithLocusMismatchCount(Locus.A, 2)
                .WithLocusMismatchCount(Locus.B, 1)
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA.DonorId);
        }

        [Test]
        public async Task Search_WithThreeAllowedMismatchesTwoAtAAndOneAtB_ReturnsDonorsWithNoMatchAtLocusA()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(3)
                .WithLocusMismatchCount(Locus.A, 2)
                .WithLocusMismatchCount(Locus.B, 1)
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithNoMatchAtLocusAAndExactMatchAtB.DonorId);
        }

        [Test]
        public async Task Search_WithThreeAllowedMismatchesTwoAtAAndOneAtB_MatchesDonorWithNoMatchAtLocusAAndHalfMatchAtLocusB()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(3)
                .WithLocusMismatchCount(Locus.A, 2)
                .WithLocusMismatchCount(Locus.B, 1)
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithNoMatchAtLocusAAndHalfMatchAtB.DonorId);
        }

        [Test]
        public async Task Search_ForMultipleSpecifiedRegistries_MatchesDonorsAtAllSpecifiedRegistries()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithRegistries(new List<RegistryCode> {RegistryCode.AN, RegistryCode.NMDP})
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithFullMatchAtAnthonyNolanRegistry.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithFullMatchAtNmdpRegistry.DonorId);
        }
        
        [Test]
        public async Task Search_DoesNotMatchDonorsAtUnspecifiedRegistries()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithRegistries(new List<RegistryCode> {DefaultRegistryCode})
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == cordDonorWithFullMatchAtNmdpRegistry.DonorId);
            results.Should().NotContain(d => d.Donor.DonorId == cordDonorWithFullMatchAtAnthonyNolanRegistry.DonorId);
        } 
        
        [Test]
        public async Task Search_ForAdultDonors_DoesNotMatchCordDonors()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Adult)
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == cordDonorWithFullHeterozygousMatchAtLocusA.DonorId);
            results.Should().NotContain(d => d.Donor.DonorId == cordDonorWithFullHomozygousMatchAtLocusA.DonorId);
        }
        
        [Test]
        public async Task Search_ForCordDonors_DoesNotMatchAdultDonors()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Cord)
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == adultDonorWithFullMatch.DonorId);
        }
        
        [Test]
        public async Task Search_ForCordDonors_MatchesCordDonors()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Cord)
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithFullHeterozygousMatchAtLocusA.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithFullHomozygousMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task Search_ForAdultDonors_DoesNotMatchDonorsWithFewerMismatchesThanSpecified()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Adult)
                .WithTotalMismatchCount(1)
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == adultDonorWithFullMatch.DonorId);
        }
        
        [Test]
        public async Task Search_ForCordDonors_MatchesDonorsWithFewerMismatchesThanSpecified()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Cord)
                .WithTotalMismatchCount(1)
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithFullHeterozygousMatchAtLocusA.DonorId);
        }
        
        /// <returns> An input donor builder pre-populated with default donor data of an exact match. </returns>
        private InputDonorBuilder GetDefaultInputDonorBuilder()
        {
            return new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithRegistryCode(DefaultRegistryCode)
                .WithDonorType(DefaultDonorType)
                .WithHlaAtLocus(
                    Locus.A,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusA_PositionOne}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusA_PositionTwo}}
                )
                .WithHlaAtLocus(
                    Locus.B,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusB_PositionOne}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusB_PositionTwo}}
                )
                .WithHlaAtLocus(
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
                .WithTotalMismatchCount(0);
        }
    }
}
