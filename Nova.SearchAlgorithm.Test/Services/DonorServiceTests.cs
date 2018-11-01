using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Services;
using Nova.Utils.Http.Exceptions;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services
{
    [TestFixture]
    public class DonorServiceTests
    {
        private IDonorService donorService;
        private IDonorImportRepository importRepository;
        private IDonorInspectionRepository inspectionRepository;
        private IExpandHlaPhenotypeService expandHlaPhenotypeService;

        [SetUp]
        public void SetUp()
        {
            importRepository = Substitute.For<IDonorImportRepository>();
            inspectionRepository = Substitute.For<IDonorInspectionRepository>();
            expandHlaPhenotypeService = Substitute.For<IExpandHlaPhenotypeService>();

            donorService = new DonorService(importRepository, expandHlaPhenotypeService, inspectionRepository);
        }

        [Test]
        public void CreateDonor_WhenDonorExists_ThrowsException()
        {
            inspectionRepository.GetDonor(Arg.Any<int>()).Returns(new DonorResult());

            Assert.ThrowsAsync<NovaHttpException>(() => donorService.CreateDonor(new InputDonor()));
        }

        [Test]
        public async Task CreateDonor_WhenDonorDoesNotExist_AddsDonor()
        {
            var donor = new InputDonor
            {
                DonorId = 1,
                DonorType = DonorType.Adult,
                RegistryCode = RegistryCode.AN,
                HlaNames = new PhenotypeInfo<string>("hla")
            };
            inspectionRepository.GetDonor(Arg.Any<int>()).Returns(null, new DonorResult());

            await donorService.CreateDonor(donor);

            await importRepository.Received().AddDonorWithHla(Arg.Is<InputDonorWithExpandedHla>(d => d.DonorId == donor.DonorId));
        }

        [Test]
        public void UpdateDonor_WhenDonorDoesNotExist_ThrowsException()
        {
            Assert.ThrowsAsync<NovaNotFoundException>(() => donorService.UpdateDonor(new InputDonor()));
        }

        [Test]
        public async Task UpdateDonor_WhenDonorExists_UpdatesDonor()
        {
            var donor = new InputDonor
            {
                DonorId = 1,
                DonorType = DonorType.Adult,
                RegistryCode = RegistryCode.AN,
                HlaNames = new PhenotypeInfo<string>("hla")
            };
            inspectionRepository.GetDonor(Arg.Any<int>()).Returns(new DonorResult {HlaNames = new PhenotypeInfo<string>("different-hla")});

            await donorService.UpdateDonor(donor);

            await importRepository.Received().UpdateDonorWithHla(Arg.Is<InputDonorWithExpandedHla>(d => d.DonorId == donor.DonorId));
        }

        [Test]
        public async Task UpdateDonor_WhenDonorExistsAndIsUnchanged_DoesNotUpdateDonor()
        {
            var hlaStrings = new PhenotypeInfo<string>("hla");
            const DonorType donorType = DonorType.Adult;
            const RegistryCode registryCode = RegistryCode.AN;
            
            var donor = new InputDonor
            {
                DonorId = 1,
                DonorType = donorType,
                RegistryCode = registryCode,
                HlaNames = hlaStrings
            };
            var existingDonor = new DonorResult
            {
                DonorType = donorType,
                RegistryCode = registryCode,
                HlaNames = hlaStrings
            };
            inspectionRepository.GetDonor(Arg.Any<int>()).Returns(existingDonor);

            await donorService.UpdateDonor(donor);

            await importRepository.DidNotReceive().UpdateDonorWithHla(Arg.Is<InputDonorWithExpandedHla>(d => d.DonorId == donor.DonorId));
        }
    }
}