using System;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ContextFactory = Atlas.MatchingAlgorithm.Data.Context.ContextFactory;
using Injection = Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection.DependencyInjection;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Import
{
    [NonParallelizable]
    public class DonorImportRepositoryTests
    {
        private Fixture fixture;
        private IDonorImportRepository donorImportRepository;
        private string dormantConnectionString;
        private string activeConnectionString;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();
            DatabaseManager.ClearTransientDatabases();

            donorImportRepository = Injection.Provider.GetService<IDormantRepositoryFactory>().GetDonorImportRepository();
            dormantConnectionString = Injection.Provider.GetService<DormantTransientSqlConnectionStringProvider>().GetConnectionString();
            activeConnectionString = Injection.Provider.GetService<ActiveTransientSqlConnectionStringProvider>().GetConnectionString();
        }

        [Test]
        public async Task RemoveAllDonorInformation_TruncatesSubjectGenotypeSetTables()
        {
            await InsertSubjectGenotypeSetRows(dormantConnectionString);

            await donorImportRepository.RemoveAllDonorInformation();

            (await CountRows<SubjectGenotypeSetValue>(dormantConnectionString)).Should().Be(0);
            (await CountRows<DonorSubjectGenotypeSet>(dormantConnectionString)).Should().Be(0);
        }

        [Test]
        public async Task RemoveAllDonorInformation_DoesNotTruncateSubjectGenotypeSetTablesOnActiveDatabase()
        {
            await InsertSubjectGenotypeSetRows(activeConnectionString);

            await donorImportRepository.RemoveAllDonorInformation();

            (await CountRows<SubjectGenotypeSetValue>(activeConnectionString)).Should().Be(1);
            (await CountRows<DonorSubjectGenotypeSet>(activeConnectionString)).Should().Be(1);
        }

        [Test]
        public async Task RemoveAllDonorInformation_TruncatesDonorGenotypePrecomputationTables()
        {
            // The build gives the group ids from 1, so the groups of a run that stopped part way would collide with the
            // groups of the next run on this database.
            await InsertDonorGenotypePrecomputationRows(dormantConnectionString);

            await donorImportRepository.RemoveAllDonorInformation();

            (await CountRows<DonorGenotypePrecomputationRun>(dormantConnectionString)).Should().Be(0);
            (await CountRows<DonorGenotypePrecomputationBatch>(dormantConnectionString)).Should().Be(0);
            (await CountRows<DonorGenotypePrecomputationGroup>(dormantConnectionString)).Should().Be(0);
            (await CountRows<DonorGenotypePrecomputationGroupDonor>(dormantConnectionString)).Should().Be(0);
        }

        [Test]
        public async Task RemoveAllDonorInformation_DoesNotTruncateDonorGenotypePrecomputationTablesOnActiveDatabase()
        {
            await InsertDonorGenotypePrecomputationRows(activeConnectionString);

            await donorImportRepository.RemoveAllDonorInformation();

            (await CountRows<DonorGenotypePrecomputationRun>(activeConnectionString)).Should().Be(1);
            (await CountRows<DonorGenotypePrecomputationBatch>(activeConnectionString)).Should().Be(1);
            (await CountRows<DonorGenotypePrecomputationGroup>(activeConnectionString)).Should().Be(1);
            (await CountRows<DonorGenotypePrecomputationGroupDonor>(activeConnectionString)).Should().Be(1);
        }

        private async Task InsertDonorGenotypePrecomputationRows(string connectionString)
        {
            await using var context = new ContextFactory().Create(connectionString);

            var run = new DonorGenotypePrecomputationRun
            {
                DataRefreshRecordId = fixture.Create<int>(),
                // The column holds 32 characters; a whole AutoFixture string is 36.
                HlaNomenclatureVersion = fixture.Create<string>()[..8],
                Status = DonorGenotypePrecomputationRunStatus.Cancelled,
                GroupsPerBatch = 1,
                CreatedUtc = DateTime.UtcNow,
                StatusDateUtc = DateTime.UtcNow
            };
            context.DonorGenotypePrecomputationRuns.Add(run);
            await context.SaveChangesAsync();

            var group = new DonorGenotypePrecomputationGroup
            {
                Id = fixture.Create<int>(),
                RunId = run.Id,
                AllowedLociKey = fixture.Create<AllowedLociKey>(),
                RepresentativeDonorId = fixture.Create<int>(),
                DonorCount = 1
            };
            context.DonorGenotypePrecomputationGroups.Add(group);
            context.DonorGenotypePrecomputationGroupDonors.Add(new DonorGenotypePrecomputationGroupDonor
            {
                GroupId = group.Id,
                DonorId = group.RepresentativeDonorId
            });
            context.DonorGenotypePrecomputationBatches.Add(new DonorGenotypePrecomputationBatch
            {
                RunId = run.Id,
                BatchNumber = fixture.Create<int>(),
                FirstGroupId = group.Id,
                LastGroupId = group.Id,
                GroupCount = 1,
                DonorAssignmentCount = 1,
                Status = DonorGenotypePrecomputationBatchStatus.Pending,
                StatusDateUtc = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        private static async Task InsertSubjectGenotypeSetRows(string connectionString)
        {
            await using var context = new ContextFactory().Create(connectionString);

            var value = new SubjectGenotypeSetValue
            {
                HlaTypingKey = "typing-key",
                HaplotypeFrequencySetId = 1,
                AllowedLociKey = AllowedLociKey.ABCDrb1Dqb1,
                IsUnrepresented = false,
                SubjectGenotypeSetData = new byte[] { 1, 2, 3 }
            };
            context.SubjectGenotypeSetValues.Add(value);
            await context.SaveChangesAsync();

            context.DonorSubjectGenotypeSets.Add(new DonorSubjectGenotypeSet
            {
                DonorId = 1,
                AllowedLociKey = AllowedLociKey.ABCDrb1Dqb1,
                SubjectGenotypeSetValueId = value.Id
            });
            await context.SaveChangesAsync();
        }

        private static async Task<int> CountRows<T>(string connectionString) where T : class
        {
            await using var context = new ContextFactory().Create(connectionString);
            return await context.Set<T>().CountAsync();
        }
    }
}
