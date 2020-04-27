using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Helpers;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Settings;
using Microsoft.Extensions.Options;
using NSubstitute;
using Locus = Atlas.MatchingAlgorithm.Common.Models.Locus;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Import
{
    public class HlaProcessorTests
    {
        private IDonorImportRepository importRepo;
        private IDonorInspectionRepository inspectionRepo;
        private IHlaProcessor processor;

        private const string DefaultHlaDatabaseVersion = "3330";

        [SetUp]
        public void SetUp()
        {
            var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IDormantRepositoryFactory>();
            importRepo = repositoryFactory.GetDonorImportRepository();
            // We want to inspect the dormant database, as this is what the import will have run on
            inspectionRepo = repositoryFactory.GetDonorInspectionRepository();
            processor = DependencyInjection.DependencyInjection.Provider.GetService<IHlaProcessor>();
        }

        [Test]
        public async Task UpdateDonorHla_DoesNotChangeStoredDonorInformation()
        {
            var donorInfo = new DonorInfoBuilder(DonorIdGenerator.NextId()).Build();
            await importRepo.InsertBatchOfDonors(new List<DonorInfo> { donorInfo });

            await processor.UpdateDonorHla(DefaultHlaDatabaseVersion);

            var storedDonor = await inspectionRepo.GetDonor(donorInfo.DonorId);
            AssertStoredDonorInfoMatchesOriginalDonorInfo(storedDonor, donorInfo);
        }

        [Test]
        public async Task UpdateDonorHla_DonorHlaHasMultiplePGroups_InsertsRowForEachPGroup()
        {
            // We know the number of p-groups for a given hla string from the in memory matching dictionary.
            // If the underlying data changes, this may become incorrect.
            const string hlaWithKnownPGroups = "01:XX";
            const int expectedPGroupCount = 213;

            var donorInfo = new DonorInfoBuilder(DonorIdGenerator.NextId())
                    .WithHlaAtLocus(Locus.A, TypePosition.One, hlaWithKnownPGroups)
                    .Build();

            await importRepo.InsertBatchOfDonors(new List<DonorInfo> { donorInfo });

            await processor.UpdateDonorHla(DefaultHlaDatabaseVersion);

            var actualPGroupCount = await GetPGroupCountAtLocusAPositionOne(donorInfo.DonorId);
            actualPGroupCount.Should().Be(expectedPGroupCount);
        }

        [Test]
        public async Task UpdateDonorHla_WhenHlaUpdateIsRerun_DoesNotAddMorePGroups()
        {
            var donorInfo = new DonorInfoBuilder(DonorIdGenerator.NextId()).Build();
            await importRepo.InsertBatchOfDonors(new List<DonorInfo> { donorInfo });

            await processor.UpdateDonorHla(DefaultHlaDatabaseVersion);

            var initialPGroupCount = await GetPGroupCountAtLocusAPositionOne(donorInfo.DonorId);

            await processor.UpdateDonorHla(DefaultHlaDatabaseVersion);

            var pGroupCount = await GetPGroupCountAtLocusAPositionOne(donorInfo.DonorId);
            pGroupCount.Should().Be(initialPGroupCount);
        }

        [Test]
        public async Task UpdateDonorHla_UpdatesHlaForDonorsInsertedSinceLastRun()
        {
            var donorInfo = new DonorInfoBuilder(DonorIdGenerator.NextId()).Build();
            await importRepo.InsertBatchOfDonors(new List<DonorInfo> { donorInfo });
            await processor.UpdateDonorHla(DefaultHlaDatabaseVersion);

            var newDonor = new DonorInfoBuilder(DonorIdGenerator.NextId()).Build();
            await importRepo.InsertBatchOfDonors(new List<DonorInfo> { newDonor });
            await processor.UpdateDonorHla(DefaultHlaDatabaseVersion);

            var pGroupCount = await GetPGroupCountAtLocusAPositionOne(newDonor.DonorId);
            pGroupCount.Should().NotBeNull();
        }

        private static void AssertStoredDonorInfoMatchesOriginalDonorInfo(DonorInfo donorInfoActual, DonorInfo donorInfoExpected)
        {
            donorInfoActual.DonorId.Should().Be(donorInfoExpected.DonorId);
            donorInfoActual.DonorType.Should().Be(donorInfoExpected.DonorType);
            donorInfoActual.IsAvailableForSearch.Should().Be(donorInfoExpected.IsAvailableForSearch);
            donorInfoActual.HlaNames.ShouldBeEquivalentTo(donorInfoExpected.HlaNames);
        }

        private async Task<int?> GetPGroupCountAtLocusAPositionOne(int donorId)
        {
            var pGroups = await inspectionRepo.GetPGroupsForDonors(new[] { donorId });

            return pGroups.First().PGroupNames.A.Position1?.Count();
        }
    }
}