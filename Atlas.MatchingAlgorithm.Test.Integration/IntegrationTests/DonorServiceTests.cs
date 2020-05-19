using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests
{
    [TestFixture]
    public class DonorServiceTests
    {
        private IDonorService donorService;
        private IDonorInspectionRepository donorInspectionRepository;

        [SetUp]
        public void SetUp()
        {
            var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IActiveRepositoryFactory>();

            donorService = DependencyInjection.DependencyInjection.Provider.GetService<IDonorService>();
            donorInspectionRepository = repositoryFactory.GetDonorInspectionRepository();
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorDoesNotExist_CreatesDonorInDatabase()
        {
            var donorInfo = new DonorInfoBuilder(DonorIdGenerator.NextId()).Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo });

            var donor = await donorInspectionRepository.GetDonor(donorInfo.DonorId);
            donor.Should().NotBeNull();
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorDoesNotExist_PopulatesPGroupsForDonorHla()
        {
            var donorInfo = new DonorInfoBuilder(DonorIdGenerator.NextId()).Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo });

            var pGroupCount = await GetPGroupCount(donorInfo.DonorId, Locus.A, TypePosition.One);
            pGroupCount.Should().NotBe(0);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorDoesNotExist_AndInvalidHlaName_DoesNotCreateDonorInDatabase()
        {
            var donorInfo = new DonorInfoBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(Locus.A, TypePosition.One, "invalid-hla-name")
                .Build();

            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo });

            var donor = await donorInspectionRepository.GetDonor(donorInfo.DonorId);
            donor.Should().BeNull();
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenCalledMultipleTimesForADonor_DoesNotCreateMultipleDonorsWithTheSameId()
        {
            var donorInfo = new DonorInfoBuilder(DonorIdGenerator.NextId()).Build();

            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo });
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo });
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo });

            var donors = await donorInspectionRepository.GetDonors(new[] { donorInfo.DonorId });
            donors.Count().Should().Be(1);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorExists_DoesNotCreateANewDonorWithTheSameDonorId()
        {
            var donorId = DonorIdGenerator.NextId();
            var donorInfo = new DonorInfoBuilder(donorId).WithDonorType(DonorType.Adult).Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo });

            var updatedDonor = new DonorInfoBuilder(donorId).WithDonorType(DonorType.Cord).Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { updatedDonor });

            var donors = await donorInspectionRepository.GetDonors(new[] { donorId });
            donors.Count().Should().Be(1);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorExists_UpdatesDonorDetailsInDatabase()
        {
            var donorId = DonorIdGenerator.NextId();
            var donorInfo = new DonorInfoBuilder(donorId).WithDonorType(DonorType.Adult).Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo });

            const DonorType newDonorType = DonorType.Cord;
            var updatedDonor = new DonorInfoBuilder(donorId).WithDonorType(newDonorType).Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { updatedDonor });

            var donor = await donorInspectionRepository.GetDonor(donorId);
            donor.DonorType.Should().Be(newDonorType);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorExists_ReprocessesHla()
        {
            var donorId = DonorIdGenerator.NextId();
            const Locus locus = Locus.A;
            const TypePosition position = TypePosition.One;

            var donorInfo = new DonorInfoBuilder(donorId).WithHlaAtLocus(locus, position, "*01:01").Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo });
            var initialPGroupsCount = await GetPGroupCount(donorId, locus, position);

            // XX code will always have more p-groups than a single allele
            var updatedDonor = new DonorInfoBuilder(donorId).WithHlaAtLocus(locus, position, "*01:XX").Build();

            await donorService.CreateOrUpdateDonorBatch(new[] { updatedDonor });
            var updatedPGroupsCount = await GetPGroupCount(donorId, locus, position);

            updatedPGroupsCount.Should().BeGreaterThan(initialPGroupsCount);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorExists_AndInvalidHlaName_DoesNotUpdateDonorDetails()
        {
            const DonorType oldDonorType = DonorType.Adult;
            const DonorType newDonorType = DonorType.Cord;

            var donorId = DonorIdGenerator.NextId();
            var donorInfo = new DonorInfoBuilder(donorId).WithDonorType(oldDonorType).Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo });

            var updatedDonor = new DonorInfoBuilder(donorId).WithDonorType(newDonorType)
                .WithHlaAtLocus(Locus.A, TypePosition.One, "invalid-hla-name")
                .Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { updatedDonor });

            var donor = await donorInspectionRepository.GetDonor(donorId);
            donor.DonorType.Should().Be(oldDonorType);
            donor.DonorType.Should().NotBe(newDonorType);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorExists_AndInvalidHlaName_DoesNotReprocessHla()
        {
            var donorId = DonorIdGenerator.NextId();
            const Locus locus = Locus.A;
            const TypePosition position = TypePosition.One;

            var donorInfo = new DonorInfoBuilder(donorId)
                .WithHlaAtLocus(locus, position, "*01:01")
                .Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo });
            var initialPGroupsCount = await GetPGroupCount(donorId, locus, position);

            var updatedDonor = new DonorInfoBuilder(donorId)
                .WithHlaAtLocus(locus, position, "invalid-hla-name")
                .Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { updatedDonor });
            var updatedPGroupsCount = await GetPGroupCount(donorId, locus, position);

            updatedPGroupsCount.Should().Be(initialPGroupsCount);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_CreatesMultipleDonorsInDatabase()
        {
            var donorInfo1 = new DonorInfoBuilder(DonorIdGenerator.NextId()).Build();
            var donorInfo2 = new DonorInfoBuilder(DonorIdGenerator.NextId()).Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo1, donorInfo2
            });

            var donor1 = await donorInspectionRepository.GetDonor(donorInfo1.DonorId);
            var donor2 = await donorInspectionRepository.GetDonor(donorInfo2.DonorId);
            donor1.Should().NotBeNull();
            donor2.Should().NotBeNull();
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_PopulatesPGroupsForMultipleDonors()
        {
            var donorInfo1 = new DonorInfoBuilder(DonorIdGenerator.NextId()).Build();
            var donorInfo2 = new DonorInfoBuilder(DonorIdGenerator.NextId()).Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo1, donorInfo2 });

            var pGroupCounts = (await GetPGroupCounts(new[] { donorInfo1.DonorId, donorInfo2.DonorId }, Locus.A, TypePosition.One)).ToList();
            pGroupCounts.First().Should().NotBe(0);
            pGroupCounts.Last().Should().NotBe(0);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_UpdatesMultipleDonorDetailsInDatabase()
        {
            const DonorType oldDonorType = DonorType.Adult;
            var donorId1 = DonorIdGenerator.NextId();
            var donorId2 = DonorIdGenerator.NextId();
            var donorInfo1 = new DonorInfoBuilder(donorId1).WithDonorType(oldDonorType).Build();
            var donorInfo2 = new DonorInfoBuilder(donorId2).WithDonorType(oldDonorType).Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo1, donorInfo2 });

            const DonorType newDonorType = DonorType.Cord;
            var updatedDonor1 = new DonorInfoBuilder(donorId1).WithDonorType(newDonorType).Build();
            var updatedDonor2 = new DonorInfoBuilder(donorId2).WithDonorType(newDonorType).Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { updatedDonor1, updatedDonor2 });

            var donor1 = await donorInspectionRepository.GetDonor(donorId1);
            var donor2 = await donorInspectionRepository.GetDonor(donorId2);
            donor1.DonorType.Should().Be(newDonorType);
            donor2.DonorType.Should().Be(newDonorType);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_ReprocessesMultipleDonorsHla()
        {
            var donorId1 = DonorIdGenerator.NextId();
            var donorId2 = DonorIdGenerator.NextId();
            const Locus locus = Locus.A;
            const TypePosition position = TypePosition.One;

            var donorInfo1 = new DonorInfoBuilder(donorId1).WithHlaAtLocus(locus, position, "*01:01").Build();
            var donorInfo2 = new DonorInfoBuilder(donorId2).WithHlaAtLocus(locus, position, "*01:01:01").Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo1, donorInfo2 });
            var initialPGroupsCounts = (await GetPGroupCounts(new[] { donorId1, donorId2 }, locus, position)).ToList();

            // XX code will always have more p-groups than a single allele
            var updatedDonor1 = new DonorInfoBuilder(donorId1).WithHlaAtLocus(locus, position, "*01:XX").Build();
            var updatedDonor2 = new DonorInfoBuilder(donorId2).WithHlaAtLocus(locus, position, "*01:XX").Build();

            await donorService.CreateOrUpdateDonorBatch(new[] { updatedDonor1, updatedDonor2 });
            var updatedPGroupsCounts = (await GetPGroupCounts(new[] { donorId1, donorId2 }, locus, position)).ToList();

            updatedPGroupsCounts.First().Should().BeGreaterThan(initialPGroupsCounts.First());
            updatedPGroupsCounts.Last().Should().BeGreaterThan(initialPGroupsCounts.Last());
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_CreatesNewAndUpdatesExistingMultipleDonorDetailsInDatabase()
        {
            const DonorType oldDonorType = DonorType.Adult;
            var donorId1 = DonorIdGenerator.NextId();
            var donorId2 = DonorIdGenerator.NextId();
            var donorInfo1 = new DonorInfoBuilder(donorId1).WithDonorType(oldDonorType).Build();
            var donorInfo2 = new DonorInfoBuilder(donorId2).WithDonorType(oldDonorType).Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo1, donorInfo2 });

            var donorId3 = DonorIdGenerator.NextId();
            var donorId4 = DonorIdGenerator.NextId();
            var donorInfo3 = new DonorInfoBuilder(donorId3).WithDonorType(oldDonorType).Build();
            var donorInfo4 = new DonorInfoBuilder(donorId4).WithDonorType(oldDonorType).Build();

            const DonorType newDonorType = DonorType.Cord;
            var updatedDonor1 = new DonorInfoBuilder(donorId1).WithDonorType(newDonorType).Build();
            var updatedDonor2 = new DonorInfoBuilder(donorId2).WithDonorType(newDonorType).Build();

            await donorService.CreateOrUpdateDonorBatch(new[] { updatedDonor1, updatedDonor2, donorInfo3, donorInfo4 });

            var donor1 = await donorInspectionRepository.GetDonor(donorId1);
            var donor2 = await donorInspectionRepository.GetDonor(donorId2);
            var donor3 = await donorInspectionRepository.GetDonor(donorId3);
            var donor4 = await donorInspectionRepository.GetDonor(donorId4);
            donor1.DonorType.Should().Be(newDonorType);
            donor2.DonorType.Should().Be(newDonorType);
            donor3.Should().NotBeNull();
            donor4.Should().NotBeNull();
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorsDoNotExist_CreatesDonorBatchAsAvailableForSearch()
        {
            var donorInfo1 = new DonorInfoBuilder(DonorIdGenerator.NextId()).Build();
            var donorInfo2 = new DonorInfoBuilder(DonorIdGenerator.NextId()).Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo1, donorInfo2 });

            var donors = (await donorInspectionRepository.GetDonors(new[] { donorInfo1.DonorId, donorInfo2.DonorId })).ToList();
            donors.First().Value.IsAvailableForSearch.Should().BeTrue();
            donors.Last().Value.IsAvailableForSearch.Should().BeTrue();
        }

        [Test]
        public async Task SetDonorBatchAsUnavailableForSearch_SetsDonorBatchAsUnavailableForSearch()
        {
            var donorInfo1 = new DonorInfoBuilder(DonorIdGenerator.NextId()).Build();
            var donorInfo2 = new DonorInfoBuilder(DonorIdGenerator.NextId()).Build();
            var donorInfoIds = new[] { donorInfo1.DonorId, donorInfo2.DonorId };
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo1, donorInfo2 });

            await donorService.SetDonorBatchAsUnavailableForSearch(donorInfoIds);

            var donors = (await donorInspectionRepository.GetDonors(donorInfoIds)).ToList();
            donors.First().Value.IsAvailableForSearch.Should().BeFalse();
            donors.Last().Value.IsAvailableForSearch.Should().BeFalse();
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorsExist_SetsDonorBatchAsAvailableForSearch()
        {
            // Arrange: first create donors, then set as unavailable
            var donorInfo1 = new DonorInfoBuilder(DonorIdGenerator.NextId()).Build();
            var donorInfo2 = new DonorInfoBuilder(DonorIdGenerator.NextId()).Build();
            var donorInfos = new[] { donorInfo1, donorInfo2 };
            var donorInfoIds = new[] { donorInfo1.DonorId, donorInfo2.DonorId };
            await donorService.CreateOrUpdateDonorBatch(donorInfos);
            await donorService.SetDonorBatchAsUnavailableForSearch(donorInfoIds);

            await donorService.CreateOrUpdateDonorBatch(donorInfos);

            var donors = (await donorInspectionRepository.GetDonors(donorInfoIds)).ToList();
            donors.First().Value.IsAvailableForSearch.Should().BeTrue();
            donors.Last().Value.IsAvailableForSearch.Should().BeTrue();
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorExists_SetsDonorAsAvailableForSearchAndUpdatesDonorDetailsAndReprocessesHla()
        {
            var donorId = DonorIdGenerator.NextId();
            const DonorType oldDonorType = DonorType.Adult;
            const DonorType newDonorType = DonorType.Cord;

            // XX code will always have more p-groups than a single allele
            const string oldHla = "*01:01";
            const string newHla = "*01:XX";
            const Locus locus = Locus.A;
            const TypePosition position = TypePosition.One;

            var donorInfo = new DonorInfoBuilder(donorId)
                .WithDonorType(oldDonorType)
                .WithHlaAtLocus(locus, position, oldHla)
                .Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo });
            var initialPGroupsCount = await GetPGroupCount(donorId, locus, position);

            var updatedDonor = new DonorInfoBuilder(donorId)
                .WithDonorType(newDonorType)
                .WithHlaAtLocus(locus, position, newHla)
                .Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { updatedDonor });

            var donor = await donorInspectionRepository.GetDonor(donorId);
            var updatedPGroupsCount = await GetPGroupCount(donorId, locus, position);

            donor.IsAvailableForSearch.Should().BeTrue();
            donor.DonorType.Should().Be(newDonorType);
            updatedPGroupsCount.Should().BeGreaterThan(initialPGroupsCount);
        }

        private async Task<int> GetPGroupCount(int donorId, Locus locus, TypePosition position)
        {
            var counts = await GetPGroupCounts(new[] { donorId }, locus, position);
            return counts.SingleOrDefault();
        }

        private async Task<IEnumerable<int>> GetPGroupCounts(IEnumerable<int> donorIds, Locus locus, TypePosition position)
        {
            var pGroupsForDonor = await donorInspectionRepository.GetPGroupsForDonors(donorIds);
            var pGroupNames = pGroupsForDonor.Select(p => p.PGroupNames.DataAtPosition(locus, position));
            return pGroupNames.Where(p => p != null).Select(p => p.Count());
        }
    }
}