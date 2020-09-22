using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchingAlgorithm.Data.Persistent.Context;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Repositories
{
    internal interface ITestDataRefreshHistoryRepository : IDataRefreshHistoryRepository
    {
        public Task<Dictionary<DataRefreshStage, DateTime?>> GetStageCompletionTimes(int recordId);

        /// <summary>
        /// Used when we want a successful refresh to exist in the integration test database, but don't care much about the specifics of the record.
        /// </summary>
        public int InsertDummySuccessfulRefreshRecord(string hlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion);

        public Task<int?> GetLastSuccessfullyInsertedDonor(int recordId);

        public Task RemoveAllDataRefreshRecords();

        public Task SwitchDormantDatabase();
    }

    internal class TestDataRefreshHistoryRepository : DataRefreshHistoryRepository, ITestDataRefreshHistoryRepository
    {
        /// <inheritdoc />
        public TestDataRefreshHistoryRepository(SearchAlgorithmPersistentContext context) : base(context)
        {
        }

        #region Implementation of ITestDataRefreshHistoryRepository

        /// <inheritdoc />
        public new async Task<Dictionary<DataRefreshStage, DateTime?>> GetStageCompletionTimes(int recordId)
        {
            return await base.GetStageCompletionTimes(recordId);
        }

        /// <inheritdoc />
        public int InsertDummySuccessfulRefreshRecord(string hlaNomenclatureVersion)
        {
            var dataRefreshRecord = new DataRefreshRecord
            {
                Database = "DatabaseA",
                WasSuccessful = true,
                HlaNomenclatureVersion = hlaNomenclatureVersion,
                RefreshRequestedUtc = DateTime.UtcNow,
                RefreshEndUtc = DateTime.UtcNow
            };
            Context.DataRefreshRecords.Add(dataRefreshRecord);
            Context.SaveChanges();
            return dataRefreshRecord.Id;
        }

        /// <inheritdoc />
        public new async Task<int?> GetLastSuccessfullyInsertedDonor(int recordId)
        {
            return await base.GetLastSuccessfullyInsertedDonor(recordId);
        }

        /// <inheritdoc />
        public async Task RemoveAllDataRefreshRecords()
        {
            var allRecords = Context.DataRefreshRecords.ToList();
            Context.DataRefreshRecords.RemoveRange(allRecords);
            await Context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task SwitchDormantDatabase()
        {
            var latest = GetLastSuccessfulRecord();
            var active = GetActiveDatabase() ?? TransientDatabase.DatabaseA;
            var dataRefreshRecord = new DataRefreshRecord
            {
                Database = active.Other().ToString(),
                WasSuccessful = true,
                HlaNomenclatureVersion = latest.HlaNomenclatureVersion,
                RefreshRequestedUtc = DateTime.UtcNow,
                RefreshEndUtc = DateTime.UtcNow
            };
            await Context.DataRefreshRecords.AddAsync(dataRefreshRecord);
            await Context.SaveChangesAsync();
        }

        #endregion
    }
}