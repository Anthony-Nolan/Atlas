using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories.DonorRetrieval;
using Nova.SearchAlgorithm.Common.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Data.Extensions;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests.Import
{
    public class DonorStorageTests
    {
        private IDonorImportRepository donorImportRepository;
        private IDonorUpdateRepository donorUpdateRepository;
        private IDonorInspectionRepository inspectionRepo;

        private readonly InputDonorWithExpandedHla donorWithAllelesAtThreeLoci = new InputDonorWithExpandedHla
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

        private readonly InputDonorWithExpandedHla donorWithXxCodesAtThreeLoci = new InputDonorWithExpandedHla
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
            var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IDormantRepositoryFactory>();
            // By default donor update and import will happen on different databases - override this for these tests so the same database is used throughout
            donorImportRepository = repositoryFactory.GetDonorImportRepository();
            donorUpdateRepository = repositoryFactory.GetDonorUpdateRepository();
            inspectionRepo = repositoryFactory.GetDonorInspectionRepository();
        }

        [Test]
        public async Task InsertBatchOfDonorsWithExpandedHla_ForDonorWithAlleles_InsertsDonorInfoCorrectly()
        {
            var donor = donorWithAllelesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] { donor });

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donor, result);
        }

        [Test]
        public void InsertBatchOfDonorsWithExpandedHla_ForNewDonor_ForDonorWithUntypedRequiredLocus_ThrowsException()
        {
            var donor = new InputDonorWithExpandedHlaBuilder(DonorIdGenerator.NextId()).Build();

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] { donor }));
        }

        [Test]
        public async Task InsertBatchOfDonorsWithExpandedHla_ForNewDonor_ForDonorWithUntypedOptionalLocus_InsertsUntypedLocusAsNull()
        {
            var donor = donorWithAllelesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] { donor });

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            result.HlaNames.C.Position1.Should().BeNull();
        }

        [Test]
        public async Task InsertBatchOfDonorsWithExpandedHla_ForDonorWithXXCodes_InsertsDonorInfoCorrectly()
        {
            var donor = donorWithXxCodesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] { donor });

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donor, result);
        }

        [Test]
        public async Task InsertBatchOfDonors_ForDonorWithAlleles_InsertsDonorInfoCorrectly()
        {
            var donor = donorWithAllelesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            await donorImportRepository.InsertBatchOfDonors(new List<InputDonor> { donor.ToInputDonor() });

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donorWithAllelesAtThreeLoci, result);
        }

        [Test]
        public async Task InsertBatchOfDonors_ForDonorWithXXCodes_InsertsDonorInfoCorrectly()
        {
            var donor = donorWithXxCodesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            await donorImportRepository.InsertBatchOfDonors(new List<InputDonor> { donor.ToInputDonor() });

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donorWithXxCodesAtThreeLoci, result);
        }

        [Test]
        public void InsertBatchOfDonors_ForDonorWithUntypedRequiredLocus_ThrowsException()
        {
            var donor = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(Locus.A, TypePosition.One, null)
                .WithHlaAtLocus(Locus.A, TypePosition.Two, null)
                .Build();

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await donorImportRepository.InsertBatchOfDonors(new List<InputDonor> { donor }));
        }

        [Test]
        public async Task InsertBatchOfDonors_ForDonorWithUntypedOptionalLocus_InsertsUntypedLocusAsNull()
        {
            var donor = donorWithXxCodesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            donor.MatchingHla.C.Position1 = null;
            await donorImportRepository.InsertBatchOfDonors(new List<InputDonor> { donor.ToInputDonor() });

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            result.HlaNames.C.Position1.Should().BeNull();
        }

        [Test]
        public async Task UpdateDonorBatch_ForDonorWithAlleles_InsertsDonorInfoCorrectly()
        {
            var donorId = DonorIdGenerator.NextId();
            var donor = donorWithAllelesAtThreeLoci;
            donor.DonorId = donorId;
            await donorImportRepository.InsertBatchOfDonors(new List<InputDonor> { new InputDonorBuilder(donorId).Build() });
            await donorUpdateRepository.UpdateDonorBatch(new List<InputDonorWithExpandedHla> { donor });

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donorWithAllelesAtThreeLoci, result);
        }

        [Test]
        public async Task UpdateDonorBatch_ForDonorWithXXCodes_InsertsDonorInfoCorrectly()
        {
            var donorId = DonorIdGenerator.NextId();
            var donor = donorWithXxCodesAtThreeLoci;
            donor.DonorId = donorId;
            await donorImportRepository.InsertBatchOfDonors(new List<InputDonor> { new InputDonorBuilder(donorId).Build() });
            await donorUpdateRepository.UpdateDonorBatch(new List<InputDonorWithExpandedHla> { donor });

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donorWithXxCodesAtThreeLoci, result);
        }

        [Test]
        public async Task UpdateDonorBatch_WithUntypedRequiredLocus_ThrowsException()
        {
            // arbitrary hla at all loci, as the value of the hla does not matter for this test case
            var expandedHla = new ExpandedHla { OriginalName = "01:02", PGroups = new List<string> { "01:01P", "01:02" } };
            var donor = new InputDonorWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithMatchingHla(new PhenotypeInfo<ExpandedHla>(expandedHla))
                .Build();
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] { donor });

            donor.MatchingHla.A.Position1 = null;

            Assert.ThrowsAsync<SqlException>(async () =>
                await donorUpdateRepository.UpdateDonorBatch(new[] { donor }));
        }

        [Test]
        public async Task UpdateDonorBatch_WithUntypedOptionalLocus_UpdatesUntypedLocusAsNull()
        {
            // arbitrary hla at all loci, as the value of the hla does not matter for this test case
            var expandedHla = new ExpandedHla { OriginalName = "01:02", PGroups = new List<string> { "01:01P", "01:02" } };
            var donor = new InputDonorWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithMatchingHla(new PhenotypeInfo<ExpandedHla>(expandedHla))
                .Build();
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] { donor });

            donor.MatchingHla.Dqb1.Position1 = null;
            await donorUpdateRepository.UpdateDonorBatch(new[] { donor });

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            result.HlaNames.Dqb1.Position1.Should().BeNull();
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