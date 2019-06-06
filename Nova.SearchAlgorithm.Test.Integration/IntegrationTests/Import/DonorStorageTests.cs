using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests.Import
{
    public class DonorStorageTests
    {
        private IDonorImportRepository importRepo;
        private IDonorInspectionRepository inspectionRepo;

        private readonly InputDonorWithExpandedHla donorWithAlleles = new InputDonorWithExpandedHla
        {
            RegistryCode = RegistryCode.DKMS,
            DonorType = DonorType.Cord,
            MatchingHla = new PhenotypeInfo<ExpandedHla>
            {
                A =
                {
                    Position1 = new ExpandedHla {OriginalName = "01:02", PGroups = new List<string> {"01:01P", "01:02"}},
                    Position2 = new ExpandedHla {OriginalName = "30:02", PGroups = new List<string> {"01:01P", "30:02P"}},
                },
                B =
                {
                    Position1 = new ExpandedHla {OriginalName = "07:02", PGroups = new List<string> {"07:02P"}},
                    Position2 = new ExpandedHla {OriginalName = "08:01", PGroups = new List<string> {"08:01P"}},
                },
                Drb1 =
                {
                    Position1 = new ExpandedHla {OriginalName = "01:11", PGroups = new List<string> {"01:11P"}},
                    Position2 = new ExpandedHla {OriginalName = "03:41", PGroups = new List<string> {"03:41P"}},
                }
            }
        };

        private readonly InputDonorWithExpandedHla donorWithXxCodes = new InputDonorWithExpandedHla
        {
            RegistryCode = RegistryCode.AN,
            DonorType = DonorType.Cord,
            MatchingHla = new PhenotypeInfo<ExpandedHla>
            {
                A =
                {
                    Position1 = new ExpandedHla {OriginalName = "*01:XX", PGroups = new List<string> {"01:01P", "01:02"}},
                    Position2 = new ExpandedHla {OriginalName = "30:XX", PGroups = new List<string> {"01:01P", "30:02P"}},
                },
                B =
                {
                    Position1 = new ExpandedHla {OriginalName = "*07:XX", PGroups = new List<string> {"07:02P"}},
                    Position2 = new ExpandedHla {OriginalName = "08:XX", PGroups = new List<string> {"08:01P"}},
                },
                Drb1 =
                {
                    Position1 = new ExpandedHla {OriginalName = "*01:XX", PGroups = new List<string> {"01:11P"}},
                    Position2 = new ExpandedHla {OriginalName = "03:XX", PGroups = new List<string> {"03:41P"}},
                }
            }
        };

        [SetUp]
        public void ResolveSearchRepo()
        {
            importRepo = DependencyInjection.DependencyInjection.Provider.GetService<IDonorImportRepository>();
            inspectionRepo = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
        }

        [Test]
        public async Task AddOrUpdateDonorWithHla_ForDonorWithAlleles_InsertsDonorInfoCorrectly()
        {
            var donor = donorWithAlleles;
            donor.DonorId = DonorIdGenerator.NextId();
            await importRepo.InsertDonorWithExpandedHla(donor);

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donor, result);
        }

        [Test]
        public async Task AddOrUpdateDonorWithHla_ForDonorWithXXCodes_InsertsDonorInfoCorrectly()
        {
            var donor = donorWithXxCodes;
            donor.DonorId = DonorIdGenerator.NextId();
            await importRepo.InsertDonorWithExpandedHla(donor);

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donor, result);
        }

        [Test]
        public async Task InsertBatchOfDonors_ForDonorWithAlleles_InsertsDonorInfoCorrectly()
        {
            var donor = donorWithAlleles;
            donor.DonorId = DonorIdGenerator.NextId();
            await importRepo.InsertBatchOfDonors(new List<InputDonor> {donor.ToInputDonor()});

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donorWithAlleles, result);
        }

        [Test]
        public async Task InsertBatchOfDonors_ForDonorWithXXCodes_InsertsDonorInfoCorrectly()
        {
            var donor = donorWithXxCodes;
            donor.DonorId = DonorIdGenerator.NextId();
            await importRepo.InsertBatchOfDonors(new List<InputDonor> {donor.ToInputDonor()});

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donorWithXxCodes, result);
        }

        private static void AssertStoredDonorInfoMatchesOriginalDonorInfo(InputDonorWithExpandedHla expectedDonor, DonorResult actualDonor)
        {
            actualDonor.DonorId.Should().Be(expectedDonor.DonorId);
            actualDonor.DonorType.Should().Be(expectedDonor.DonorType);
            actualDonor.RegistryCode.Should().Be(expectedDonor.RegistryCode);

            actualDonor.HlaNames.ShouldBeEquivalentTo(expectedDonor.MatchingHla.Map((l, p, hla) => hla?.OriginalName));
        }
    }
}