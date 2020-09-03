using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
using Atlas.MatchingAlgorithm.Settings;
using EnumStringValues;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshCleanupService
    {
        /// <summary>
        /// Runs appropriate clean up after a data refresh job - i.e. scaling down the dormant database, and re-enabling donor update functions.
        /// This should only ever be run manually, and only if the server dies in the middle of data-refresh, as the normal teardown will not have run.
        /// </summary>
        Task RunDataRefreshCleanup();
    }

    public class DataRefreshCleanupService : IDataRefreshCleanupService
    {
        private readonly IMatchingAlgorithmImportLogger logger;
        private readonly IAzureDatabaseNameProvider azureDatabaseNameProvider;
        private readonly IActiveDatabaseProvider activeDatabaseProvider;
        private readonly IAzureDatabaseManager azureDatabaseManager;
        private readonly DataRefreshSettings dataRefreshSettings;
        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private readonly IDataRefreshSupportNotificationSender notificationSender;

        public DataRefreshCleanupService(
            IMatchingAlgorithmImportLogger logger,
            IAzureDatabaseNameProvider azureDatabaseNameProvider,
            IActiveDatabaseProvider activeDatabaseProvider,
            IAzureDatabaseManager azureDatabaseManager,
            DataRefreshSettings dataRefreshSettings,
            IDataRefreshHistoryRepository dataRefreshHistoryRepository,
            IDataRefreshSupportNotificationSender notificationSender
        )
        {
            this.logger = logger;
            this.azureDatabaseNameProvider = azureDatabaseNameProvider;
            this.activeDatabaseProvider = activeDatabaseProvider;
            this.azureDatabaseManager = azureDatabaseManager;
            this.dataRefreshSettings = dataRefreshSettings;
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            this.notificationSender = notificationSender;
        }

        public async Task RunDataRefreshCleanup()
        {
            if (IsCleanupNecessary())
            {
                logger.SendTrace("DATA REFRESH: Manual Teardown requested. This indicates that the data refresh failed unexpectedly.");
                await notificationSender.SendRequestManualTeardownNotification();

                await ScaleDatabase();
                await UpdateStalledDataRefreshHistoryRecords();
            }
            else
            {
                logger.SendTrace("Data Refresh cleanup triggered, but no in progress jobs detected. Cleanup is not necessary.");
            }
        }

        private bool IsCleanupNecessary()
        {
            // This assumes that the method will not be called when a job is actually in progress, only when a job has finished without appropriate teardown 
            return dataRefreshHistoryRepository.GetInProgressJobs().Any();
        }

        private async Task ScaleDatabase()
        {
            var targetSize = dataRefreshSettings.DormantDatabaseSize.ParseToEnum<AzureDatabaseSize>();
            var databaseName = azureDatabaseNameProvider.GetDatabaseName(activeDatabaseProvider.GetDormantDatabase());
            logger.SendTrace($"DATA REFRESH CLEANUP: Scaling database: {databaseName} to size {targetSize}");
            await azureDatabaseManager.UpdateDatabaseSize(databaseName, targetSize);
        }

        private async Task UpdateStalledDataRefreshHistoryRecords()
        {
            var dataRefreshRecords = dataRefreshHistoryRepository.GetInProgressJobs().ToList();
            foreach (var job in dataRefreshRecords)
            {
                await dataRefreshHistoryRepository.UpdateExecutionDetails(job.Id, job.HlaNomenclatureVersion, DateTime.UtcNow);
                await dataRefreshHistoryRepository.UpdateSuccessFlag(job.Id, false);
            }
        }
    }
}