using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Donors;
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
        private readonly string tokenHlaVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;

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
            var donorInfo = new DonorInfoBuilder().Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo }, default, tokenHlaVersion, false);

            var donor = await donorInspectionRepository.GetDonor(donorInfo.DonorId);
            donor.Should().NotBeNull();
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorDoesNotExist_PopulatesPGroupsForDonorHla()
        {
            var donorInfo = new DonorInfoBuilder().Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo }, default, tokenHlaVersion, false);

            var pGroupCount = await GetPGroupCount(donorInfo.DonorId, Locus.A, LocusPosition.One);
            pGroupCount.Should().NotBe(0);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorDoesNotExist_AndInvalidHlaName_DoesNotCreateDonorInDatabase()
        {
            var donorInfo = new DonorInfoBuilder()
                .WithHlaAtLocus(Locus.A, LocusPosition.One, "9999:9999")
                .Build();

            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo }, default, tokenHlaVersion, false);

            var donor = await donorInspectionRepository.GetDonor(donorInfo.DonorId);
            donor.Should().BeNull();
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenCalledMultipleTimesForADonor_DoesNotCreateMultipleDonorsWithTheSameId()
        {
            var donorInfo = new DonorInfoBuilder().Build();

            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo }, default, tokenHlaVersion, false);
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo }, default, tokenHlaVersion, false);
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo }, default, tokenHlaVersion, false);

            var donors = await donorInspectionRepository.GetDonors(new[] { donorInfo.DonorId });
            donors.Count().Should().Be(1);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorExists_DoesNotCreateANewDonorWithTheSameDonorId()
        {
            var donorInfo = new DonorInfoBuilder().WithDonorType(DonorType.Adult).Build();
            var donorId = donorInfo.DonorId;
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo }, default, tokenHlaVersion, false);

            var updatedDonor = new DonorInfoBuilder(donorId).WithDonorType(DonorType.Cord).Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { updatedDonor }, default, tokenHlaVersion, false);

            var donors = await donorInspectionRepository.GetDonors(new[] { donorId });
            donors.Count().Should().Be(1);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorExists_UpdatesDonorDetailsInDatabase()
        {
            var donorInfo = new DonorInfoBuilder().WithDonorType(DonorType.Adult).Build();
            var donorId = donorInfo.DonorId;
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo }, default, tokenHlaVersion, false);

            const DonorType newDonorType = DonorType.Cord;
            var updatedDonor = new DonorInfoBuilder(donorId).WithDonorType(newDonorType).Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { updatedDonor }, default, tokenHlaVersion, false);

            var donor = await donorInspectionRepository.GetDonor(donorId);
            donor.DonorType.Should().Be(newDonorType);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorExists_ReprocessesHla()
        {
            const Locus locus = Locus.A;
            const LocusPosition position = LocusPosition.One;

            var donorInfo = new DonorInfoBuilder().WithHlaAtLocus(locus, position, "*01:01").Build();
            var donorId = donorInfo.DonorId;
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo }, default, tokenHlaVersion, false);
            var initialPGroupsCount = await GetPGroupCount(donorId, locus, position);

            // XX code will always have more p-groups than a single allele
            var updatedDonor = new DonorInfoBuilder(donorId).WithHlaAtLocus(locus, position, "*01:XX").Build();

            await donorService.CreateOrUpdateDonorBatch(new[] { updatedDonor }, default, tokenHlaVersion, false);
            var updatedPGroupsCount = await GetPGroupCount(donorId, locus, position);

            updatedPGroupsCount.Should().BeGreaterThan(initialPGroupsCount);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorExists_AndInvalidHlaName_DoesNotUpdateDonorDetails()
        {
            const DonorType oldDonorType = DonorType.Adult;
            const DonorType newDonorType = DonorType.Cord;

            var donorInfo = new DonorInfoBuilder().WithDonorType(oldDonorType).Build();
            var donorId = donorInfo.DonorId;
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo }, default, tokenHlaVersion, false);

            var updatedDonor = new DonorInfoBuilder(donorId).WithDonorType(newDonorType)
                .WithHlaAtLocus(Locus.A, LocusPosition.One, "*9999:9999")
                .Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { updatedDonor }, default, tokenHlaVersion, false);

            var donor = await donorInspectionRepository.GetDonor(donorId);
            donor.DonorType.Should().Be(oldDonorType);
            donor.DonorType.Should().NotBe(newDonorType);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorExists_AndInvalidHlaName_DoesNotReprocessHla()
        {
            const Locus locus = Locus.A;
            const LocusPosition position = LocusPosition.One;

            var donorInfo = new DonorInfoBuilder()
                .WithHlaAtLocus(locus, position, "*01:01")
                .Build();
            var donorId = donorInfo.DonorId;
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo }, default, tokenHlaVersion, false);
            var initialPGroupsCount = await GetPGroupCount(donorId, locus, position);

            var updatedDonor = new DonorInfoBuilder(donorId)
                .WithHlaAtLocus(locus, position, "*9999:9999")
                .Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { updatedDonor }, default, tokenHlaVersion, false);
            var updatedPGroupsCount = await GetPGroupCount(donorId, locus, position);

            updatedPGroupsCount.Should().Be(initialPGroupsCount);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_CreatesMultipleDonorsInDatabase()
        {
            var donorInfo1 = new DonorInfoBuilder().Build();
            var donorInfo2 = new DonorInfoBuilder().Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo1, donorInfo2}, default, tokenHlaVersion, false);

            var donor1 = await donorInspectionRepository.GetDonor(donorInfo1.DonorId);
            var donor2 = await donorInspectionRepository.GetDonor(donorInfo2.DonorId);
            donor1.Should().NotBeNull();
            donor2.Should().NotBeNull();
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_PopulatesPGroupsForMultipleDonors()
        {
            var donorInfo1 = new DonorInfoBuilder().Build();
            var donorInfo2 = new DonorInfoBuilder().Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo1, donorInfo2 }, default, tokenHlaVersion, false);

            var pGroupCounts = (await GetPGroupCounts(new[] { donorInfo1.DonorId, donorInfo2.DonorId }, Locus.A, LocusPosition.One)).ToList();
            pGroupCounts.First().Should().NotBe(0);
            pGroupCounts.Last().Should().NotBe(0);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_UpdatesMultipleDonorDetailsInDatabase()
        {
            const DonorType oldDonorType = DonorType.Adult;
            var donorInfo1 = new DonorInfoBuilder().WithDonorType(oldDonorType).Build();
            var donorInfo2 = new DonorInfoBuilder().WithDonorType(oldDonorType).Build();
            var donorId1 = donorInfo1.DonorId;
            var donorId2 = donorInfo2.DonorId;
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo1, donorInfo2 }, default, tokenHlaVersion, false);

            const DonorType newDonorType = DonorType.Cord;
            var updatedDonor1 = new DonorInfoBuilder(donorId1).WithDonorType(newDonorType).Build();
            var updatedDonor2 = new DonorInfoBuilder(donorId2).WithDonorType(newDonorType).Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { updatedDonor1, updatedDonor2 }, default, tokenHlaVersion, false);

            var donor1 = await donorInspectionRepository.GetDonor(donorId1);
            var donor2 = await donorInspectionRepository.GetDonor(donorId2);
            donor1.DonorType.Should().Be(newDonorType);
            donor2.DonorType.Should().Be(newDonorType);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_ReprocessesMultipleDonorsHla()
        {
            const Locus locus = Locus.A;
            const LocusPosition position = LocusPosition.One;

            var donorInfo1 = new DonorInfoBuilder().WithHlaAtLocus(locus, position, "*01:01").Build();
            var donorInfo2 = new DonorInfoBuilder().WithHlaAtLocus(locus, position, "*01:01:01").Build();
            var donorId1 = donorInfo1.DonorId;
            var donorId2 = donorInfo2.DonorId;
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo1, donorInfo2 }, default, tokenHlaVersion, false);
            var initialPGroupsCounts = (await GetPGroupCounts(new[] { donorId1, donorId2 }, locus, position)).ToList();

            // XX code will always have more p-groups than a single allele
            var updatedDonor1 = new DonorInfoBuilder(donorId1).WithHlaAtLocus(locus, position, "*01:XX").Build();
            var updatedDonor2 = new DonorInfoBuilder(donorId2).WithHlaAtLocus(locus, position, "*01:XX").Build();

            await donorService.CreateOrUpdateDonorBatch(new[] { updatedDonor1, updatedDonor2 }, default, tokenHlaVersion, false);
            var updatedPGroupsCounts = (await GetPGroupCounts(new[] { donorId1, donorId2 }, locus, position)).ToList();

            updatedPGroupsCounts.First().Should().BeGreaterThan(initialPGroupsCounts.First());
            updatedPGroupsCounts.Last().Should().BeGreaterThan(initialPGroupsCounts.Last());
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_CreatesNewAndUpdatesExistingMultipleDonorDetailsInDatabase()
        {
            const DonorType oldDonorType = DonorType.Adult;
            var donorInfo1 = new DonorInfoBuilder().WithDonorType(oldDonorType).Build();
            var donorInfo2 = new DonorInfoBuilder().WithDonorType(oldDonorType).Build();
            var donorId1 = donorInfo1.DonorId;
            var donorId2 = donorInfo2.DonorId;
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo1, donorInfo2 }, default, tokenHlaVersion, false);

            var donorInfo3 = new DonorInfoBuilder().WithDonorType(oldDonorType).Build();
            var donorInfo4 = new DonorInfoBuilder().WithDonorType(oldDonorType).Build();
            var donorId3 = donorInfo3.DonorId;
            var donorId4 = donorInfo4.DonorId;

            const DonorType newDonorType = DonorType.Cord;
            var updatedDonor1 = new DonorInfoBuilder(donorId1).WithDonorType(newDonorType).Build();
            var updatedDonor2 = new DonorInfoBuilder(donorId2).WithDonorType(newDonorType).Build();

            await donorService.CreateOrUpdateDonorBatch(new[] { updatedDonor1, updatedDonor2, donorInfo3, donorInfo4 }, default, tokenHlaVersion, false);

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
            var donorInfo1 = new DonorInfoBuilder().Build();
            var donorInfo2 = new DonorInfoBuilder().Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo1, donorInfo2 }, default, tokenHlaVersion, false);

            var donors = (await donorInspectionRepository.GetDonors(new[] { donorInfo1.DonorId, donorInfo2.DonorId })).ToList();
            donors.First().Value.IsAvailableForSearch.Should().BeTrue();
            donors.Last().Value.IsAvailableForSearch.Should().BeTrue();
        }

        [Test]
        public async Task SetDonorBatchAsUnavailableForSearch_SetsDonorBatchAsUnavailableForSearch()
        {
            var donorInfo1 = new DonorInfoBuilder().Build();
            var donorInfo2 = new DonorInfoBuilder().Build();
            var donorInfoIds = new List<int> { donorInfo1.DonorId, donorInfo2.DonorId };
            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo1, donorInfo2 }, default, tokenHlaVersion, false);

            await donorService.SetDonorBatchAsUnavailableForSearch(donorInfoIds, default);

            var donors = (await donorInspectionRepository.GetDonors(donorInfoIds)).ToList();
            donors.First().Value.IsAvailableForSearch.Should().BeFalse();
            donors.Last().Value.IsAvailableForSearch.Should().BeFalse();
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorsExist_SetsDonorBatchAsAvailableForSearch()
        {
            // Arrange: first create donors, then set as unavailable
            var donorInfo1 = new DonorInfoBuilder().Build();
            var donorInfo2 = new DonorInfoBuilder().Build();
            var donorInfos = new[] { donorInfo1, donorInfo2 };
            var donorInfoIds = new List<int> { donorInfo1.DonorId, donorInfo2.DonorId };
            await donorService.CreateOrUpdateDonorBatch(donorInfos, default, tokenHlaVersion, false);
            await donorService.SetDonorBatchAsUnavailableForSearch(donorInfoIds, default);

            await donorService.CreateOrUpdateDonorBatch(donorInfos, default, tokenHlaVersion, false);

            var donors = (await donorInspectionRepository.GetDonors(donorInfoIds)).ToList();
            donors.First().Value.IsAvailableForSearch.Should().BeTrue();
            donors.Last().Value.IsAvailableForSearch.Should().BeTrue();
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorExists_SetsDonorAsAvailableForSearchAndUpdatesDonorDetailsAndReprocessesHla()
        {
            const DonorType oldDonorType = DonorType.Adult;
            const DonorType newDonorType = DonorType.Cord;

            // XX code will always have more p-groups than a single allele
            const string oldHla = "*01:01";
            const string newHla = "*01:XX";
            const Locus locus = Locus.A;
            const LocusPosition position = LocusPosition.One;

            var donorInfo = new DonorInfoBuilder()
                .WithDonorType(oldDonorType)
                .WithHlaAtLocus(locus, position, oldHla)
                .Build();
            var donorId = donorInfo.DonorId;

            await donorService.CreateOrUpdateDonorBatch(new[] { donorInfo }, default, tokenHlaVersion, false);
            var initialPGroupsCount = await GetPGroupCount(donorId, locus, position);

            var updatedDonor = new DonorInfoBuilder(donorId)
                .WithDonorType(newDonorType)
                .WithHlaAtLocus(locus, position, newHla)
                .Build();
            await donorService.CreateOrUpdateDonorBatch(new[] { updatedDonor }, default, tokenHlaVersion, false);

            var donor = await donorInspectionRepository.GetDonor(donorId);
            var updatedPGroupsCount = await GetPGroupCount(donorId, locus, position);

            donor.IsAvailableForSearch.Should().BeTrue();
            donor.DonorType.Should().Be(newDonorType);
            updatedPGroupsCount.Should().BeGreaterThan(initialPGroupsCount);
        }

        private async Task<int> GetPGroupCount(int donorId, Locus locus, LocusPosition position)
        {
            var counts = await GetPGroupCounts(new[] { donorId }, locus, position);
            return counts.SingleOrDefault();
        }

        private async Task<IEnumerable<int>> GetPGroupCounts(IEnumerable<int> donorIds, Locus locus, LocusPosition position)
        {
            var pGroupsForDonor = await donorInspectionRepository.GetPGroupsForDonors(donorIds);
            var pGroupNames = pGroupsForDonor.Select(p => p.PGroupNames.GetPosition(locus, position));
            return pGroupNames.Where(p => p != null).Select(p => p.Count());
        }
    }
}