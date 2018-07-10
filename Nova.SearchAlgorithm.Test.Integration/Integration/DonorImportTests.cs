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
    public class DonorImportTests : IntegrationTestBase
    {
        private IDonorImportRepository importRepo;
        private IDonorInspectionRepository inspectionRepo;
        private IHlaUpdateService updateService;

        public DonorImportTests(DonorStorageImplementation param) : base(param) { }

        [SetUp]
        public void ResolveSearchRepo()
        {
            importRepo = container.Resolve<IDonorImportRepository>();
            inspectionRepo = container.Resolve<IDonorInspectionRepository>();
            updateService = container.Resolve<IHlaUpdateService>();
        }
        
        [Test]
        public async Task InsertDonor_InsertsCorrectDonorData()
        {
            const int donorId = 1;
            var inputDonor = DonorWithId(donorId);
            await importRepo.InsertDonor(inputDonor);

            var storedDonor = await inspectionRepo.GetDonor(donorId);
            AssertStoredDonorInfoMatchesOriginalDonorInfo(storedDonor, inputDonor);
        }
        
        [Test]
        public async Task InsertBatchOfDonors_InsertsCorrectDonorData()
        {
            const int donorId1 = 3;
            const int donorId2 = 4;
            var donorIds = new List<int> {donorId1, donorId2};
            var inputDonors = donorIds.Select(DonorWithId).ToList();
            await importRepo.InsertBatchOfDonors(inputDonors);

            var storedDonor1 = await inspectionRepo.GetDonor(donorId1);
            var storedDonor2 = await inspectionRepo.GetDonor(donorId2);
            AssertStoredDonorInfoMatchesOriginalDonorInfo(storedDonor1, inputDonors.Single(d => d.DonorId == donorId1));
            AssertStoredDonorInfoMatchesOriginalDonorInfo(storedDonor2, inputDonors.Single(d => d.DonorId == donorId2));
        }
        
        [Test]
        public async Task UpdateDonorHla_DoesNotUpdateStoredDonorInformation()
        {
            const int donorId = 2;
            var inputDonor = DonorWithId(donorId);
            await importRepo.InsertDonor(inputDonor);
            
            await updateService.UpdateDonorHla();

            var storedDonor = await inspectionRepo.GetDonor(donorId);
            AssertStoredDonorInfoMatchesOriginalDonorInfo(storedDonor, inputDonor);
        }

        private static void AssertStoredDonorInfoMatchesOriginalDonorInfo(DonorResult donorActual, RawInputDonor donorExpected)
        {
            donorActual.DonorId.Should().Be(donorExpected.DonorId);
            donorActual.DonorType.Should().Be(donorExpected.DonorType);
            donorActual.RegistryCode.Should().Be(donorExpected.RegistryCode);
            donorActual.HlaNames.ShouldBeEquivalentTo(donorExpected.HlaNames);
        }

        private static RawInputDonor DonorWithId(int id)
        {
            return  new RawInputDonor
            {
                RegistryCode = RegistryCode.DKMS,
                DonorType = DonorType.Cord,
                DonorId = id,
                HlaNames = new PhenotypeInfo<string>
                {
                    A_1 = "01:02",
                    A_2 = "30:02:01:01",
                    B_1 = "07:02",
                    B_2 = "08:01",
                    DRB1_1 = "01:11",
                    DRB1_2 = "03:41",
                }
            };
        }
    }
}
