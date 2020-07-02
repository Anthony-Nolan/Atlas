using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Persistent.Context;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Repositories
{
    internal interface ITestDataRefreshHistoryRepository: IDataRefreshHistoryRepository
    {
        public Task<Dictionary<DataRefreshStage, DateTime?>> GetStageCompletionTimes(int recordId);
        /// <summary>
        /// Used when we want a successful refresh to exist in the integration test database, but don't care much about the specifics of the record.
        /// </summary>
        public int InsertDummySuccessfulRefreshRecord(string hlaNomenclatureVersion);

        public Task RemoveAllDataRefreshRecords();
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
                RefreshBeginUtc = DateTime.UtcNow,
                RefreshEndUtc = DateTime.UtcNow
            };
            Context.DataRefreshRecords.Add(dataRefreshRecord);
            Context.SaveChanges();
            return dataRefreshRecord.Id;
        }

        /// <inheritdoc />
        public async Task RemoveAllDataRefreshRecords()
        {
            var allRecords = Context.DataRefreshRecords.ToList();
            Context.DataRefreshRecords.RemoveRange(allRecords);
            await Context.SaveChangesAsync();
        }

        #endregion
    }
}