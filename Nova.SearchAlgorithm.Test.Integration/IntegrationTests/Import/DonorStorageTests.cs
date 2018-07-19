using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests.Import
{
    public class DonorStorageTests : IntegrationTestBase
    {
        private IDonorImportRepository importRepo;
        private IDonorInspectionRepository inspectionRepo;

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
            importRepo = Container.Resolve<IDonorImportRepository>();
            inspectionRepo = Container.Resolve<IDonorInspectionRepository>();
        }

        [Test]
        public async Task AddOrUpdateDonorWithHla_ForDonorWithAlleles_InsertsDonorInfoCorrectly()
        {
            var donor = donorWithAlleles;
            donor.DonorId = DonorIdGenerator.NextId();
            await importRepo.AddOrUpdateDonorWithHla(donor);

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donor, result);
        }

        [Test]
        public async Task AddOrUpdateDonorWithHla_ForDonorWithXXCodes_InsertsDonorInfoCorrectly()
        {
            var donor = donorWithXxCodes;
            donor.DonorId = DonorIdGenerator.NextId();
            await importRepo.AddOrUpdateDonorWithHla(donor);

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donor, result);
        }

        [Test]
        public async Task InsertBatchOfDonors_ForDonorWithAlleles_InsertsDonorInfoCorrectly()
        {
            var donor = donorWithAlleles;
            donor.DonorId = DonorIdGenerator.NextId();
            await importRepo.InsertBatchOfDonors(new List<RawInputDonor>{donor.ToRawInputDonor()});

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donorWithAlleles, result);
        }

        [Test]
        public async Task InsertBatchOfDonors_ForDonorWithXXCodes_InsertsDonorInfoCorrectly()
        {
            var donor = donorWithXxCodes;
            donor.DonorId = DonorIdGenerator.NextId();
            await importRepo.InsertBatchOfDonors(new List<RawInputDonor>{donor.ToRawInputDonor()});

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donorWithXxCodes, result);
        }
        
        private static void AssertStoredDonorInfoMatchesOriginalDonorInfo(InputDonor expectedDonor, DonorResult actualDonor)
        {
            actualDonor.DonorId.Should().Be(expectedDonor.DonorId);
            actualDonor.DonorType.Should().Be(expectedDonor.DonorType);
            actualDonor.RegistryCode.Should().Be(expectedDonor.RegistryCode);

            actualDonor.HlaNames.ShouldBeEquivalentTo(expectedDonor.MatchingHla.Map((l, p, hla) => hla?.OriginalName));
        }
    }
}
