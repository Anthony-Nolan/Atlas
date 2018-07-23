using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Services;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests.Import
{
    public class DonorImportTests : IntegrationTestBase
    {
        private IDonorImportRepository importRepo;
        private IDonorInspectionRepository inspectionRepo;
        private IHlaUpdateService updateService;
        
        // We know the number of p-groups for a given hla string, from the in memory matching dictionary. If the underlying data changes, this may become incorrect.
        private readonly Tuple<string, int> AHlaWithKnownPGroups1 = new Tuple<string, int>("01:XX", 207);

        [SetUp]
        public void ResolveSearchRepo()
        {
            importRepo = Container.Resolve<IDonorImportRepository>();
            inspectionRepo = Container.Resolve<IDonorInspectionRepository>();
            updateService = Container.Resolve<IHlaUpdateService>();
        }

        [Test]
        public async Task InsertBatchOfDonors_InsertsCorrectDonorData()
        {
            var inputDonors = new List<RawInputDonor> {NextDonor(), NextDonor()};
            await importRepo.InsertBatchOfDonors(inputDonors);

            var storedDonor1 = await inspectionRepo.GetDonor(inputDonors.First().DonorId);
            var storedDonor2 = await inspectionRepo.GetDonor(inputDonors.Last().DonorId);
            AssertStoredDonorInfoMatchesOriginalDonorInfo(storedDonor1, inputDonors.Single(d => d.DonorId == inputDonors.First().DonorId));
            AssertStoredDonorInfoMatchesOriginalDonorInfo(storedDonor2, inputDonors.Single(d => d.DonorId == inputDonors.Last().DonorId));
        }

        #region UpdateDonorHla
        
        [Test]
        public async Task UpdateDonorHla_DoesNotUpdateStoredDonorInformation()
        {
            var inputDonor = NextDonor();
            await importRepo.InsertBatchOfDonors(new List<RawInputDonor> {inputDonor});

            await updateService.UpdateDonorHla();

            var storedDonor = await inspectionRepo.GetDonor(inputDonor.DonorId);
            AssertStoredDonorInfoMatchesOriginalDonorInfo(storedDonor, inputDonor);
        }

        [Test]
        public async Task UpdateDonorHla_ForPatientHlaMatchingMultiplePGroups_InsertsMatchRowForEachPGroup()
        {
            var inputDonor = NextDonor();
            await importRepo.InsertBatchOfDonors(new List<RawInputDonor> {inputDonor});

            await updateService.UpdateDonorHla();

            var pGroups = await inspectionRepo.GetPGroupsForDonor(inputDonor.DonorId);
            pGroups.A_1.Count().Should().Be(AHlaWithKnownPGroups1.Item2);
        }
        
        #endregion
        
        private static void AssertStoredDonorInfoMatchesOriginalDonorInfo(DonorResult donorActual, RawInputDonor donorExpected)
        {
            donorActual.DonorId.Should().Be(donorExpected.DonorId);
            donorActual.DonorType.Should().Be(donorExpected.DonorType);
            donorActual.RegistryCode.Should().Be(donorExpected.RegistryCode);
            donorActual.HlaNames.ShouldBeEquivalentTo(donorExpected.HlaNames);
        }

        private RawInputDonor DonorWithId(int id)
        {
            return new RawInputDonor
            {
                RegistryCode = RegistryCode.DKMS,
                DonorType = DonorType.Cord,
                DonorId = id,
                HlaNames = new PhenotypeInfo<string>
                {
                    A_1 = AHlaWithKnownPGroups1.Item1,
                    A_2 = "30:02:01:01",
                    B_1 = "07:02",
                    B_2 = "08:01",
                    DRB1_1 = "01:11",
                    DRB1_2 = "03:41",
                }
            };
        }

        /// <returns> Donor with default information, and an auto-incremented donorId to avoid duplicates in the test DB</returns>
        private RawInputDonor NextDonor()
        {
            var donor = DonorWithId(DonorIdGenerator.NextId());
            return donor;
        }
    }
}