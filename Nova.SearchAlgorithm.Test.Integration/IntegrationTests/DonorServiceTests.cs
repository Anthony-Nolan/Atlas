using System.Linq;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using Nova.Utils.Http.Exceptions;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests
{
    [TestFixture]
    public class DonorServiceTests: IntegrationTestBase
    {
        private IDonorService donorService;
        private IDonorInspectionRepository donorInspectionRepository;

        [SetUp]
        public void SetUp()
        {
            donorService = Container.Resolve<IDonorService>();
            donorInspectionRepository = Container.Resolve<IDonorInspectionRepository>();
        }

        [Test]
        public async Task CreateDonor_WhenCalledMultipleTimesForADonor_DoesNotCreateMultipleDonorsWithTheSameId()
        {
            var inputDonor = new InputDonorBuilder(DonorIdGenerator.NextId()).Build();
            await donorService.CreateDonor(inputDonor);
            Assert.ThrowsAsync<NovaHttpException>(() => donorService.CreateDonor(inputDonor));

            // Should not throw exception
            await donorInspectionRepository.GetDonor(inputDonor.DonorId);
        }
        
        [Test]
        public async Task CreateDonor_CreatesDonorInDatabase()
        {
            var inputDonor = new InputDonorBuilder(DonorIdGenerator.NextId()).Build();
            await donorService.CreateDonor(inputDonor);

            var donor = await donorInspectionRepository.GetDonor(inputDonor.DonorId);
            donor.Should().NotBeNull();
        }
        
        [Test]
        public async Task CreateDonor_PopulatesPGroupsForDonorHla()
        {
            var inputDonor = new InputDonorBuilder(DonorIdGenerator.NextId()).Build();
            await donorService.CreateDonor(inputDonor);

            var donors = await donorInspectionRepository.GetPGroupsForDonors(new[]{inputDonor.DonorId});
            donors.First().PGroupNames.A_1.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task UpdateDonor_DoesNotCreateANewDonorWithTheSameDonorId()
        {
            var donorId = DonorIdGenerator.NextId();
            var inputDonor = new InputDonorBuilder(donorId).WithDonorType(DonorType.Adult).Build();
            await donorService.CreateDonor(inputDonor);
            
            var updatedDonor = new InputDonorBuilder(donorId).WithDonorType(DonorType.Cord).Build();
            await donorService.UpdateDonor(updatedDonor);

            // Should not throw exception
            await donorInspectionRepository.GetDonor(donorId);
        }

        [Test]
        public async Task UpdateDonor_UpdatesDonorDetailsInDatabase()
        {
            var donorId = DonorIdGenerator.NextId();
            var inputDonor = new InputDonorBuilder(donorId).WithDonorType(DonorType.Adult).Build();
            await donorService.CreateDonor(inputDonor);

            const DonorType newDonorType = DonorType.Cord;
            var updatedDonor = new InputDonorBuilder(donorId).WithDonorType(newDonorType).Build();
            await donorService.UpdateDonor(updatedDonor);

            var donor = await donorInspectionRepository.GetDonor(donorId);
            donor.DonorType.Should().Be(newDonorType);
        }

        [Test]
        public async Task UpdateDonor_ReprocessesHla()
        {
            var donorId = DonorIdGenerator.NextId();
            const Locus locus = Locus.A;
            const TypePosition position = TypePosition.One;
            
            var inputDonor = new InputDonorBuilder(donorId).WithHlaAtLocus(locus, position, "*01:01").Build();
            await donorService.CreateDonor(inputDonor);
            var initialPGroupsCount = (await donorInspectionRepository.GetPGroupsForDonors(new[]{donorId}))
                .First().PGroupNames.DataAtPosition(locus, position).Count();

            const DonorType newDonorType = DonorType.Cord;
            // XX code will always have more p-groups than a single allele
            var updatedDonor = new InputDonorBuilder(donorId).WithHlaAtLocus(locus, position, "*01:XX").Build();

            await donorService.UpdateDonor(updatedDonor);
            var updatedPGroupsCount = (await donorInspectionRepository.GetPGroupsForDonors(new[]{donorId}))
                .First().PGroupNames.DataAtPosition(locus, position).Count();

            updatedPGroupsCount.Should().BeGreaterThan(initialPGroupsCount);
        }
    }
}