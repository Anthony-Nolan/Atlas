using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Services;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Integration.Integration
{
    public class MatchingTests : IntegrationTestBase
    {
        private AlleleLevelMatchCriteria searchCriteria;

        private IDonorSearchRepository searchRepo;
        private IDonorMatchingService matchingService;

        public MatchingTests(DonorStorageImplementation param) : base(param) { }

        [SetUp]
        public void ResolveSearchRepo()
        {
            searchRepo = container.Resolve<IDonorSearchRepository>();
            matchingService = container.Resolve<IDonorMatchingService>();
        }
        
        [OneTimeSetUp]
        public void ImportTestDonor()
        {
            IDonorImportRepository importRepo = container.Resolve<IDonorImportRepository>();

            // potential 2/2 homozygous match at locus A
            importRepo.AddOrUpdateDonorWithHla(new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 1,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { "01:01P", "01:02" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "01:01P", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    B_2 = new ExpandedHla { PGroups = new List<string> { "08:01P" } },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { "01:11P" } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "03:41P" } }
                }
            }).Wait();

            // potential 2/2 heterozygous match at locus A
            importRepo.AddOrUpdateDonorWithHla(new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 2,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { "01:01P", "30:02P" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "02:01", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    B_2 = new ExpandedHla { PGroups = new List<string> { "08:01P" } },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { "01:11P" } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "03:41P" } }
                }
            }).Wait();

            // potential 1/2 match at locus A - 1/2 in HvG direction, 2/2 in GvH direction
            importRepo.AddOrUpdateDonorWithHla(new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 3,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { "01:01P", "30:02P" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "26:04", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    B_2 = new ExpandedHla { PGroups = new List<string> { "08:01P" } },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { "01:11P" } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "03:41P" } }
                }
            }).Wait();

            // potential 1/2 match at locus A - 1/2 in both directions
            importRepo.AddOrUpdateDonorWithHla(new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 4,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { "01:02", "02:01" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "26:04", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    B_2 = new ExpandedHla { PGroups = new List<string> { "08:01P" } },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { "01:11P" } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "03:41P" } }
                }
            }).Wait();

            // 0/2 at locus A
            importRepo.AddOrUpdateDonorWithHla(new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 5,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { "11:59", "30:02P" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "26:04", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    B_2 = new ExpandedHla { PGroups = new List<string> { "08:01P" } },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { "01:11P" } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "03:41P" } }
                }
            }).Wait();

            // 0/2 at locus A, 1/2 at locus B
            importRepo.AddOrUpdateDonorWithHla(new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 6,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { "11:59", "30:02P" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "11:59", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    B_2 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { "01:11P" } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "03:41P" } }
                }
            }).Wait();
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
                    HlaNamesToMatchInPositionOne = new List<string> { "01:01P", "01:02" },
                    HlaNamesToMatchInPositionTwo = new List<string> { "01:01P", "02:01" }
                },
                LocusMismatchB = new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    HlaNamesToMatchInPositionOne = new List<string> { "07:02P" },
                    HlaNamesToMatchInPositionTwo = new List<string> { "08:01P" }
                },
                LocusMismatchDRB1 = new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    HlaNamesToMatchInPositionOne = new List<string> { "01:11P" },
                    HlaNamesToMatchInPositionTwo = new List<string> { "03:41P" }
                }
            };
        }

        private async Task<List<PotentialSearchResult>> Search(AlleleLevelMatchCriteria criteria)
        {
            var results = await matchingService.Search(criteria);
            return results.ToList();
        }

        [Test]
        public async Task TwoOfTwoHomozygousMatchAtLocusA()
        {
            var results = await Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == 1);
        }

        [Test]
        public async Task TwoOfTwoHeterozygousMatchAtLocusA()
        {
            var results = await Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == 2);
        }

        [Test]
        public async Task OneHalfMatchesNotPresentInExactSearchResults()
        {
            var results = await Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == 3);
            results.Should().NotContain(d => d.Donor.DonorId == 4);
            results.Should().NotContain(d => d.Donor.DonorId == 5);
            results.Should().NotContain(d => d.Donor.DonorId == 6);
        }

        [Test]
        public async Task ExactMatchIsReturnedBySearchWithOneMismatch()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchA.MismatchCount = 1;
            var results = await Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == 1);
            results.Should().Contain(d => d.Donor.DonorId == 2);
        }

        [Test]
        public async Task OneOfTwoHvGAtLocusAReturnedBySearchWithOneMismatch()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchA.MismatchCount = 1;
            var results = await Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == 3);
        }

        [Test]
        public async Task OneOfTwoBothDirectionsAtLocusAReturnedBySearchWithOneMismatch()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchA.MismatchCount = 1;
            var results = await Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == 4);
        }

        [Test]
        public async Task NoMatchAtLocusANotReturnedBySearchWithOneMismatch()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchA.MismatchCount = 1;
            var results = await Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == 5);
            results.Should().NotContain(d => d.Donor.DonorId == 6);
        }

        [Test]
        public async Task NoMatchAtLocusAReturnedBySearchWithTwoMismatches()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            var results = await Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == 5);
        }

        [Test]
        public async Task NoMatchAtLocusAHalfAtLocusBNotReturnedBySearchWithTwoMismatches()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            var results = await Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == 6);
        }

        [Test]
        public async Task ExactMatchIsReturnedBySearchWithTwoMismatches()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            var results = await Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == 1);
            results.Should().Contain(d => d.Donor.DonorId == 2);
        }

        [Test]
        public async Task HalfMatchAtLocusAReturnedBySearchWithTwoMismatches()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            var results = await Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == 3);
            results.Should().Contain(d => d.Donor.DonorId == 4);
        }

        [Test]
        public async Task ExactMatchIsReturnedBySearchWithThreeMismatches()
        {
            searchCriteria.DonorMismatchCount = 3;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            searchCriteria.LocusMismatchB.MismatchCount = 1;
            var results = await Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == 1);
            results.Should().Contain(d => d.Donor.DonorId == 2);
        }

        [Test]
        public async Task HalfMatchAtLocusAReturnedBySearchWithThreeMismatches()
        {
            searchCriteria.DonorMismatchCount = 3;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            searchCriteria.LocusMismatchB.MismatchCount = 1;
            var results = await Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == 3);
            results.Should().Contain(d => d.Donor.DonorId == 4);
        }

        [Test]
        public async Task NoMatchAtLocusABReturnedBySearchWithThreeMismatches()
        {
            searchCriteria.DonorMismatchCount = 3;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            searchCriteria.LocusMismatchB.MismatchCount = 1;
            var results = await Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == 5);
        }

        [Test]
        public async Task NoMatchAtLocusAHalfAtLocusBReturnedBySearchWithThreeMismatches()
        {
            searchCriteria.DonorMismatchCount = 3;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            searchCriteria.LocusMismatchB.MismatchCount = 1;
            var results = await Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == 6);
        }
    }
}
