using System;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Data.Persistent.Repositories;
using Nova.SearchAlgorithm.Services.ConfigurationProviders;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshOrchestrator
    {
        Task RefreshDataIfNecessary();
    }

    public class DataRefreshOrchestrator : IDataRefreshOrchestrator
    {
        private readonly ILogger logger;
        private readonly IWmdaHlaVersionProvider wmdaHlaVersionProvider;
        private readonly IDataRefreshService dataRefreshService;
        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;

        public DataRefreshOrchestrator(
            ILogger logger,
            IWmdaHlaVersionProvider wmdaHlaVersionProvider,
            IDataRefreshService dataRefreshService,
            IDataRefreshHistoryRepository dataRefreshHistoryRepository)
        {
            this.logger = logger;
            this.wmdaHlaVersionProvider = wmdaHlaVersionProvider;
            this.dataRefreshService = dataRefreshService;
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
        }

        public async Task RefreshDataIfNecessary()
        {
            if (ShouldRunDataRefresh())
            {
                await RunDataRefresh();
            }
        }

        private async Task RunDataRefresh()
        {
            var databaseToRefresh = DatabaseToRefresh();
            var wmdaDatabaseVersion = wmdaHlaVersionProvider.GetLatestHlaDatabaseVersion();

            var dataRefreshRecord = new DataRefreshRecord
            {
                Database = databaseToRefresh.ToString(),
                RefreshBeginUtc = DateTime.UtcNow,
                WmdaDatabaseVersion = wmdaDatabaseVersion,
            };

            var recordId = await dataRefreshHistoryRepository.Create(dataRefreshRecord);

            try
            {
                await dataRefreshService.RefreshData(databaseToRefresh, wmdaDatabaseVersion);
                await MarkDataHistoryRecordAsComplete(recordId, true);
            }
            catch (Exception e)
            {
                logger.SendTrace($"Data Refresh Failed: ${e.ToString()}", LogLevel.Critical);
                await MarkDataHistoryRecordAsComplete(recordId, false);
            }
        }

        private async Task MarkDataHistoryRecordAsComplete(int recordId, bool wasSuccess)
        {
            await dataRefreshHistoryRepository.UpdateFinishTime(recordId, DateTime.UtcNow);
            await dataRefreshHistoryRepository.UpdateSuccessFlag(recordId, wasSuccess);
        }

        private bool ShouldRunDataRefresh()
        {
            return HasNewWmdaDataBeenPublished() && !IsRefreshInProgress();
        }

        private bool HasNewWmdaDataBeenPublished()
        {
            var activeHlaDataVersion = wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion();
            var latestHlaDataVersion = wmdaHlaVersionProvider.GetLatestHlaDatabaseVersion();
            return activeHlaDataVersion != latestHlaDataVersion;
        }

        private bool IsRefreshInProgress()
        {
            return dataRefreshHistoryRepository.GetInProgressJobs().Any();
        }

        private TransientDatabase DatabaseToRefresh()
        {
            var activeDatabase = dataRefreshHistoryRepository.GetActiveDatabase();
            switch (activeDatabase)
            {
                case TransientDatabase.DatabaseA:
                    return TransientDatabase.DatabaseB;
                case TransientDatabase.DatabaseB:
                case null:
                    return TransientDatabase.DatabaseA;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}