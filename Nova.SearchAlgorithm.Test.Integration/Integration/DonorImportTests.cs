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

        private const int DonorId = 231;

        private readonly RawInputDonor original = new RawInputDonor
        {
            RegistryCode = RegistryCode.DKMS,
            DonorType = DonorType.Cord,
            DonorId = DonorId,
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

        public DonorImportTests(DonorStorageImplementation param) : base(param) { }

        [SetUp]
        public void ResolveSearchRepo()
        {
            importRepo = container.Resolve<IDonorImportRepository>();
            inspectionRepo = container.Resolve<IDonorInspectionRepository>();
            updateService = container.Resolve<IHlaUpdateService>();
        }
        
        [Test]
        public async Task ImportThenUpdateDonor()
        {
            await importRepo.InsertDonor(original);

            await ValidateDonor();
            
            await updateService.UpdateDonorHla();

            await ValidateDonor();
        }

        private async Task ValidateDonor()
        {
            DonorResult result = await inspectionRepo.GetDonor(DonorId);

            result.DonorId.Should().Be(DonorId);
            result.DonorType.Should().Be(original.DonorType);
            result.RegistryCode.Should().Be(original.RegistryCode);

            result.HlaNames.ShouldBeEquivalentTo(original.HlaNames);
        }
    }
}
