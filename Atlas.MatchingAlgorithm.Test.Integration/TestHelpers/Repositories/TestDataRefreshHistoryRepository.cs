using System;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Persistent.Context;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Repositories
{
    internal interface ITestDataRefreshHistoryRepository: IDataRefreshHistoryRepository
    {
        public Task<DateTime?> GetStageCompletionTime(int recordId, DataRefreshStage stage);
        /// <summary>
        /// Used when we want a successful refresh to exist in the integration test database, but don't care much about the specifics of the record.
        /// </summary>
        public int InsertDummySuccessfulRefreshRecord(string hlaNomenclatureVersion = null);
    }

    internal class TestDataRefreshHistoryRepository : DataRefreshHistoryRepository, ITestDataRefreshHistoryRepository
    {
        /// <inheritdoc />
        public TestDataRefreshHistoryRepository(SearchAlgorithmPersistentContext context) : base(context)
        {
        }

        #region Implementation of ITestDataRefreshHistoryRepository

        /// <inheritdoc />
        public async Task<DateTime?> GetStageCompletionTime(int recordId, DataRefreshStage stage)
        {
            var record = await GetRecordById(recordId);
            
            return stage switch
            {
                DataRefreshStage.MetadataDictionaryRefresh => record.MetadataDictionaryRefreshCompleted,
                DataRefreshStage.DataDeletion => record.DataDeletionCompleted,
                DataRefreshStage.DatabaseScalingSetup => record.DatabaseScalingSetupCompleted,
                DataRefreshStage.DonorImport => record.DonorImportCompleted,
                DataRefreshStage.DonorHlaProcessing => record.DonorHlaProcessingCompleted,
                DataRefreshStage.DatabaseScalingTearDown => record.DatabaseScalingTearDownCompleted,
                DataRefreshStage.QueuedDonorUpdateProcessing => record.QueuedDonorUpdatesCompleted,
                _ => throw new ArgumentOutOfRangeException(nameof(stage))
            };
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

        #endregion
    }
}