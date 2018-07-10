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
    public class MatchingTests : IntegrationTestBase
    {
        private AlleleLevelMatchCriteria searchCriteria;

        private IDonorMatchingService matchingService;
        private InputDonor donorWithFullHomozygousMatchAtLocusA;
        private InputDonor donorWithFullHeterozygousMatchAtLocusA;
        private InputDonor donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA;
        private InputDonor donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA;
        private InputDonor donorWithNoMatchAtLocusAAndExactMatchAtB;
        private InputDonor donorWithNoMatchAtLocusAAndHalfMatchAtB;

        public MatchingTests(DonorStorageImplementation param) : base(param) { }

        [SetUp]
        public void ResolveSearchRepo()
        {
            matchingService = container.Resolve<IDonorMatchingService>();
        }
        
        [OneTimeSetUp]
        public void ImportTestDonors()
        {
            var importRepo = container.Resolve<IDonorImportRepository>();

            donorWithFullHomozygousMatchAtLocusA = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = DonorIdGenerator.NextId(),
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { "01:01P", "01:02" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "01:01P", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    B_2 = new ExpandedHla { PGroups = new List<string> { "08:01P" } },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { "01:11P" } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "03:41P" } }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithFullHomozygousMatchAtLocusA).Wait();

            donorWithFullHeterozygousMatchAtLocusA = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = DonorIdGenerator.NextId(),
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { "01:01P", "30:02P" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "02:01", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    B_2 = new ExpandedHla { PGroups = new List<string> { "08:01P" } },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { "01:11P" } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "03:41P" } }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithFullHeterozygousMatchAtLocusA).Wait();

            donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = DonorIdGenerator.NextId(),
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { "01:01P", "30:02P" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "26:04", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    B_2 = new ExpandedHla { PGroups = new List<string> { "08:01P" } },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { "01:11P" } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "03:41P" } }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA).Wait();

            donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = DonorIdGenerator.NextId(),
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { "01:02", "02:01" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "26:04", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    B_2 = new ExpandedHla { PGroups = new List<string> { "08:01P" } },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { "01:11P" } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "03:41P" } }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA).Wait();

            donorWithNoMatchAtLocusAAndExactMatchAtB = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = DonorIdGenerator.NextId(),
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { "11:59", "30:02P" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "26:04", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    B_2 = new ExpandedHla { PGroups = new List<string> { "08:01P" } },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { "01:11P" } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "03:41P" } }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithNoMatchAtLocusAAndExactMatchAtB).Wait();

            donorWithNoMatchAtLocusAAndHalfMatchAtB = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = DonorIdGenerator.NextId(),
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { "11:59", "30:02P" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "11:59", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    B_2 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { "01:11P" } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "03:41P" } }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithNoMatchAtLocusAAndHalfMatchAtB).Wait();
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
                    PGroupsToMatchInPositionOne = new List<string> { "01:01P", "01:02" },
                    PGroupsToMatchInPositionTwo = new List<string> { "01:01P", "02:01" }
                },
                LocusMismatchB = new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    PGroupsToMatchInPositionOne = new List<string> { "07:02P" },
                    PGroupsToMatchInPositionTwo = new List<string> { "08:01P" }
                },
                LocusMismatchDRB1 = new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    PGroupsToMatchInPositionOne = new List<string> { "01:11P" },
                    PGroupsToMatchInPositionTwo = new List<string> { "03:41P" }
                }
            };
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtA_DoesNotMatchDonorWithNoMatchAtLocusAAndHalfMatchAtLocusB()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            var results = await matchingService.Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithNoMatchAtLocusAAndHalfMatchAtB.DonorId);
        }

        [Test]
        public async Task Search_WithThreeAllowedMismatchesTwoAtAAndOneAtB_MatchesDonorsWithExactMatchAtAAndB()
        {
            searchCriteria.DonorMismatchCount = 3;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            searchCriteria.LocusMismatchB.MismatchCount = 1;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHeterozygousMatchAtLocusA.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHomozygousMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task Search_WithThreeAllowedMismatchesTwoAtAAndOneAtB_MatchesDonorsWithSingleMatchAtAAndCompleteMatchAtB()
        {
            searchCriteria.DonorMismatchCount = 3;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            searchCriteria.LocusMismatchB.MismatchCount = 1;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA.DonorId);
        }

        [Test]
        public async Task Search_WithThreeAllowedMismatchesTwoAtAAndOneAtB_ReturnsDonorsWithNoMatchAtLocusA()
        {
            searchCriteria.DonorMismatchCount = 3;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            searchCriteria.LocusMismatchB.MismatchCount = 1;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithNoMatchAtLocusAAndExactMatchAtB.DonorId);
        }

        [Test]
        public async Task Search_WithThreeAllowedMismatchesTwoAtAAndOneAtB_MatchesDonorWithNoMatchAtLocusAAndHalfMatchAtLocusB()
        {
            searchCriteria.DonorMismatchCount = 3;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            searchCriteria.LocusMismatchB.MismatchCount = 1;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithNoMatchAtLocusAAndHalfMatchAtB.DonorId);
        }
    }
}
