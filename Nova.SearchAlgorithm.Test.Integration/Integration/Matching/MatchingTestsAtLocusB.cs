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
    public class MatchingTestsAtLocusB : IntegrationTestBase
    {
        private AlleleLevelMatchCriteria searchCriteria;

        private IDonorMatchingService matchingService;
        private InputDonor donorWithFullHomozygousMatchAtLocusB;
        private InputDonor donorWithFullExactHeterozygousMatchAtLocusB;
        private InputDonor donorWithFullCrossHeterozygousMatchAtLocusB;
        private InputDonor donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusB;
        private InputDonor donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusB;
        private InputDonor donorWithNoMatchAtLocusB;
        
        private readonly List<string> pGroupsAtA = new List<string> { "01:02P" };
        private readonly List<string> pGroupsAtDrb = new List<string> { "01:11P" };

        private const string PatientPGroupAtBothPositions = "07:136P";
        private const string PatientPGroupAtPositionOne = "35:113";
        private const string PatientPGroupAtPositionTwo = "44:20";

        public MatchingTestsAtLocusB(DonorStorageImplementation param) : base(param) { }

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
            donorWithFullHomozygousMatchAtLocusB = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 1,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = pGroupsAtA },
                    A_2 = new ExpandedHla { PGroups = pGroupsAtA },
                    B_1 = new ExpandedHla { PGroups = new List<string>{PatientPGroupAtBothPositions} },
                    B_2 = new ExpandedHla { PGroups = new List<string>{PatientPGroupAtBothPositions} },
                    DRB1_1 = new ExpandedHla { PGroups = pGroupsAtDrb },
                    DRB1_2 = new ExpandedHla { PGroups = pGroupsAtDrb }
                }
            };

            importRepo.AddOrUpdateDonorWithHla(donorWithFullHomozygousMatchAtLocusB).Wait();

            donorWithFullExactHeterozygousMatchAtLocusB = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 2,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = pGroupsAtA },
                    A_2 = new ExpandedHla { PGroups = pGroupsAtA },
                    B_1 = new ExpandedHla { PGroups = new List<string>{PatientPGroupAtPositionOne} },
                    B_2 = new ExpandedHla { PGroups = new List<string>{PatientPGroupAtPositionTwo} },
                    DRB1_1 = new ExpandedHla { PGroups = pGroupsAtDrb },
                    DRB1_2 = new ExpandedHla { PGroups = pGroupsAtDrb }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithFullExactHeterozygousMatchAtLocusB).Wait();
            
            donorWithFullCrossHeterozygousMatchAtLocusB = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 3,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = pGroupsAtA },
                    A_2 = new ExpandedHla { PGroups = pGroupsAtA },
                    B_1 = new ExpandedHla { PGroups = new List<string>{PatientPGroupAtPositionTwo} },
                    B_2 = new ExpandedHla { PGroups = new List<string>{PatientPGroupAtPositionOne} },
                    DRB1_1 = new ExpandedHla { PGroups = pGroupsAtDrb },
                    DRB1_2 = new ExpandedHla { PGroups = pGroupsAtDrb }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithFullCrossHeterozygousMatchAtLocusB).Wait();

            donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusB = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 4,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = pGroupsAtA },
                    A_2 = new ExpandedHla { PGroups = pGroupsAtA },
                    B_1 = new ExpandedHla { PGroups = new List<string>{PatientPGroupAtBothPositions} },
                    B_2 = new ExpandedHla { PGroups = new List<string>{"18:126"} },
                    DRB1_1 = new ExpandedHla { PGroups = pGroupsAtDrb },
                    DRB1_2 = new ExpandedHla { PGroups = pGroupsAtDrb }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusB).Wait();

            donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusB = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 5,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = pGroupsAtA },
                    A_2 = new ExpandedHla { PGroups = pGroupsAtA },
                    B_1 = new ExpandedHla { PGroups = new List<string>{PatientPGroupAtPositionOne, PatientPGroupAtPositionTwo} },
                    B_2 = new ExpandedHla { PGroups = new List<string>{"18:126"} },
                    DRB1_1 = new ExpandedHla { PGroups = pGroupsAtDrb },
                    DRB1_2 = new ExpandedHla { PGroups = pGroupsAtDrb }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusB).Wait();

            donorWithNoMatchAtLocusB = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 6,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = pGroupsAtA },
                    A_2 = new ExpandedHla { PGroups = pGroupsAtA },
                    B_1 = new ExpandedHla { PGroups = new List<string>{"19:143"} },
                    B_2 = new ExpandedHla { PGroups = new List<string>{"18:126"} },
                    DRB1_1 = new ExpandedHla { PGroups = pGroupsAtDrb },
                    DRB1_2 = new ExpandedHla { PGroups = pGroupsAtDrb }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithNoMatchAtLocusB).Wait();
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
                    HlaNamesToMatchInPositionOne = new List<string> { PatientPGroupAtBothPositions, PatientPGroupAtPositionOne },
                    HlaNamesToMatchInPositionTwo = new List<string> { PatientPGroupAtBothPositions, PatientPGroupAtPositionTwo }
                },
                LocusMismatchDRB1 = new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    HlaNamesToMatchInPositionOne = pGroupsAtDrb,
                    HlaNamesToMatchInPositionTwo = pGroupsAtDrb
                }
            };
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ClearDatabase();
        }
        
        [Test]
        public async Task Search_WithNoAllowedMismatches_MatchesDonorWithTwoOfTwoHomozygousMatchesAtLocusB()
        {
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHomozygousMatchAtLocusB.DonorId);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_MatchesDonorWithTwoOfTwoHeterozygousMatchesAtLocusB()
        {
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullExactHeterozygousMatchAtLocusB.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullCrossHeterozygousMatchAtLocusB.DonorId);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_DoesNotMatchDonorsWithFewerThanTwoMatchesAtLocusB()
        {
            var results = await matchingService.Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusB.DonorId);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusB.DonorId);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithNoMatchAtLocusB.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtB_MatchesDonorsWithTwoMatches()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchB.MismatchCount = 1;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullExactHeterozygousMatchAtLocusB.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHomozygousMatchAtLocusB.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtB_MatchesDonorWithOneOfTwoHvGAtLocusB()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchB.MismatchCount = 1;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusB.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtB_MatchesDonorWithOneOfTwoBothDirectionsAtLocusB()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchB.MismatchCount = 1;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusB.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtB_DoesNotMatchDonorsWithNoMatchAtLocusB()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchB.MismatchCount = 1;
            var results = await matchingService.Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithNoMatchAtLocusB.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtB_MatchesDonorWithNoMatchAtLocusB()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchB.MismatchCount = 2;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithNoMatchAtLocusB.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtB_MatchesDonorsWithExactMatchAtLocusB()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchB.MismatchCount = 2;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullExactHeterozygousMatchAtLocusB.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHomozygousMatchAtLocusB.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtB_MatchesDonorsWithSingleMatchAtLocusB()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchB.MismatchCount = 2;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusB.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusB.DonorId);
        }
    }
}
