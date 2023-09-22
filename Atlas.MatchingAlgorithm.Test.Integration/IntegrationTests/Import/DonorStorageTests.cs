using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using SqlException = Microsoft.Data.SqlClient.SqlException;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Import
{
    [NonParallelizable]
    public class DonorStorageTests
    {
        private IDonorImportRepository donorImportRepository;
        private IDonorUpdateRepository donorUpdateRepository;
        private IDonorInspectionRepository inspectionRepo;

        private readonly DonorInfoWithExpandedHla donorInfoWithAllelesAtThreeLoci = new DonorInfoWithExpandedHla
        {
            DonorType = DonorType.Cord,
            ExternalDonorCode = Guid.NewGuid().ToString(),
            HlaNames = new PhenotypeInfo<string>
            (
                valueA: new LocusInfo<string>("01:02", "30:02"),
                valueB: new LocusInfo<string>("07:02", "08:01"),
                valueDrb1: new LocusInfo<string>("01:11", "03:41")
            ),
            MatchingHla = new PhenotypeInfo<INullHandledHlaMatchingMetadata>
            (
                valueA: new LocusInfo<INullHandledHlaMatchingMetadata>
                (
                    new TestHlaMetadata {LookupName = "01:02", MatchingPGroups = new List<string> {"01:01P", "01:02"}},
                    new TestHlaMetadata {LookupName = "30:02", MatchingPGroups = new List<string> {"01:01P", "30:02P"}}
                ),
                valueB: new LocusInfo<INullHandledHlaMatchingMetadata>
                (
                    new TestHlaMetadata {LookupName = "07:02", MatchingPGroups = new List<string> {"07:02P"}},
                    new TestHlaMetadata {LookupName = "08:01", MatchingPGroups = new List<string> {"08:01P"}}
                ),
                valueDrb1: new LocusInfo<INullHandledHlaMatchingMetadata>
                (
                    new TestHlaMetadata {LookupName = "01:11", MatchingPGroups = new List<string> {"01:11P"}},
                    new TestHlaMetadata {LookupName = "03:41", MatchingPGroups = new List<string> {"03:41P"}}
                )
            )
        };

        private readonly DonorInfoWithExpandedHla donorInfoWithXxCodesAtThreeLoci = new DonorInfoWithExpandedHla
        {
            DonorType = DonorType.Cord,
            ExternalDonorCode = Guid.NewGuid().ToString(),
            HlaNames = new PhenotypeInfo<string>
            (
                valueA: new LocusInfo<string>("*01:XX", "30:XX"),
                valueB: new LocusInfo<string>("*07:XX", "08:XX"),
                valueDrb1: new LocusInfo<string>("*01:XX", "03:XX")
            ),
            MatchingHla = new PhenotypeInfo<INullHandledHlaMatchingMetadata>
            (
                valueA: new LocusInfo<INullHandledHlaMatchingMetadata>
                (
                    new TestHlaMetadata {LookupName = "*01:XX", MatchingPGroups = new List<string> {"01:01P", "01:02"}},
                    new TestHlaMetadata {LookupName = "30:XX", MatchingPGroups = new List<string> {"01:01P", "30:02P"}}
                ),
                valueB: new LocusInfo<INullHandledHlaMatchingMetadata>
                (
                    new TestHlaMetadata {LookupName = "*07:XX", MatchingPGroups = new List<string> {"07:02P"}},
                    new TestHlaMetadata {LookupName = "08:XX", MatchingPGroups = new List<string> {"08:01P"}}
                ),
                valueDrb1: new LocusInfo<INullHandledHlaMatchingMetadata>
                (
                    new TestHlaMetadata {LookupName = "*01:XX", MatchingPGroups = new List<string> {"01:11P"}},
                    new TestHlaMetadata {LookupName = "03:XX", MatchingPGroups = new List<string> {"03:41P"}}
                )
            )
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
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] {donor}, false);

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donor, result);
        }

        [Test]
        public void InsertBatchOfDonorsWithExpandedHla_ForNewDonor_ForDonorWithUntypedRequiredLocus_ThrowsException()
        {
            var donor = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId()).Build();

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] {donor}, false));
        }

        [Test]
        public async Task InsertBatchOfDonorsWithExpandedHla_ForNewDonor_ForDonorWithUntypedOptionalLocus_InsertsUntypedLocusAsNull()
        {
            var donor = donorInfoWithAllelesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] {donor}, false);

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            result.HlaNames.C.Position1.Should().BeNull();
        }

        [Test]
        public async Task InsertBatchOfDonorsWithExpandedHla_ForDonorWithXXCodes_InsertsDonorInfoCorrectly()
        {
            var donor = donorInfoWithXxCodesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] {donor}, false);

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donor, result);
        }

        [Test]
        public async Task InsertBatchOfDonors_ForDonorWithAlleles_InsertsDonorInfoCorrectly()
        {
            var donor = donorInfoWithAllelesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> {donor});

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donorInfoWithAllelesAtThreeLoci, result);
        }

        [Test]
        public async Task InsertBatchOfDonors_ForDonorWithXXCodes_InsertsDonorInfoCorrectly()
        {
            var donor = donorInfoWithXxCodesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> {donor});

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donorInfoWithXxCodesAtThreeLoci, result);
        }

        [Test]
        public void InsertBatchOfDonors_ForDonorWithUntypedRequiredLocus_ThrowsException()
        {
            var donor = new DonorInfoBuilder()
                .WithHlaAtLocus(Locus.A, LocusPosition.One, null)
                .WithHlaAtLocus(Locus.A, LocusPosition.Two, null)
                .Build();

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> {donor}));
        }

        [Test]
        public async Task InsertBatchOfDonors_ForDonorWithUntypedOptionalLocus_InsertsUntypedLocusAsNull()
        {
            var donor = donorInfoWithXxCodesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            donor.MatchingHla = donor.MatchingHla.SetPosition(Locus.C, LocusPosition.One, null);
            await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> {donor});

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            result.HlaNames.C.Position1.Should().BeNull();
        }

        [Test]
        public async Task UpdateDonorBatch_ForDonorWithAlleles_UpdatesDonorInfoCorrectly()
        {
            var donorInfo = new DonorInfoBuilder().Build();
            var donor = donorInfoWithAllelesAtThreeLoci;
            donor.DonorId = donorInfo.DonorId;
            await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> {donorInfo});
            await donorUpdateRepository.UpdateDonorBatch(new List<DonorInfoWithExpandedHla> {donor}, false);

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donorInfoWithAllelesAtThreeLoci, result);
        }

        [Test]
        public async Task UpdateDonorBatch_ForDonorWithXXCodes_UpdatesDonorInfoCorrectly()
        {
            var donorInfo = new DonorInfoBuilder().Build();
            var donor = donorInfoWithXxCodesAtThreeLoci;
            donor.DonorId = donorInfo.DonorId;
            await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> {donorInfo});
            await donorUpdateRepository.UpdateDonorBatch(new List<DonorInfoWithExpandedHla> {donor}, false);

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donorInfoWithXxCodesAtThreeLoci, result);
        }

        [Test]
        public async Task UpdateDonorBatch_WithUntypedRequiredLocus_ThrowsException()
        {
            // arbitrary hla at all loci, as the value of the hla does not matter for this test case
            var expandedHla = new TestHlaMetadata {LookupName = "01:02", MatchingPGroups = new List<string> {"01:01P", "01:02"}};
            var donor = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(new PhenotypeInfo<INullHandledHlaMatchingMetadata>(expandedHla))
                .Build();
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] {donor}, false);

            donor.HlaNames = donor.HlaNames.SetPosition(Locus.A, LocusPosition.One, null);

            Assert.ThrowsAsync<SqlException>(async () =>
                await donorUpdateRepository.UpdateDonorBatch(new[] {donor}, false));
        }

        [Test]
        public async Task UpdateDonorBatch_WithUntypedOptionalLocus_UpdatesUntypedLocusAsNull()
        {
            // arbitrary hla at all loci, as the value of the hla does not matter for this test case
            var expandedHla = new TestHlaMetadata {LookupName = "01:02", MatchingPGroups = new List<string> {"01:01P", "01:02"}};
            var donor = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(new PhenotypeInfo<INullHandledHlaMatchingMetadata>(expandedHla))
                .Build();
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] {donor}, false);

            donor.HlaNames = donor.HlaNames.SetPosition(Locus.Dqb1, LocusPosition.One, null);
            await donorUpdateRepository.UpdateDonorBatch(new[] {donor}, false);

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            result.HlaNames.Dqb1.Position1.Should().BeNull();
        }

        private static void AssertStoredDonorInfoMatchesOriginalDonorInfo(DonorInfo expectedDonorInfo, DonorInfo actualDonorInfo)
        {
            actualDonorInfo.DonorId.Should().Be(expectedDonorInfo.DonorId);
            actualDonorInfo.DonorType.Should().Be(expectedDonorInfo.DonorType);
            actualDonorInfo.IsAvailableForSearch.Should().Be(expectedDonorInfo.IsAvailableForSearch);
            actualDonorInfo.HlaNames.Should().BeEquivalentTo(expectedDonorInfo.HlaNames);
        }
    }
}