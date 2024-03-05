using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.DonorUpdates;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Injection = Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection.DependencyInjection;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests
{
    public class DonorManagementServiceTests
    {
        private const int DonorId = 1;

        private IDonorManagementService service;
        private IDonorManagementLogRepository donorManagementLogRepository;

        private TransientDatabase activeDb;


        [SetUp]
        public async Task SetUp()
        {
            service = Injection.Provider.GetService<IDonorManagementService>();
            donorManagementLogRepository = Injection.Provider.GetService<IActiveRepositoryFactory>().GetDonorManagementLogRepository();

            DatabaseManager.ClearTransientDatabases();

            var dbProvider = Injection.Provider.GetService<IActiveDatabaseProvider>();
            activeDb = dbProvider.GetActiveDatabase();
        }


        [Test]
        public async Task ApplyDonorUpdatesToDatabase_WhenDonorHlaIsInvalid_DonorManagementLogIsntUpdated()
        {
            var expectedSequenceNumber = await AddUpdateDatabaseWithDonor();
            var update = new DonorInfoBuilder(DonorId)
                .WithHlaAtLocus(Atlas.Common.Public.Models.GeneticData.Locus.A, Atlas.Common.Public.Models.GeneticData.LocusPosition.One, "*01:ZZZZZZZ")
                .Build()
                .ToUpdate();

            if (update.UpdateSequenceNumber == expectedSequenceNumber)
                update.UpdateSequenceNumber++;

            // Act
            await service.ApplyDonorUpdatesToDatabase(new[] { update }, activeDb, FileBackedHlaMetadataRepositoryBaseReader.NewerTestsHlaVersion, false);

            var logEntry = (await donorManagementLogRepository.GetDonorManagementLogBatch(new[] { DonorId })).Single();
            logEntry.SequenceNumberOfLastUpdate.Should().Be(expectedSequenceNumber);
        }

        private async Task<long> AddUpdateDatabaseWithDonor()
        {
            var update = new DonorInfoBuilder(DonorId).Build().ToUpdate();
            var sequenceNumber = update.UpdateSequenceNumber;

            await service.ApplyDonorUpdatesToDatabase(new[] { update }, activeDb, FileBackedHlaMetadataRepositoryBaseReader.NewerTestsHlaVersion, false);

            return sequenceNumber;
        }
    }
}
