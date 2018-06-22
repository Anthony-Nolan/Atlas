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

        private readonly InputDonor donorWithAlleles = new InputDonor
        {
            RegistryCode = RegistryCode.DKMS,
            DonorType = DonorType.Cord,
            MatchingHla = new PhenotypeInfo<ExpandedHla>
            {
                A_1 = new ExpandedHla { OriginalName = "01:02", PGroups = new List<string> { "01:01P", "01:02" } },
                A_2 = new ExpandedHla { OriginalName = "30:02", PGroups = new List<string> { "01:01P", "30:02P" } },
                B_1 = new ExpandedHla { OriginalName = "07:02", PGroups = new List<string> { "07:02P" } },
                B_2 = new ExpandedHla { OriginalName = "08:01", PGroups = new List<string> { "08:01P" } },
                DRB1_1 = new ExpandedHla { OriginalName = "01:11", PGroups = new List<string> { "01:11P" } },
                DRB1_2 = new ExpandedHla { OriginalName = "03:41", PGroups = new List<string> { "03:41P" } }
            }
        };

        private readonly InputDonor donorWithXxCodes = new InputDonor
        {
            RegistryCode = RegistryCode.AN,
            DonorType = DonorType.Cord,
            MatchingHla = new PhenotypeInfo<ExpandedHla>
            {
                A_1 = new ExpandedHla { OriginalName = "*01:XX", PGroups = new List<string> { "01:01P", "01:02" } },
                A_2 = new ExpandedHla { OriginalName = "30:XX", PGroups = new List<string> { "01:01P", "30:02P" } },
                B_1 = new ExpandedHla { OriginalName = "*07:XX", PGroups = new List<string> { "07:02P" } },
                B_2 = new ExpandedHla { OriginalName = "08:XX", PGroups = new List<string> { "08:01P" } },
                DRB1_1 = new ExpandedHla { OriginalName = "*01:XX", PGroups = new List<string> { "01:11P" } },
                DRB1_2 = new ExpandedHla { OriginalName = "03:XX", PGroups = new List<string> { "03:41P" } }
            }
        };

        [SetUp]
        public void ResolveSearchRepo()
        {
            importRepo = container.Resolve<IDonorImportRepository>();
            inspectionRepo = container.Resolve<IDonorInspectionRepository>();
        }

        [Test]
        public async Task AddOrUpdateThenRetrieveAlleleDonor()
        {
            await AddOrUpdateThenRetrieveDonor(11, donorWithAlleles);
        }

        [Test]
        public async Task AddOrUpdateThenRetrieveXxCodeDonor()
        {
            await AddOrUpdateThenRetrieveDonor(12, donorWithXxCodes);
        }

        private async Task AddOrUpdateThenRetrieveDonor(int donorId, InputDonor donor)
        {
            donor.DonorId = donorId;

            await importRepo.AddOrUpdateDonorWithHla(donor);

            var result = await inspectionRepo.GetDonor(donorId);

            result.DonorId.Should().Be(donorId);
            result.DonorType.Should().Be(donor.DonorType);
            result.RegistryCode.Should().Be(donor.RegistryCode);

            result.HlaNames.ShouldBeEquivalentTo(donor.MatchingHla.Map((l, p, hla) => hla?.OriginalName));
        }

        [Test]
        public async Task InsertThenRetrieveAlleleDonor()
        {
            await InsertThenRetrieveDonor(13, donorWithAlleles);
        }

        [Test]
        public async Task InsertThenRetrieveXxCodeDonor()
        {
            await InsertThenRetrieveDonor(14, donorWithXxCodes);
        }

        private async Task InsertThenRetrieveDonor(int donorId, InputDonor donor)
        {
            donor.DonorId = donorId;

            await importRepo.InsertDonor(donor.ToRawInputDonor());

            var result = await inspectionRepo.GetDonor(donorId);

            result.DonorId.Should().Be(donorId);
            result.DonorType.Should().Be(donor.DonorType);
            result.RegistryCode.Should().Be(donor.RegistryCode);

            result.HlaNames.ShouldBeEquivalentTo(donor.MatchingHla.Map((l, p, hla) => hla?.OriginalName));
        }
    }
}
