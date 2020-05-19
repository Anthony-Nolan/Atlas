using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.ConfigSettings;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Extensions;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Microsoft.Extensions.Options;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshCleanupService
    {
        /// <summary>
        /// Runs appropriate clean up after a data refresh job - i.e. scaling down the dormant database, and re-enabling donor update functions.
        /// This should only ever be run manually, and only if the server dies in the middle of data-refresh, as the normal teardown will not have run.
        /// </summary>
        Task RunDataRefreshCleanup();

        /// <summary>
        /// Will decide whether we think clean up needs to be run, and if so send a notification for the support team.
        /// This is only accurate if the following are true:
        /// - This class is called from the same service as runs the data refresh
        /// - That service is a single instance, always-on application
        /// - This is only called on startup of the service
        /// </summary>
        Task SendCleanupRecommendation();
    }

    public class DataRefreshCleanupService : IDataRefreshCleanupService
    {
        private readonly ILogger logger;
        private readonly IAzureDatabaseNameProvider azureDatabaseNameProvider;
        private readonly IActiveDatabaseProvider activeDatabaseProvider;
        private readonly IAzureDatabaseManager azureDatabaseManager;
        private readonly IOptions<DataRefreshSettings> dataRefreshSettings;
        private readonly IAzureFunctionManager azureFunctionManager;
        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private readonly IDataRefreshNotificationSender notificationSender;

        public DataRefreshCleanupService(
            ILogger logger,
            IAzureDatabaseNameProvider azureDatabaseNameProvider,
            IActiveDatabaseProvider activeDatabaseProvider,
            IAzureDatabaseManager azureDatabaseManager,
            IOptions<DataRefreshSettings> dataRefreshSettings,
            IAzureFunctionManager azureFunctionManager,
            IDataRefreshHistoryRepository dataRefreshHistoryRepository,
            IDataRefreshNotificationSender notificationSender
        )
        {
            this.logger = logger;
            this.azureDatabaseNameProvider = azureDatabaseNameProvider;
            this.activeDatabaseProvider = activeDatabaseProvider;
            this.azureDatabaseManager = azureDatabaseManager;
            this.dataRefreshSettings = dataRefreshSettings;
            this.azureFunctionManager = azureFunctionManager;
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            this.notificationSender = notificationSender;
        }

        public async Task RunDataRefreshCleanup()
        {
            if (IsCleanupNecessary())
            {
                logger.SendTrace(
                    "DATA REFRESH: Manual Teardown requested. This indicates that the data refresh failed unexpectedly.",
                    LogLevel.Info);
                await notificationSender.SendRequestManualTeardownNotification();

                await ScaleDatabase();
                await EnableDonorManagementFunction();
                await UpdateStalledDataRefreshHistoryRecords();
            }
            else
            {
                logger.SendTrace(
                    "Data Refresh cleanup triggered, but no in progress jobs detected. Are you sure cleanup is necessary?",
                    LogLevel.Info
                );
            }
        }

        public async Task SendCleanupRecommendation()
        {
            if (IsCleanupNecessary())
            {
                await notificationSender.SendRecommendManualCleanupAlert();
            }
        }

        private bool IsCleanupNecessary()
        {
            // This assumes that the method will not be called when a job is actually in progress, only when a job has finished without appropriate teardown 
            return dataRefreshHistoryRepository.GetInProgressJobs().Any();
        }

        private async Task ScaleDatabase()
        {
            var targetSize = dataRefreshSettings.Value.DormantDatabaseSize.ToAzureDatabaseSize();
            var databaseName = azureDatabaseNameProvider.GetDatabaseName(activeDatabaseProvider.GetDormantDatabase());
            logger.SendTrace($"DATA REFRESH CLEANUP: Scaling database: {databaseName} to size {targetSize}", LogLevel.Info);
            await azureDatabaseManager.UpdateDatabaseSize(databaseName, targetSize);
        }

        private async Task EnableDonorManagementFunction()
        {
            var donorFunctionsAppName = dataRefreshSettings.Value.DonorFunctionsAppName;
            var donorImportFunctionName = dataRefreshSettings.Value.DonorImportFunctionName;
            logger.SendTrace($"DATA REFRESH CLEANUP: Re-enabling donor import function with name: {donorImportFunctionName}", LogLevel.Info);
            await azureFunctionManager.StartFunction(donorFunctionsAppName, donorImportFunctionName);
        }

        private async Task UpdateStalledDataRefreshHistoryRecords()
        {
            var dataRefreshRecords = dataRefreshHistoryRepository.GetInProgressJobs().ToList();
            foreach (var job in dataRefreshRecords)
            {
                await dataRefreshHistoryRepository.UpdateExecutionDetails(job.Id, null, DateTime.UtcNow);
                await dataRefreshHistoryRepository.UpdateSuccessFlag(job.Id, false);
            }
        }
    }
}