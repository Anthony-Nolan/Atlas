using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Data.Models;

namespace Nova.SearchAlgorithm.Test.Integration
{
    public class DonorStorageTests : IntegrationTestBase
    {
        private IDonorImportRepository importRepo;
        private IDonorInspectionRepository inspectionRepo;

        public DonorStorageTests(DonorStorageImplementation param) : base(param) { }

        [SetUp]
        public void ResolveSearchRepo()
        {
            importRepo = container.Resolve<IDonorImportRepository>();
            inspectionRepo = container.Resolve<IDonorInspectionRepository>();
        }
        
        [Test]
        public async Task ImportThenRetrieveDonor()
        {
            const int donorId = 231;
            InputDonor original = new InputDonor
            {
                RegistryCode = RegistryCode.DKMS,
                DonorType = DonorType.Cord,
                DonorId = donorId,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { Name = "01:02", PGroups = new List<string> {"01:01P", "01:02"} },
                    A_2 = new ExpandedHla { Name = "30:02", PGroups = new List<string> {"01:01P", "30:02P"} },
                    B_1 = new ExpandedHla { Name = "07:02", PGroups = new List<string> {"07:02P"} },
                    B_2 = new ExpandedHla { Name = "08:01", PGroups = new List<string> {"08:01P"} },
                    DRB1_1 = new ExpandedHla { Name = "01:11", PGroups = new List<string> {"01:11P"} },
                    DRB1_2 = new ExpandedHla { Name = "03:41",  PGroups = new List<string> {"03:41P"} }
                }
            };

            await importRepo.AddOrUpdateDonorWithHla(original);

            DonorResult result = await inspectionRepo.GetDonor(donorId);

            result.DonorId.Should().Be(donorId);
            result.DonorType.Should().Be(original.DonorType);
            result.RegistryCode.Should().Be(original.RegistryCode);

            result.HlaNames.ShouldBeEquivalentTo(original.MatchingHla.Map((l, p, hla) => hla?.Name));
        }
    }
}
