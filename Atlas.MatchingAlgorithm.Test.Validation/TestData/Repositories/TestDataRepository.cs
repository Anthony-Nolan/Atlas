using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Atlas.DonorImport.Data.Context;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchingAlgorithm.Data.Context;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Persistent.Context;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Repositories
{
    public interface ITestDataRepository
    {
        void SetupPersistentMatchingDatabase();
        void SetupTransientMatchingDatabase();
        void SetUpDonorDatabase();
        void AddTestDonors(IEnumerable<Donor> donors);

        /// <summary>
        /// Adds donors to the master Atlas Donor Store database.
        /// Does *not* add to the matching algorithm donor store, nor perform any donor processing.
        /// </summary>
        void AddDonorsToAtlasDonorStore(IEnumerable<int> donorIds);

        IEnumerable<Donor> GetDonors(IEnumerable<int> donorIds);
    }

    /// <summary>
    /// Repository layer for interacting with test SQL database
    /// </summary>
    public class TestDataRepository : ITestDataRepository
    {
        private readonly SearchAlgorithmContext transientSearchContext;
        private readonly SearchAlgorithmPersistentContext persistentContext;
        private readonly DonorContext donorContext;

        public TestDataRepository(
            SearchAlgorithmContext transientSearchContext,
            SearchAlgorithmPersistentContext persistentContext,
            DonorContext donorContext)
        {
            this.transientSearchContext = transientSearchContext;
            this.persistentContext = persistentContext;
            this.donorContext = donorContext;
        }

        public void SetupPersistentMatchingDatabase()
        {
            persistentContext.Database.Migrate();
            if (!persistentContext.DataRefreshRecords.Any())
            {
                persistentContext.DataRefreshRecords.Add(new DataRefreshRecord
                {
                    Database = TransientDatabase.DatabaseA.ToString(),
                    WasSuccessful = true,
                    RefreshRequestedUtc = DateTime.UtcNow,
                    RefreshEndUtc = DateTime.UtcNow,
                    HlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion
                });
                persistentContext.SaveChanges();
            }
        }

        public void SetupTransientMatchingDatabase()
        {
            // Ensure we have fresh data on each run. Done in setup rather than teardown to avoid data issues if the test suites are terminated early
            RemoveTransientMatchingTestData();
            transientSearchContext.Database.Migrate();
        }

        /// <inheritdoc />
        public void SetUpDonorDatabase()
        {
            RemoveDonorTestData();
            donorContext.Database.Migrate();
        }

        public void AddTestDonors(IEnumerable<Donor> donors)
        {
            donors = donors.ToList();

            foreach (var donor in donors)
            {
                if (!transientSearchContext.Donors.Any(d => d.DonorId == donor.DonorId))
                {
                    transientSearchContext.Donors.Add(donor);
                }
            }

            transientSearchContext.SaveChanges();

            AddDonorsToAtlasDonorStore(donors.Select(d => d.Id));
        }

        /// <inheritdoc />
        public void AddDonorsToAtlasDonorStore(IEnumerable<int> donorIds)
        {
            // custom execution strategy must be used to allow manual transactions 
            var executionStrategy = donorContext.Database.CreateExecutionStrategy();
            executionStrategy.Execute(() =>
            {
                // transaction must be used to temporarily allow identity insert
                using (var transaction = donorContext.Database.BeginTransaction())
                {
                    donorContext.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Donors.Donors ON");
                    donorContext.SaveChanges();

                    foreach (var donorId in donorIds)
                    {
                        donorContext.Donors.Add(new DonorImport.Data.Models.Donor {AtlasId = donorId, ExternalDonorCode = donorId.ToString()});
                    }

                    donorContext.SaveChanges();
                    transaction.Commit();
                }
            });
        }

        public IEnumerable<Donor> GetDonors(IEnumerable<int> donorIds)
        {
            return transientSearchContext.Donors.AsQueryable().Where(d => donorIds.Contains(d.DonorId));
        }

        private void RemoveDonorTestData()
        {
            if (DonorDatabaseExists())
            {
                TruncateTableIfExists(donorContext, "Donors", "Donors");
            }
        }

        private void RemoveTransientMatchingTestData()
        {
            if (TransientDatabaseExists())
            {
                // HlaNames and PGroups are not deleted between test runs, as these stay constant - and not needing to add them every test run saves test runtime.
                // If any issues arise from this in future, consider deleting them here also. 
                TruncateTableIfExists(transientSearchContext, "HlaNamePGroupRelationAtA");
                TruncateTableIfExists(transientSearchContext, "HlaNamePGroupRelationAtB");
                TruncateTableIfExists(transientSearchContext, "HlaNamePGroupRelationAtC");
                TruncateTableIfExists(transientSearchContext, "HlaNamePGroupRelationAtDrb1");
                TruncateTableIfExists(transientSearchContext, "HlaNamePGroupRelationAtDqb1");

                TruncateTableIfExists(transientSearchContext, "MatchingHlaAtA");
                TruncateTableIfExists(transientSearchContext, "MatchingHlaAtB");
                TruncateTableIfExists(transientSearchContext, "MatchingHlaAtC");
                TruncateTableIfExists(transientSearchContext, "MatchingHlaAtDrb1");
                TruncateTableIfExists(transientSearchContext, "MatchingHlaAtDqb1");

                TruncateTableIfExists(transientSearchContext, "Donors");
            }
        }

        private void TruncateTableIfExists(DbContext context, string tableName, string schema = "dbo")
        {
            if (TableExists(context, tableName, schema))
            {
                // SQL injection is not a risk here, as the tableName is not user provided, but hardcoded earlier in this class
                context.Database.ExecuteSqlRaw($"TRUNCATE TABLE {schema}.{tableName}");
                context.SaveChanges();
            }
        }

        private static bool TableExists(DbContext context, string tableName, string schemaName = "dbo")
        {
            var conn = context.Database.GetDbConnection();
            if (conn.State.Equals(ConnectionState.Closed))
            {
                conn.Open();
            }

            const string tableExistsSql = @"
    SELECT 1 FROM sys.tables AS T 
        INNER JOIN sys.schemas AS S ON T.schema_id = S.schema_id
    WHERE T.Name = @tableName AND S.Name = @schemaName";
            return conn.QuerySingleOrDefault(tableExistsSql, new {tableName, schemaName}) != null;
        }

        private bool TransientDatabaseExists() =>
            (transientSearchContext.Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator)?.Exists() ?? false;

        private bool DonorDatabaseExists() => (donorContext.Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator)?.Exists() ?? false;
    }
}