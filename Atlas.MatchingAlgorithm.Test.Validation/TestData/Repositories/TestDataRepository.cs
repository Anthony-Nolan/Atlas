using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchingAlgorithm.Data.Context;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Persistent.Context;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Repositories
{
    public interface ITestDataRepository
    {
        void SetupPersistentDatabase();
        void SetupDatabase();
        void AddTestDonors(IEnumerable<Donor> donors);
        IEnumerable<Donor> GetDonors(IEnumerable<int> donorIds);
    }

    /// <summary>
    /// Repository layer for interacting with test SQL database
    /// </summary>
    public class TestDataRepository : ITestDataRepository
    {
        private readonly SearchAlgorithmContext context;
        private readonly SearchAlgorithmPersistentContext persistentContext;

        public TestDataRepository(SearchAlgorithmContext context, SearchAlgorithmPersistentContext persistentContext)
        {
            this.context = context;
            this.persistentContext = persistentContext;
        }

        public void SetupPersistentDatabase()
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

        public void SetupDatabase()
        {
            // Ensure we have fresh data on each run. Done in setup rather than teardown to avoid data issues if the test suites are terminated early
            RemoveTestData();
            context.Database.Migrate();
        }

        public void AddTestDonors(IEnumerable<Donor> donors)
        {
            foreach (var donor in donors)
            {
                if (!context.Donors.Any(d => d.DonorId == donor.DonorId))
                {
                    context.Donors.Add(donor);
                }
            }

            context.SaveChanges();
        }

        public IEnumerable<Donor> GetDonors(IEnumerable<int> donorIds)
        {
            return context.Donors.AsQueryable().Where(d => donorIds.Contains(d.DonorId));
        }

        private void RemoveTestData()
        {
            if (TransientDatabaseExists() && DonorTableExists())
            {
                context.Database.ExecuteSqlRaw("TRUNCATE TABLE [Donors]");
                context.Database.ExecuteSqlRaw("TRUNCATE TABLE [MatchingHlaAtA]");
                context.Database.ExecuteSqlRaw("TRUNCATE TABLE [MatchingHlaAtB]");
                context.Database.ExecuteSqlRaw("TRUNCATE TABLE [MatchingHlaAtC]");
                context.Database.ExecuteSqlRaw("TRUNCATE TABLE [MatchingHlaAtDrb1]");
                context.Database.ExecuteSqlRaw("TRUNCATE TABLE [MatchingHlaAtDqb1]");
                context.SaveChanges();
            }
        }

        private bool DonorTableExists()
        {
            var conn = context.Database.GetDbConnection();
            if (conn.State.Equals(ConnectionState.Closed))
            {
                conn.Open();
            }

            using (var command = conn.CreateCommand())
            {
                command.CommandText = @"
    SELECT 1 FROM sys.tables AS T 
        INNER JOIN sys.schemas AS S ON T.schema_id = S.schema_id
    WHERE T.Name = 'Donors'";
                return command.ExecuteScalar() != null;
            }
        }

        private bool TransientDatabaseExists()
        {
            return (context.Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator)?.Exists() ?? false;
        }
    }
}