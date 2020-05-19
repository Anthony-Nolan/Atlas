using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;
using Atlas.MatchingAlgorithm.Client.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Import
{
    public class DonorStorageTests
    {
        private IDonorImportRepository donorImportRepository;
        private IDonorUpdateRepository donorUpdateRepository;
        private IDonorInspectionRepository inspectionRepo;

        private readonly DonorInfoWithExpandedHla donorInfoWithAllelesAtThreeLoci = new DonorInfoWithExpandedHla
        {
            DonorType = DonorType.Cord,
            HlaNames = new PhenotypeInfo<string>
            {
                A =
                {
                    Position1 = "01:02",
                    Position2 = "30:02"
                },
                B =
                {
                    Position1 = "07:02",
                    Position2 = "08:01"
                },
                Drb1 =
                {
                    Position1 = "01:11",
                    Position2 = "03:41"
                }
            },
            MatchingHla = new PhenotypeInfo<IHlaMatchingLookupResult>
            {
                A =
                {
                    Position1 = new TestHla {LookupName = "01:02", MatchingPGroups = new List<string> {"01:01P", "01:02"}},
                    Position2 = new TestHla {LookupName = "30:02", MatchingPGroups = new List<string> {"01:01P", "30:02P"}},
                },
                B =
                {
                    Position1 = new TestHla {LookupName = "07:02", MatchingPGroups = new List<string> {"07:02P"}},
                    Position2 = new TestHla {LookupName = "08:01", MatchingPGroups = new List<string> {"08:01P"}},
                },
                Drb1 =
                {
                    Position1 = new TestHla {LookupName = "01:11", MatchingPGroups = new List<string> {"01:11P"}},
                    Position2 = new TestHla {LookupName = "03:41", MatchingPGroups = new List<string> {"03:41P"}},
                }
            }
        };

        private readonly DonorInfoWithExpandedHla donorInfoWithXxCodesAtThreeLoci = new DonorInfoWithExpandedHla
        {
            DonorType = DonorType.Cord,
            HlaNames = new PhenotypeInfo<string>
            {
                A =
                {
                    Position1 = "*01:XX",
                    Position2 = "30:XX"
                },
                B =
                {
                    Position1 = "*07:XX",
                    Position2 = "08:XX"
                },
                Drb1 =
                {
                    Position1 = "*01:XX",
                    Position2 = "03:XX"
                }
            },
            MatchingHla = new PhenotypeInfo<IHlaMatchingLookupResult>
            {
                A =
                {
                    Position1 = new TestHla {LookupName = "*01:XX", MatchingPGroups = new List<string> {"01:01P", "01:02"}},
                    Position2 = new TestHla {LookupName = "30:XX", MatchingPGroups = new List<string> {"01:01P", "30:02P"}},
                },
                B =
                {
                    Position1 = new TestHla {LookupName = "*07:XX", MatchingPGroups = new List<string> {"07:02P"}},
                    Position2 = new TestHla {LookupName = "08:XX", MatchingPGroups = new List<string> {"08:01P"}},
                },
                Drb1 =
                {
                    Position1 = new TestHla {LookupName = "*01:XX", MatchingPGroups = new List<string> {"01:11P"}},
                    Position2 = new TestHla {LookupName = "03:XX", MatchingPGroups = new List<string> {"03:41P"}},
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
            var donor = donorInfoWithAllelesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] { donor });

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donor, result);
        }

        [Test]
        public void InsertBatchOfDonorsWithExpandedHla_ForNewDonor_ForDonorWithUntypedRequiredLocus_ThrowsException()
        {
            var donor = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId()).Build();

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] { donor }));
        }

        [Test]
        public async Task InsertBatchOfDonorsWithExpandedHla_ForNewDonor_ForDonorWithUntypedOptionalLocus_InsertsUntypedLocusAsNull()
        {
            var donor = donorInfoWithAllelesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] { donor });

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            result.HlaNames.C.Position1.Should().BeNull();
        }

        [Test]
        public async Task InsertBatchOfDonorsWithExpandedHla_ForDonorWithXXCodes_InsertsDonorInfoCorrectly()
        {
            var donor = donorInfoWithXxCodesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] { donor });

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donor, result);
        }

        [Test]
        public async Task InsertBatchOfDonors_ForDonorWithAlleles_InsertsDonorInfoCorrectly()
        {
            var donor = donorInfoWithAllelesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> { donor });

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donorInfoWithAllelesAtThreeLoci, result);
        }

        [Test]
        public async Task InsertBatchOfDonors_ForDonorWithXXCodes_InsertsDonorInfoCorrectly()
        {
            var donor = donorInfoWithXxCodesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> { donor });

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donorInfoWithXxCodesAtThreeLoci, result);
        }

        [Test]
        public void InsertBatchOfDonors_ForDonorWithUntypedRequiredLocus_ThrowsException()
        {
            var donor = new DonorInfoBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(Locus.A, TypePosition.One, null)
                .WithHlaAtLocus(Locus.A, TypePosition.Two, null)
                .Build();

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> { donor }));
        }

        [Test]
        public async Task InsertBatchOfDonors_ForDonorWithUntypedOptionalLocus_InsertsUntypedLocusAsNull()
        {
            var donor = donorInfoWithXxCodesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            donor.MatchingHla.C.Position1 = null;
            await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> { donor });

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            result.HlaNames.C.Position1.Should().BeNull();
        }

        [Test]
        public async Task UpdateDonorBatch_ForDonorWithAlleles_UpdatesDonorInfoCorrectly()
        {
            var donorId = DonorIdGenerator.NextId();
            var donor = donorInfoWithAllelesAtThreeLoci;
            donor.DonorId = donorId;
            await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> { new DonorInfoBuilder(donorId).Build() });
            await donorUpdateRepository.UpdateDonorBatch(new List<DonorInfoWithExpandedHla> { donor });

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donorInfoWithAllelesAtThreeLoci, result);
        }

        [Test]
        public async Task UpdateDonorBatch_ForDonorWithXXCodes_UpdatesDonorInfoCorrectly()
        {
            var donorId = DonorIdGenerator.NextId();
            var donor = donorInfoWithXxCodesAtThreeLoci;
            donor.DonorId = donorId;
            await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> { new DonorInfoBuilder(donorId).Build() });
            await donorUpdateRepository.UpdateDonorBatch(new List<DonorInfoWithExpandedHla> { donor });

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donorInfoWithXxCodesAtThreeLoci, result);
        }

        [Test]
        public async Task UpdateDonorBatch_WithUntypedRequiredLocus_ThrowsException()
        {
            // arbitrary hla at all loci, as the value of the hla does not matter for this test case
            var expandedHla = new TestHla { LookupName = "01:02", MatchingPGroups = new List<string> { "01:01P", "01:02" } };
            var donor = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(new PhenotypeInfo<IHlaMatchingLookupResult>(expandedHla))
                .Build();
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] { donor });

            donor.HlaNames.A.Position1 = null;

            Assert.ThrowsAsync<SqlException>(async () =>
                await donorUpdateRepository.UpdateDonorBatch(new[] { donor }));
        }

        [Test]
        public async Task UpdateDonorBatch_WithUntypedOptionalLocus_UpdatesUntypedLocusAsNull()
        {
            // arbitrary hla at all loci, as the value of the hla does not matter for this test case
            var expandedHla = new TestHla { LookupName = "01:02", MatchingPGroups = new List<string> { "01:01P", "01:02" } };
            var donor = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(new PhenotypeInfo<IHlaMatchingLookupResult>(expandedHla))
                .Build();
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] { donor });

            donor.HlaNames.Dqb1.Position1 = null;
            await donorUpdateRepository.UpdateDonorBatch(new[] { donor });

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            result.HlaNames.Dqb1.Position1.Should().BeNull();
        }

        private static void AssertStoredDonorInfoMatchesOriginalDonorInfo(DonorInfo expectedDonorInfo, DonorInfo actualDonorInfo)
        {
            actualDonorInfo.DonorId.Should().Be(expectedDonorInfo.DonorId);
            actualDonorInfo.DonorType.Should().Be(expectedDonorInfo.DonorType);
            actualDonorInfo.IsAvailableForSearch.Should().Be(expectedDonorInfo.IsAvailableForSearch);
            actualDonorInfo.HlaNames.ShouldBeEquivalentTo(expectedDonorInfo.HlaNames);
        }
    }
}