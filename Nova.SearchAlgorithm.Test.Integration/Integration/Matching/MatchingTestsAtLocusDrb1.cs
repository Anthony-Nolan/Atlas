using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Services.Matching;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Integration.Integration.Matching
{
    public class MatchingTestsAtLocusDrb1 : IntegrationTestBase
    {
        private AlleleLevelMatchCriteria searchCriteria;

        private IDonorMatchingService matchingService;
        private InputDonor donorWithFullHomozygousMatchAtLocusDrb1;
        private InputDonor donorWithFullExactHeterozygousMatchAtLocusDrb1;
        private InputDonor donorWithFullCrossHeterozygousMatchAtLocusDrb1;
        private InputDonor donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusDrb1;
        private InputDonor donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusDrb1;
        private InputDonor donorWithNoMatchAtLocusDrb1;
        
        private readonly List<string> pGroupsAtA = new List<string> { "01:02P" };
        private readonly List<string> pGroupsAtB = new List<string> { "07:02P" };

        private const string PatientPGroupAtBothPositions = "14:01P";
        private const string PatientPGroupAtPositionOne = "03:18";
        private const string PatientPGroupAtPositionTwo = "13:31";

        public MatchingTestsAtLocusDrb1(DonorStorageImplementation param) : base(param) { }

        [SetUp]
        public void ResolveSearchRepo()
        {
            matchingService = container.Resolve<IDonorMatchingService>();
        }
        
        /// <summary>
        /// Set up test data. This test suite is only testing variations on Locus B - the other required loci should be assumed to always match
        /// </summary>
        [OneTimeSetUp]
        public void ImportTestDonors()
        {
            var importRepo = container.Resolve<IDonorImportRepository>();
            donorWithFullHomozygousMatchAtLocusDrb1 = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 1,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = pGroupsAtA },
                    A_2 = new ExpandedHla { PGroups = pGroupsAtA },
                    B_1 = new ExpandedHla { PGroups = pGroupsAtB },
                    B_2 = new ExpandedHla { PGroups = pGroupsAtB },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { PatientPGroupAtBothPositions, PatientPGroupAtPositionOne } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { PatientPGroupAtBothPositions, "01:01P" } }
                }
            };

            importRepo.AddOrUpdateDonorWithHla(donorWithFullHomozygousMatchAtLocusDrb1).Wait();

            donorWithFullExactHeterozygousMatchAtLocusDrb1 = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 2,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = pGroupsAtA },
                    A_2 = new ExpandedHla { PGroups = pGroupsAtA },
                    B_1 = new ExpandedHla { PGroups = pGroupsAtB },
                    B_2 = new ExpandedHla { PGroups = pGroupsAtB },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { PatientPGroupAtPositionOne } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { PatientPGroupAtPositionTwo } }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithFullExactHeterozygousMatchAtLocusDrb1).Wait();
            
            donorWithFullCrossHeterozygousMatchAtLocusDrb1 = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 3,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = pGroupsAtA },
                    A_2 = new ExpandedHla { PGroups = pGroupsAtA },
                    B_1 = new ExpandedHla { PGroups = pGroupsAtB },
                    B_2 = new ExpandedHla { PGroups = pGroupsAtB },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { PatientPGroupAtPositionTwo } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { PatientPGroupAtPositionOne } }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithFullCrossHeterozygousMatchAtLocusDrb1).Wait();

            donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusDrb1 = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 4,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = pGroupsAtA },
                    A_2 = new ExpandedHla { PGroups = pGroupsAtA },
                    B_1 = new ExpandedHla { PGroups = pGroupsAtB },
                    B_2 = new ExpandedHla { PGroups = pGroupsAtB },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { PatientPGroupAtBothPositions } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "01:02P" } }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusDrb1).Wait();

            donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusDrb1 = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 5,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = pGroupsAtA },
                    A_2 = new ExpandedHla { PGroups = pGroupsAtA },
                    B_1 = new ExpandedHla { PGroups = pGroupsAtB },
                    B_2 = new ExpandedHla { PGroups = pGroupsAtB },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { PatientPGroupAtPositionOne, PatientPGroupAtPositionTwo} },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "01:17" } }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusDrb1).Wait();

            donorWithNoMatchAtLocusDrb1 = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 6,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = pGroupsAtA },
                    A_2 = new ExpandedHla { PGroups = pGroupsAtA },
                    B_1 = new ExpandedHla { PGroups = pGroupsAtB },
                    B_2 = new ExpandedHla { PGroups = pGroupsAtB },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { "01:23", "01:06P" } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "01:99", "17:99P" } }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithNoMatchAtLocusDrb1).Wait();
        }

        [SetUp]
        public void ResetSearchCriteria()
        {
            searchCriteria = new AlleleLevelMatchCriteria
            {
                SearchType = DonorType.Adult,
                RegistriesToSearch = new List<RegistryCode> { RegistryCode.AN },
                DonorMismatchCount = 0,
                LocusMismatchA = new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    HlaNamesToMatchInPositionOne = pGroupsAtA,
                    HlaNamesToMatchInPositionTwo = pGroupsAtA
                },
                LocusMismatchB = new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    HlaNamesToMatchInPositionOne = pGroupsAtB,
                    HlaNamesToMatchInPositionTwo = pGroupsAtB
                },
                LocusMismatchDRB1 = new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    HlaNamesToMatchInPositionOne = new List<string> { PatientPGroupAtBothPositions, PatientPGroupAtPositionOne },
                    HlaNamesToMatchInPositionTwo = new List<string> { PatientPGroupAtBothPositions, PatientPGroupAtPositionTwo }
                }
            };
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ClearDatabase();
        }
        
        [Test]
        public async Task Search_WithNoAllowedMismatches_MatchesDonorWithTwoOfTwoHomozygousMatchesAtLocusDrb1()
        {
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHomozygousMatchAtLocusDrb1.DonorId);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_MatchesDonorWithTwoOfTwoHeterozygousMatchesAtLocusDrb1()
        {
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullExactHeterozygousMatchAtLocusDrb1.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullCrossHeterozygousMatchAtLocusDrb1.DonorId);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_DoesNotMatchDonorsWithFewerThanTwoMatchesAtA()
        {
            var results = await matchingService.Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusDrb1.DonorId);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusDrb1.DonorId);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithNoMatchAtLocusDrb1.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtDrb1_MatchesDonorsWithTwoMatches()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchDRB1.MismatchCount = 1;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullExactHeterozygousMatchAtLocusDrb1.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHomozygousMatchAtLocusDrb1.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtDrb1_MatchesDonorWithOneOfTwoHvGAtLocusDrb1()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchDRB1.MismatchCount = 1;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusDrb1.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtDrb1_MatchesDonorWithOneOfTwoBothDirectionsAtLocusDrb1()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchDRB1.MismatchCount = 1;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusDrb1.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtDrb1_DoesNotMatchDonorsWithNoMatchAtLocusDrb1()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchDRB1.MismatchCount = 1;
            var results = await matchingService.Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithNoMatchAtLocusDrb1.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtDrb1_MatchesDonorWithNoMatchAtLocusDrb1()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchDRB1.MismatchCount = 2;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithNoMatchAtLocusDrb1.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtDrb1_MatchesDonorsWithExactMatchAtLocusDrb1()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchDRB1.MismatchCount = 2;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullExactHeterozygousMatchAtLocusDrb1.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHomozygousMatchAtLocusDrb1.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtDrb1_MatchesDonorsWithSingleMatchAtLocusDrb1()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchDRB1.MismatchCount = 2;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusDrb1.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusDrb1.DonorId);
        }
    }
}
