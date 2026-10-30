using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using AutoFixture;
using AwesomeAssertions;
using Dapper;
using Microsoft.Data.SqlClient;
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

        // ATL-495 experiment only: the hash partitions of SubjectGenotypeSetValues.

        [TestCase(8)]
        [TestCase(16)]
        [TestCase(32)]
        public async Task RemoveAllPrecomputedGenotypeSets_WithBuckets_PartitionsTheValuesTable_AndKeepsTheHashKeyUnique(int bucketCount)
        {
            try
            {
                await InsertSubjectGenotypeSetRows(dormantConnectionString);

                await donorImportRepository.RemoveAllPrecomputedGenotypeSets(bucketCount);

                (await CountRows<SubjectGenotypeSetValue>(dormantConnectionString)).Should().Be(0);
                (await CountPartitions(dormantConnectionString, "PK_SubjectGenotypeSetValues")).Should().Be(bucketCount);
                (await CountPartitions(dormantConnectionString, UniqueIndexName)).Should().Be(1);

                var valueId = await InsertSubjectGenotypeSetRows(dormantConnectionString);
                await using var context = new ContextFactory().Create(dormantConnectionString);
                (await context.SubjectGenotypeSetValues.SingleAsync(v => v.Id == valueId)).HlaTypingKey.Should().Be("typing-key");

                var insertTheSameKeyAgain = () => InsertSubjectGenotypeSetRows(dormantConnectionString);
                await insertTheSameKeyAgain.Should().ThrowAsync<DbUpdateException>();
            }
            finally
            {
                await donorImportRepository.RemoveAllPrecomputedGenotypeSets(1);
            }
        }

        [Test]
        public async Task RemoveAllPrecomputedGenotypeSets_WithOneBucket_PutsBackTheShapeOfTheMigrations()
        {
            await donorImportRepository.RemoveAllPrecomputedGenotypeSets(16);

            await donorImportRepository.RemoveAllPrecomputedGenotypeSets(1);

            await using var connection = new SqlConnection(dormantConnectionString);
            (await CountPartitions(dormantConnectionString, "PK_SubjectGenotypeSetValues")).Should().Be(1);
            (await connection.ExecuteScalarAsync<int?>("SELECT COL_LENGTH('dbo.SubjectGenotypeSetValues', 'Bucket')")).Should().BeNull();
            (await connection.QueryAsync<string>(@"
                SELECT c.name FROM sys.index_columns ic
                JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                WHERE i.object_id = OBJECT_ID('dbo.SubjectGenotypeSetValues') AND i.is_primary_key = 1"))
                .Should().BeEquivalentTo("Id");
            (await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sys.partition_schemes WHERE name = 'PS_SubjectGenotypeSetValues_Bucket'"))
                .Should().Be(0);
        }

        [TestCase(0)]
        [TestCase(3)]
        [TestCase(64)]
        public void BuildReshapeSubjectGenotypeSetValuesSql_WithABucketCountThatDoesNotDivide32_Throws(int bucketCount)
        {
            var act = () => DonorImportRepository.BuildReshapeSubjectGenotypeSetValuesSql(bucketCount);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        private const string UniqueIndexName = "IX_SubjectGenotypeSetValues_HlaTypingKey_HaplotypeFrequencySetId_AllowedLociKey";

        private static async Task<int> CountPartitions(string connectionString, string indexName)
        {
            await using var connection = new SqlConnection(connectionString);
            return await connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM sys.partitions p
                JOIN sys.indexes i ON i.object_id = p.object_id AND i.index_id = p.index_id
                WHERE i.object_id = OBJECT_ID('dbo.SubjectGenotypeSetValues') AND i.name = @indexName", new { indexName });
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

        private static async Task<int> InsertSubjectGenotypeSetRows(string connectionString)
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
            return value.Id;
        }

        private static async Task<int> CountRows<T>(string connectionString) where T : class
        {
            await using var context = new ContextFactory().Create(connectionString);
            return await context.Set<T>().CountAsync();
        }
    }
}
